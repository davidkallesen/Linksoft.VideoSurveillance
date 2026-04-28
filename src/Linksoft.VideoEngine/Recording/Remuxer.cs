// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
namespace Linksoft.VideoEngine.Recording;

/// <summary>
/// Records a video stream by remuxing (packet copy, no transcoding) to a container file.
/// </summary>
[SuppressMessage("", "CA1806:calls av_*", Justification = "OK")]
internal sealed unsafe class Remuxer : IDisposable
{
    private readonly Lock syncLock = new();
    private AVFormatContext* outputCtx;
    private AVRational outputTimeBase;
    private long firstDts = AV_NOPTS_VALUE;
    private long lastDts = AV_NOPTS_VALUE;
    private bool receivedKeyframe;
    private bool disposed;

    public bool IsOpen => outputCtx is not null;

    public void Open(
        string outputPath,
        AVCodecParameters* codecpar,
        AVRational inputTimeBase,
        int rotationDegrees = 0)
    {
        lock (syncLock)
        {
            OpenLocked(outputPath, codecpar, inputTimeBase, rotationDegrees);
        }
    }

    /// <summary>
    /// Atomically closes the current output file and opens
    /// <paramref name="newOutputPath"/>. Holding the lock across both
    /// operations means packets arriving from the demux thread mid-switch
    /// either land in the previous segment or the new one — they are
    /// never silently dropped in the close/open gap.
    /// </summary>
    public void SwitchTo(
        string newOutputPath,
        AVCodecParameters* codecpar,
        AVRational inputTimeBase,
        int rotationDegrees = 0)
    {
        lock (syncLock)
        {
            CloseLocked();
            OpenLocked(newOutputPath, codecpar, inputTimeBase, rotationDegrees);
        }
    }

    private void OpenLocked(
        string outputPath,
        AVCodecParameters* codecpar,
        AVRational inputTimeBase,
        int rotationDegrees)
    {
        AVFormatContext* ctx = null;

        var ret = avformat_alloc_output_context2(ref ctx, null, null, outputPath);
        if (ret < 0 || ctx is null)
        {
            throw new FFmpegException(ret, "Failed to allocate output context");
        }

        outputCtx = ctx;

        var outStream = avformat_new_stream(outputCtx, null);
        if (outStream is null)
        {
            throw new InvalidOperationException("Failed to create output stream.");
        }

        ret = avcodec_parameters_copy(outStream->codecpar, codecpar);
        if (ret < 0)
        {
            throw new FFmpegException(ret, "Failed to copy codec parameters to output");
        }

        outStream->codecpar->codec_tag = 0;
        outputTimeBase = outStream->time_base;

        // For rotated streams, write a "rotate" tag on the stream metadata.
        // FFmpeg's MP4/MOV muxer translates this into a tkhd display matrix
        // that VLC, Media Player, browsers, and ffplay all honour at playback.
        if (rotationDegrees != 0)
        {
            var rotationStr = rotationDegrees.ToString(System.Globalization.CultureInfo.InvariantCulture);
            av_dict_set(ref outStream->metadata, "rotate", rotationStr, DictWriteFlags.None);
        }

        ret = avio_open(ref outputCtx->pb, outputPath, IOFlags.Write);
        if (ret < 0)
        {
            throw new FFmpegException(ret, "Failed to open output file");
        }

        AVDictionary* opts = null;
        ret = avformat_write_header(outputCtx, ref opts);
        if (ret < 0)
        {
            throw new FFmpegException(ret, "Failed to write output header");
        }

        outputTimeBase = outStream->time_base;
        firstDts = AV_NOPTS_VALUE;
        lastDts = AV_NOPTS_VALUE;
        receivedKeyframe = false;
    }

    public void WritePacket(
        AVPacket* srcPacket,
        AVRational srcTimeBase)
    {
        lock (syncLock)
        {
            if (outputCtx is null)
            {
                return;
            }

            var isKeyframe = (srcPacket->flags & PktFlags.Key) != PktFlags.None;
            if (!receivedKeyframe)
            {
                if (!isKeyframe)
                {
                    return;
                }

                receivedKeyframe = true;
            }

            var clonedPkt = av_packet_clone(srcPacket);
            if (clonedPkt is null)
            {
                return;
            }

            try
            {
                if (firstDts == AV_NOPTS_VALUE)
                {
                    firstDts = clonedPkt->dts != AV_NOPTS_VALUE ? clonedPkt->dts : 0;
                }

                var dtsOffset = clonedPkt->dts != AV_NOPTS_VALUE ? clonedPkt->dts - firstDts : 0;
                var ptsOffset = clonedPkt->pts != AV_NOPTS_VALUE ? clonedPkt->pts - firstDts : dtsOffset;

                clonedPkt->dts = av_rescale_q(dtsOffset, srcTimeBase, outputTimeBase);
                clonedPkt->pts = av_rescale_q(ptsOffset, srcTimeBase, outputTimeBase);
                clonedPkt->stream_index = 0;
                clonedPkt->pos = -1;

                // Enforce monotonically increasing DTS — required by strict muxers (e.g. Matroska).
                // RTSP streams with B-frames can deliver packets out of DTS order.
                if (lastDts != AV_NOPTS_VALUE && clonedPkt->dts <= lastDts)
                {
                    clonedPkt->dts = lastDts + 1;
                    if (clonedPkt->pts < clonedPkt->dts)
                    {
                        clonedPkt->pts = clonedPkt->dts;
                    }
                }

                lastDts = clonedPkt->dts;

                _ = av_interleaved_write_frame(outputCtx, clonedPkt);
            }
            finally
            {
                av_packet_free(ref clonedPkt);
            }
        }
    }

    public void Close()
    {
        lock (syncLock)
        {
            CloseLocked();
        }
    }

    private void CloseLocked()
    {
        if (outputCtx is null)
        {
            return;
        }

        if (receivedKeyframe)
        {
            av_write_trailer(outputCtx);
        }

        avio_closep(ref outputCtx->pb);
        avformat_free_context(outputCtx);
        outputCtx = null;
        firstDts = AV_NOPTS_VALUE;
        lastDts = AV_NOPTS_VALUE;
        receivedKeyframe = false;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Close();
    }
}