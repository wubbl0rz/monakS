using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace monakS.FFMPEG
{
  public class ObjectDetectorResult
  {
    public string Top { get; set; }
    public string Bottom { get; set; }
    public string Right { get; set; }
    public string Left { get; set; }
    public string Label { get; set; }
    public double Confidence { get; set; }
  }

  public class ObjectDetectorSummary
  {
    public List<ObjectDetectorResult> Detections { get; set; } = new List<ObjectDetectorResult>();
  }

  public class ObjectDetector : IDisposable
  {
    private readonly Channel<AVPacketHandle> _images = Channel.CreateBounded<AVPacketHandle>(1);
    private bool _initialized = false;
    private FrameResizer _resizer;
    private unsafe AVCodecContext* _h264DecCtx;
    private unsafe AVCodecContext* _jpgEncCtx;

    public DateTime LastSuccessfulDetection { get; private set; }
    public DateTime LastDetectionCheck { get; private set; }
    public event Action<ObjectDetectorSummary, AVPacketHandle> Detected;

    public ObjectDetector()
    {
      this.Loop();
    }

    private async void Loop()
    {
      var httpClient = new HttpClient();

      await foreach (var pkt in _images.Reader.ReadAllAsync())
      {
        var jpgImage = this.DecodeAndResize(pkt);

        var json = new JObject
        {
          {"detector_name", "default"},
          {"data", Convert.ToBase64String(jpgImage.Data)},
          {"detect", new JObject(new JProperty("*", 60))}
        };

        try
        {
          var response = await httpClient.PostAsync(
            "http://localhost:8080/detect",
            new StringContent(json.ToString(), Encoding.UTF8, "application/json"));
          
          var summary = JsonConvert.DeserializeObject<ObjectDetectorSummary>(
            await response.Content.ReadAsStringAsync());

          this.LastDetectionCheck = DateTime.Now;

          if (summary.Detections.Count > 0)
          {
            this.LastSuccessfulDetection = this.LastDetectionCheck;
            this.Detected?.Invoke(summary, jpgImage);
          }
        }
        catch (Exception)
        {
          //TODO: logger error
        }
      }
    }

    private unsafe void InitDecoderEncoder(AVPacketHandle pkt)
    {
      var decoder = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
      var encoder = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_MJPEG);

      _resizer = new FrameResizer(new Size(pkt.Width, pkt.Height),
        new Size(300, 300),
        AVPixelFormat.AV_PIX_FMT_YUV420P,
        false);

      _h264DecCtx = ffmpeg.avcodec_alloc_context3(decoder);
      _jpgEncCtx = ffmpeg.avcodec_alloc_context3(encoder);

      _jpgEncCtx->bit_rate = 400000;
      _jpgEncCtx->width = _resizer.Dst.Width;
      _jpgEncCtx->height = _resizer.Dst.Height;
      _jpgEncCtx->time_base = new AVRational() {den = 1, num = 1};
      _jpgEncCtx->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUVJ420P;

      ffmpeg.avcodec_open2(_h264DecCtx, decoder, null);
      ffmpeg.avcodec_open2(_jpgEncCtx, encoder, null);
    }

    private unsafe AVPacketHandle DecodeAndResize(AVPacketHandle pkt)
    {
      if (!_initialized)
      {
        this.InitDecoderEncoder(pkt);
        _initialized = true;
      }

      var frameSrc = ffmpeg.av_frame_alloc();
      var outPkt = AVPacketHandle.Empty;
      
      if (!FFMPEGHelper.Try(ffmpeg.avcodec_send_packet(_h264DecCtx, pkt.GetRawPacket())) ||
          !FFMPEGHelper.Try(ffmpeg.avcodec_receive_frame(_h264DecCtx, frameSrc)))
      {
        ffmpeg.av_frame_free(&frameSrc);
        return outPkt;
      }
      
      var frameResized = _resizer.Resize(frameSrc);
      var thumbPkt = ffmpeg.av_packet_alloc();

      if (FFMPEGHelper.Try(ffmpeg.avcodec_send_frame(_jpgEncCtx, frameResized)) &&
          FFMPEGHelper.Try(ffmpeg.avcodec_receive_packet(_jpgEncCtx, thumbPkt)))
      {
        outPkt = new AVPacketHandle(thumbPkt, _resizer.Dst.Width, _resizer.Dst.Height, 0);
      }

      ffmpeg.av_packet_free(&thumbPkt);
      ffmpeg.av_frame_free(&frameResized);
      ffmpeg.av_frame_free(&frameSrc);

      return outPkt;
    }

    // TODO await bool ?
    public void Detect(AVPacketHandle pkt,
      Action<ObjectDetectorSummary, AVPacketHandle> detectedCallback = null)
    {
      _images.Writer.TryWrite(pkt);
    }

    public void Dispose()
    {
      //cleanup
    }
  }

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

  public static class FFMPEGHelper
  {
    public static unsafe bool Try(int res)
    {
      if (res == 0)
        return true;

      fixed (byte* buffer = new byte[2048])
      {
        ffmpeg.av_strerror(res, buffer, 2048);
        var str = System.Text.Encoding.Default.GetString(buffer, 2048);
        Console.WriteLine(str);
      }

      return false;
    }
  }
}