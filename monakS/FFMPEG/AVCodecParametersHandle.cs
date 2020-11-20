using FFmpeg.AutoGen;

namespace monakS.FFMPEG
{
  public class AVCodecParametersHandle
  {
    private readonly unsafe AVCodecParameters* _parameters;

    public unsafe AVCodecParameters* GetRawParameters()
    {
      return _parameters;
    }

    public unsafe AVCodecParametersHandle(AVCodecParameters* parameters)
    {
      this._parameters = ffmpeg.avcodec_parameters_alloc();
      ffmpeg.avcodec_parameters_copy(this._parameters, parameters);
    }

    ~AVCodecParametersHandle()
    {
      unsafe
      {
        fixed (AVCodecParameters** tmpRef = &this._parameters)
        {
          ffmpeg.avcodec_parameters_free(tmpRef);
        }
      }
    }
  }
}