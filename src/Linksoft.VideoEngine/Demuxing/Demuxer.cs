namespace Linksoft.VideoEngine.Demuxing;

/// <summary>
/// Wraps FFmpeg demuxing for RTSP/HTTP video streams with interrupt-based timeout.
/// </summary>
internal sealed unsafe class Demuxer : IDisposable
{
    private const int OpenTimeoutSeconds = 15;
    private const int ReadTimeoutSeconds = 10;
    private const int AverrorExit = -1414092869;

    private readonly ILogger? logger;
    private readonly Stopwatch timeoutWatch = new();

    private AVFormatContext* fmtCtx;
    private AVPacket* packet;
    private int videoStreamIndex = -1;
    private AVCodecParameters* videoCodecParameters;
    private AVRational videoTimeBase;
    private AVRational videoFrameRate;
    private bool disposed;

    private int timeoutSeconds = OpenTimeoutSeconds;
    private volatile bool abortRequested;
    private CancellationToken cancellationToken;
    private GCHandle gcHandle;
    private AVIOInterruptCB_callback? interruptDelegate;

    internal Demuxer()
        : this(logger: null)
    {
    }

    internal Demuxer(ILogger? logger)
    {
        this.logger = logger;
    }

    public int VideoStreamIndex => videoStreamIndex;

    public AVCodecParameters* VideoCodecParameters => videoCodecParameters;

    public AVRational VideoTimeBase => videoTimeBase;

    public AVRational VideoFrameRate => videoFrameRate;

    public AVPacket* CurrentPacket => packet;

    public bool IsVideoPacket => packet->stream_index == videoStreamIndex;

    /// <summary>
    /// Signals the interrupt callback to abort any blocking FFmpeg call immediately.
    /// </summary>
    public void RequestAbort()
    {
        abortRequested = true;
    }

    public void Open(
        Uri streamUri,
        StreamOptions options,
        CancellationToken ct = default)
    {
        cancellationToken = ct;
        fmtCtx = avformat_alloc_context();
        if (fmtCtx is null)
        {
            throw new InvalidOperationException("Failed to allocate AVFormatContext.");
        }

        gcHandle = GCHandle.Alloc(this);
        interruptDelegate = new AVIOInterruptCB_callback(InterruptCallback);
        fmtCtx->interrupt_callback.callback = interruptDelegate;
        fmtCtx->interrupt_callback.opaque = (void*)GCHandle.ToIntPtr(gcHandle);

        AVDictionary* dict = null;
        SetFormatOptions(ref dict, options);

        timeoutSeconds = OpenTimeoutSeconds;
        timeoutWatch.Restart();

        var url = streamUri.AbsoluteUri;
        var ret = avformat_open_input(ref fmtCtx, url, null, ref dict);
        av_dict_free(ref dict);

        if (ret < 0)
        {
            if (ret == AverrorExit)
            {
                logger?.LogWarning(
                    "avformat_open_input returned AVERROR_EXIT: abortRequested={Abort}, ctCancelled={CtCancelled}, elapsed={Elapsed:F1}s, timeout={Timeout}s",
                    abortRequested,
                    cancellationToken.IsCancellationRequested,
                    timeoutWatch.Elapsed.TotalSeconds,
                    timeoutSeconds);
            }

            fmtCtx = null;
            throw new FFmpegException(ret, "Failed to open input");
        }

        timeoutWatch.Restart();
        AVDictionary* infoDict = null;
        ret = avformat_find_stream_info(fmtCtx, ref infoDict);
        if (ret < 0)
        {
            throw new FFmpegException(ret, "Failed to find stream info");
        }

        if (fmtCtx->pb is not null)
        {
            fmtCtx->pb->eof_reached = 0;
        }

        FindVideoStream();

        timeoutSeconds = ReadTimeoutSeconds;
        timeoutWatch.Restart();

        packet = av_packet_alloc();
        if (packet is null)
        {
            throw new InvalidOperationException("Failed to allocate AVPacket.");
        }
    }

    public int ReadPacket()
    {
        timeoutWatch.Restart();
        return av_read_frame(fmtCtx, packet);
    }

    public void UnrefPacket()
    {
        av_packet_unref(packet);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (packet is not null)
        {
            var p = packet;
            av_packet_free(ref p);
            packet = null;
        }

        if (fmtCtx is not null)
        {
            avformat_close_input(ref fmtCtx);
        }

        if (gcHandle.IsAllocated)
        {
            gcHandle.Free();
        }

        interruptDelegate = null;
    }

    private void FindVideoStream()
    {
        for (uint i = 0; i < fmtCtx->nb_streams; i++)
        {
            var stream = fmtCtx->streams[i];
            if (stream->codecpar->codec_type != AVMediaType.Video)
            {
                continue;
            }

            videoStreamIndex = (int)i;
            videoCodecParameters = stream->codecpar;
            videoTimeBase = stream->time_base;
            videoFrameRate = stream->avg_frame_rate.Num > 0
                ? stream->avg_frame_rate
                : stream->r_frame_rate;
            return;
        }

        throw new InvalidOperationException("No video stream found in input.");
    }

    private static void SetFormatOptions(
        ref AVDictionary* dict,
        StreamOptions options)
    {
        av_dict_set(ref dict, "rtsp_transport", options.RtspTransport, DictWriteFlags.None);
        av_dict_set(ref dict, "probesize", "50000000", DictWriteFlags.None);
        av_dict_set(ref dict, "analyzeduration", "10000000", DictWriteFlags.None);

        var timeoutUs = (OpenTimeoutSeconds * 1_000_000).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (FFmpegLoader.IsVersion8OrGreater)
        {
            av_dict_set(ref dict, "timeout", timeoutUs, DictWriteFlags.None);
        }
        else
        {
            av_dict_set(ref dict, "stimeout", timeoutUs, DictWriteFlags.None);
        }

        if (options.UseLowLatencyMode)
        {
            av_dict_set(ref dict, "fflags", "nobuffer", DictWriteFlags.None);
            av_dict_set(ref dict, "flags", "low_delay", DictWriteFlags.None);
        }
    }

    private static int InterruptCallback(void* opaque)
    {
        var handle = GCHandle.FromIntPtr((IntPtr)opaque);
        if (handle.Target is not Demuxer demuxer)
        {
            return 1;
        }

        if (demuxer.abortRequested || demuxer.cancellationToken.IsCancellationRequested)
        {
            return 1;
        }

        return demuxer.timeoutWatch.Elapsed.TotalSeconds > demuxer.timeoutSeconds ? 1 : 0;
    }
}