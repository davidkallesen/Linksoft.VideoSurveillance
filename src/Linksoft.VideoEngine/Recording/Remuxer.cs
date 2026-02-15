namespace Linksoft.VideoEngine.Recording;

/// <summary>
/// Records a video stream by remuxing (packet copy, no transcoding) to a container file.
/// </summary>
internal sealed unsafe class Remuxer : IDisposable
{
    private readonly Lock syncLock = new();
    private AVFormatContext* outputCtx;
    private AVRational inputTimeBase;
    private AVRational outputTimeBase;
    private long firstDts = AV_NOPTS_VALUE;
    private long lastDts = AV_NOPTS_VALUE;
    private bool receivedKeyframe;
    private bool disposed;

    public bool IsOpen => outputCtx is not null;

    public void Open(
        string outputPath,
        AVCodecParameters* codecpar,
        AVRational inputTimeBase)
    {
        lock (syncLock)
        {
            AVFormatContext* ctx = null;
            int ret = avformat_alloc_output_context2(ref ctx, null, null, outputPath);
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
            this.inputTimeBase = inputTimeBase;
            outputTimeBase = outStream->time_base;

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

            bool isKeyframe = (srcPacket->flags & PktFlags.Key) != 0;
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

                long dtsOffset = clonedPkt->dts != AV_NOPTS_VALUE ? clonedPkt->dts - firstDts : 0;
                long ptsOffset = clonedPkt->pts != AV_NOPTS_VALUE ? clonedPkt->pts - firstDts : dtsOffset;

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