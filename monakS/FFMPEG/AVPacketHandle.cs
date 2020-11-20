using System;
using FFmpeg.AutoGen;

namespace monakS.FFMPEG
{
  public class AVPacketHandle
  {
    private readonly unsafe AVPacket* _pkt;

    public unsafe AVPacket* GetRawPacket()
    {
      return _pkt;
    }

    public byte[] Data { get; } = new byte[0];
    public double DurationSeconds { get; set; }
    public long Duration { get; } = 0;
    public int Height { get; } = 0;
    public int Width { get; } = 0;
    public AVRational TimeBase { get; }
    public unsafe bool IsKeyframe => _pkt->flags == ffmpeg.AV_PKT_FLAG_KEY;
    public AVCodecParametersHandle CodecParameters { get; }

    public static AVPacketHandle Empty => new AVPacketHandle();

    private AVPacketHandle()
    {
    }

    public unsafe AVPacketHandle(AVPacket* pkt,
      int width,
      int height,
      double seconds = 0,
      AVRational timeBase = default,
      AVCodecParametersHandle codecParameters = null)
    {
      Width = width;
      Height = height;
      _pkt = ffmpeg.av_packet_clone(pkt);
      this.Data = new Span<byte>(_pkt->data, _pkt->size).ToArray();
      this.Duration = pkt->duration;
      this.DurationSeconds = seconds;
      this.TimeBase = timeBase;
      this.CodecParameters = codecParameters;
    }

    ~AVPacketHandle()
    {
      unsafe
      {
        var tmpPkt = _pkt;
        ffmpeg.av_packet_free(&tmpPkt);
      }
    }
  }
}