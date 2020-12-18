using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Threading.Channels;
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
    public List<ObjectDetectorResult> Detections { get; set; } =
      new List<ObjectDetectorResult>();
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
    public event Action<string> OnError;

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

        var serviceURL = @"http://localhost:8080/detect";
        
        try
        {
          var response = await httpClient.PostAsync(
            serviceURL,
            new StringContent(json.ToString(),
              Encoding.UTF8,
              "application/json"));

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
          this.OnError?.Invoke("Could not connect to: " + serviceURL);
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

    public void Detect(AVPacketHandle pkt)
    {
      _images.Writer.TryWrite(pkt);
    }

    public void Dispose()
    {
      //todo: cleanup
    }
  }
}