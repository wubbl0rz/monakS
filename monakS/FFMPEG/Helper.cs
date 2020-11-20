using System;
using System.Text;
using FFmpeg.AutoGen;

namespace monakS.FFMPEG
{
  public static class FFMPEGHelper
  {
    public static unsafe bool Try(int res)
    {
      if (res == 0)
        return true;

      fixed (byte* buffer = new byte[2048])
      {
        ffmpeg.av_strerror(res, buffer, 2048);
        var str = Encoding.Default.GetString(buffer, 2048);
        Console.WriteLine(str);
      }

      return false;
    }
  }
}