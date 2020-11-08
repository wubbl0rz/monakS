using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using monakS.Hubs;
using monakS.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace monakS.FFMPEG
{
  public class CameraStreamPool
  {
    private readonly ILogger<CameraStreamPool> _log;
    private readonly IHubContext<WebRtcSignalHub> _hubContext;

    private readonly ConcurrentDictionary<int, CameraStream> _streams =
      new ConcurrentDictionary<int, CameraStream>();

    public void GetOutput(Camera cam, Action<AVPacketHandle> callback)
    {
      if (_streams.TryGetValue(cam.Id, out var cameraStream))
      {
        cameraStream.OnNextFrame += callback;
      }
    }

    public CameraStreamPool(ILogger<CameraStreamPool> log, IHubContext<WebRtcSignalHub> hubContext)
    {
      _log = log;
      _hubContext = hubContext;
    }

    public void Stop(Camera cam)
    {
      if (_streams.TryRemove(cam.Id, out var cameraStream))
      {
        cameraStream.Break();
      }
    }

    public bool Start(Camera cam)
    {
      var cameraStream = new CameraStream(cam);

      var created = _streams.TryAdd(cam.Id, cameraStream);

      Task.Run(async () =>
      {
        while (created)
        {
          try
          {
            await Task.Run(cameraStream.Loop);
            break;
          }
          catch (Exception ex)
          {
            _log.LogWarning(ex.Message);
          }
          finally
          {
            cameraStream.Reset();
          }

          await Task.Delay(5000);
        }

        if (created)
        {
          _streams.TryRemove(cam.Id, out _);
        }
      });

      return created;
    }
  }

  public class CameraStream
  {
    private readonly Camera _cam;

    public event Action<AVPacketHandle> OnNextFrame;
    public event Action<AVPacketHandle> OnKeyFrame;

    private unsafe AVPacket* _pkt;
    private unsafe AVFormatContext* _inputCtx;
    private unsafe AVStream* _stream;
    private double _frameRate;
    private int _streamIndex;

    private AVIOInterruptCB_callback _timeoutCallback;
    private readonly Stopwatch _timeoutCounter = new Stopwatch();

    private unsafe int CheckTimeout(void* p)
    {
      return _timeoutCounter.Elapsed.Seconds > 5 ? 1 : 0;
    }

    public unsafe CameraStream(Camera cam)
    {
      _cam = cam;
      _timeoutCallback = CheckTimeout;
    }

    public void Reset()
    {
      _timeoutCounter.Reset();

      unsafe
      {
        fixed (AVFormatContext** tmpCtx = &_inputCtx)
        {
          ffmpeg.avformat_close_input(tmpCtx);
        }

        fixed (AVPacket** tmpPkt = &_pkt)
        {
          ffmpeg.av_packet_free(tmpPkt);
        }
      }
    }

    private bool _cancel = false;

    public void Break()
    {
      var taskCompletionSource = new TaskCompletionSource<bool>();
      _cancel = true;
    }

    public unsafe void Loop()
    {
      ffmpeg.av_log_set_level(ffmpeg.AV_LOG_QUIET);
      AVDictionary* dict = null;
      ffmpeg.av_dict_set(&dict, "rtsp_transport", "tcp", 0);
      ffmpeg.av_dict_set(&dict, "stimeout", $"{TimeSpan.FromSeconds(5).TotalMilliseconds * 1000}", 0);

      var inputCtx = ffmpeg.avformat_alloc_context();

      _timeoutCounter.Start();
      inputCtx->interrupt_callback.callback = _timeoutCallback;

      // will free inputCtx on failure automatically
      if (ffmpeg.avformat_open_input(&inputCtx, _cam.StreamUrl, null, &dict) != 0)
      {
        throw new Exception("Cannot connect to: " + _cam.StreamUrl);
      }

      _inputCtx = inputCtx;

      _timeoutCounter.Restart();

      ffmpeg.avformat_find_stream_info(_inputCtx, null);

      _timeoutCounter.Stop();

      for (var i = 0; i < _inputCtx->nb_streams; i++)
      {
        var stream = _inputCtx->streams[i];
        if (stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
        {
          _streamIndex = stream->index;
          _stream = stream;
          _frameRate = ffmpeg.av_q2d(_stream->r_frame_rate);
          break;
        }
      }

      if (_stream == null)
      {
        throw new Exception("No VIDEO STREAM found");
      }

      var pkt = ffmpeg.av_packet_alloc();
      var lastTimestamp = 0L;

      Console.WriteLine($"Connected: {_cam.StreamUrl} ({_cam.Name})");

      while (!_cancel)
      {
        ffmpeg.av_init_packet(pkt);

        if (ffmpeg.av_read_frame(_inputCtx, pkt) != 0)
        {
          ffmpeg.av_packet_unref(pkt);
          throw new Exception("Error reading STREAM: " + _cam.StreamUrl);
        }

        if (pkt->stream_index != _streamIndex || pkt->pts < 0)
        {
          ffmpeg.av_packet_unref(pkt);
          continue;
        }

        if (pkt->pts <= lastTimestamp)
        {
          Console.WriteLine("WRONG TS ... " + _cam.Id);
          continue;
        }
        
        lastTimestamp = pkt->pts;

        pkt->duration = pkt->duration == 0 ? (long) (90000 / _frameRate) : pkt->duration;

        var eventPkt = new AVPacketHandle(pkt,
          _stream->codecpar->width,
          _stream->codecpar->height);

        if (pkt->flags == ffmpeg.AV_PKT_FLAG_KEY)
        {
          this.OnKeyFrame?.Invoke(eventPkt);
        }

        //Console.WriteLine(pkt->duration);

        OnNextFrame?.Invoke(eventPkt);

        ffmpeg.av_packet_unref(pkt);
      }
    }
  }
}