using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using FFmpeg.AutoGen;

namespace monakS.FFMPEG
{
  public class VideoFile : IDisposable
  {
    private readonly string _fileName;
    private unsafe AVFormatContext* _ctx = null;

    public bool IsOpen { get; set; } = false;
    public long CurrentTimestamp { get; set; } = 0L;

    public VideoFile(string fileName)
    {
      _fileName = fileName;
    }

    public unsafe bool Open(AVCodecParametersHandle codecParameters, AVRational timeBase)
    {
      if (codecParameters == null || timeBase.Equals(new AVRational()))
        return false;

      AVFormatContext* ctx = null;
      var format = ffmpeg.av_guess_format(null, _fileName, null);

      if (!FFMPEGHelper.Try(ffmpeg.avformat_alloc_output_context2(&ctx, format, null, null)))
        return false;

      _ctx = ctx;

      var stream = ffmpeg.avformat_new_stream(_ctx, null);

      var parameters = ffmpeg.avcodec_parameters_alloc();
      ffmpeg.avcodec_parameters_copy(parameters, codecParameters.GetRawParameters());

      stream->time_base = timeBase;
      stream->id = 0;
      stream->codecpar = parameters;
      stream->codecpar->codec_tag = 0;

      if (!FFMPEGHelper.Try(ffmpeg.avio_open2(&_ctx->pb, _fileName,
        ffmpeg.AVIO_FLAG_WRITE, null, null)))
        return false;

      AVDictionary* opts = null;
      ffmpeg.av_dict_set(&opts, "movflags", "+frag_keyframe+empty_moov+default_base_moof", 0);

      ffmpeg.avformat_write_header(_ctx, &opts);

      ffmpeg.av_dict_free(&opts);

      this.IsOpen = true;

      return true;
    }

    public void Write(IEnumerable<AVPacketHandle> packets, CancellationToken token)
    {
      foreach (var pkt in packets)
      {
        if (token.IsCancellationRequested)
          break;
        this.Write(pkt);
      }
    }

    public unsafe void Write(AVPacketHandle packet)
    {
      if (!this.IsOpen)
        throw new IOException($"Write before open {_fileName}");

      var writePkt = ffmpeg.av_packet_clone(packet.GetRawPacket());
      writePkt->pts = this.CurrentTimestamp;
      writePkt->dts = this.CurrentTimestamp;

      ffmpeg.av_write_frame(_ctx, writePkt);

      ffmpeg.av_packet_unref(writePkt);

      this.CurrentTimestamp += packet.Duration;
    }

    public unsafe void Dispose()
    {
      if (IsOpen)
      {
        ffmpeg.av_write_trailer(_ctx);
      }

      fixed (AVFormatContext** tmpCtx = &_ctx)
      {
        ffmpeg.avformat_close_input(tmpCtx);
      }
    }
  }
}