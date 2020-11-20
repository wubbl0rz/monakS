using System;
using System.Drawing;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace monakS.FFMPEG
{
  public class FrameResizer : IDisposable
  {
    public Size Src { get; }
    public Size Dst { get; }

    private readonly unsafe SwsContext* _ctx;
    private byte_ptrArray4 _dstData;
    private int_array4 _dstLinesize;
    private IntPtr _buffer;
    private AVPixelFormat _dstFmt;
    private AVPixelFormat _srcFmt;

    public unsafe FrameResizer(Size src, Size dst, AVPixelFormat fmt, bool keepRatio = true,
      AVPixelFormat dstFmt = AVPixelFormat.AV_PIX_FMT_NONE)
    {
      if (dstFmt == AVPixelFormat.AV_PIX_FMT_NONE)
      {
        dstFmt = fmt;
      }

      if (keepRatio)
      {
        var tmpW = ffmpeg.av_rescale(dst.Height, src.Width, src.Height);
        var tmpH = ffmpeg.av_rescale(dst.Width, src.Height, src.Width);

        dst.Height = (int) Math.Min(tmpH, dst.Height);
        dst.Width = (int) Math.Min(tmpW, dst.Width);
      }

      Dst = dst;
      Src = src;
      _dstFmt = dstFmt;
      _srcFmt = fmt;

      _ctx = ffmpeg.sws_getContext(
        src.Width,
        src.Height,
        fmt,
        dst.Width,
        dst.Height,
        dstFmt,
        ffmpeg.SWS_POINT,
        null,
        null,
        null);

      var bufferSize = ffmpeg.av_image_get_buffer_size(
        dstFmt,
        dst.Width,
        dst.Height,
        32);

      _buffer = Marshal.AllocHGlobal(bufferSize);
      _dstData = new byte_ptrArray4();
      _dstLinesize = new int_array4();

      ffmpeg.av_image_fill_arrays(ref
        _dstData,
        ref _dstLinesize,
        (byte*) _buffer,
        dstFmt,
        dst.Width,
        dst.Height,
        32);
    }

    public unsafe void Dispose()
    {
      Marshal.FreeHGlobal(_buffer);
      ffmpeg.sws_freeContext(_ctx);
    }

    public unsafe AVFrame* Resize(AVFrame* srcFrame)
    {
      ffmpeg.sws_scale(_ctx,
        srcFrame->data,
        srcFrame->linesize,
        0,
        srcFrame->height,
        _dstData,
        _dstLinesize);

      var data = new byte_ptrArray8();
      data.UpdateFrom(_dstData);
      var linesize = new int_array8();
      linesize.UpdateFrom(_dstLinesize);

      var outFrame = ffmpeg.av_frame_alloc();
      outFrame->data = data;
      outFrame->linesize = linesize;
      outFrame->height = Dst.Height;
      outFrame->width = Dst.Width;
      outFrame->format = (int) _dstFmt;

      return outFrame;
    }
  }
}