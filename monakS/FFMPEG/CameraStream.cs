using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using monakS.Hubs;
using monakS.Models;


namespace monakS.FFMPEG
{
  public class CameraStreamPool
  {
    private readonly ILogger<CameraStreamPool> _log;
    private readonly MessageEventBus _eventBus;
    
    private readonly ConcurrentDictionary<int, CameraStream> _streams =
      new ConcurrentDictionary<int, CameraStream>();

    public CameraStreamInfo GetOutputInfo(Camera cam)
    {
      var info = new CameraStreamInfo();

      if (_streams.TryGetValue(cam.Id, out var cameraStream))
      {
        info.CodecParameters = cameraStream.CodecParameters;
        info.TimeBase = cameraStream.TimeBase;
      }

      return info;
    }

    public IObservable<AVPacketHandle> GetOutput(Camera cam)
    {
      if (_streams.TryGetValue(cam.Id, out var cameraStream))
      {
        return cameraStream;
      }

      throw new IOException("Camera not found");
    }

    public CameraStreamPool(ILogger<CameraStreamPool> log, MessageEventBus eventBus)
    {
      _log = log;
      _eventBus = eventBus;
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

      if (!created)
        return false;

      var connected = false;

      cameraStream.OnConnected += () =>
      {
        _log.LogInformation($"Connected: {cam.StreamUrl} ({cam.Name})");
        if (!connected)
        {
          connected = true;
          _eventBus.Publish(new CameraStartedMessage()
          {
            Cam = cam,
            Info = new CameraStreamInfo()
            {
              CodecParameters = cameraStream.CodecParameters,
              TimeBase = cameraStream.TimeBase
            },
            Output = cameraStream
          });
        }
      };

      Task.Run(async () =>
      {
        while (true)
        {
          _log.LogInformation($"Connecting: {cam.StreamUrl} ({cam.Name})");

          try
          {
            await Task.Run(cameraStream.Loop);
            break;
          }
          catch (Exception ex)
          {
            _log.LogWarning(ex.Message);
            cameraStream.Reset();
          }

          await Task.Delay(5000);
        }

        _streams.TryRemove(cam.Id, out _);
        cameraStream.Dispose();
      });

      return true;
    }
  }

  public class CameraStreamInfo
  {
    public AVCodecParametersHandle CodecParameters { get; set; }
    public AVRational TimeBase { get; set; }
  }

  interface ICameraStream
  {
    public CameraStreamInfo Info { get; set; }
  }

  public class CameraStream : IDisposable, IObservable<AVPacketHandle>
  {
    private readonly Camera _cam;

    public event Action<AVPacketHandle> OnNextFrame;
    public event Action OnConnected;
    public event Action OnDispose;

    public AVCodecParametersHandle CodecParameters { get; private set; }
    public AVRational TimeBase { get; private set; }

    private unsafe AVPacket* _pkt;
    private unsafe AVFormatContext* _inputCtx;
    private unsafe AVStream* _stream;
    private double _frameRate;
    private int _streamIndex;

    private AVIOInterruptCB_callback _timeoutCallback;
    private readonly Stopwatch _timeoutCounter = new Stopwatch();

    private unsafe int CheckTimeout(void* p)
    {
      return _timeoutCounter.Elapsed.Seconds > 15 ? 1 : 0;
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
      _cancel = true;
    }

    public unsafe void Loop()
    {
      Console.WriteLine("Start connect: " + _cam.StreamUrl);
      AVDictionary* dict = null;
      ffmpeg.av_dict_set(&dict, "rtsp_transport", "tcp", 0);
      ffmpeg.av_dict_set(&dict, "stimeout", $"{TimeSpan.FromSeconds(15).TotalMilliseconds * 1000}", 0);

      var inputCtx = ffmpeg.avformat_alloc_context();

      _timeoutCounter.Start();
      inputCtx->interrupt_callback.callback = _timeoutCallback;

      // will free inputCtx on failure automatically
      if (ffmpeg.avformat_open_input(&inputCtx, _cam.StreamUrl, null, &dict) != 0)
      {
        ffmpeg.av_dict_free(&dict);
        throw new Exception("Cannot connect to: " + _cam.StreamUrl);
      }

      ffmpeg.av_dict_free(&dict);

      _inputCtx = inputCtx;

      _timeoutCounter.Restart();

      ffmpeg.avformat_find_stream_info(inputCtx, null);

      _timeoutCounter.Stop();

      for (var i = 0; i < inputCtx->nb_streams; i++)
      {
        var stream = inputCtx->streams[i];
        if (stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
        {
          _streamIndex = stream->index;
          _stream = stream;
          _frameRate = ffmpeg.av_q2d(stream->r_frame_rate);
          break;
        }
      }

      if (_stream == null)
      {
        ffmpeg.avformat_close_input(&inputCtx);
        throw new Exception("No VIDEO STREAM found");
      }

      _pkt = ffmpeg.av_packet_alloc();
      var lastTimestamp = 0L;

      this.CodecParameters = new AVCodecParametersHandle(_stream->codecpar);
      this.TimeBase = _stream->time_base;

      this.OnConnected?.Invoke();

      while (!_cancel)
      {
        ffmpeg.av_packet_unref(_pkt);
        //ffmpeg.av_init_packet(pkt);

        _timeoutCounter.Restart();

        if (ffmpeg.av_read_frame(inputCtx, _pkt) != 0)
        {
          // ffmpeg.av_packet_free(&pkt);
          // ffmpeg.avformat_close_input(&inputCtx);
          throw new Exception("Error reading STREAM: " + _cam.StreamUrl);
        }

        _timeoutCounter.Stop();

        if (_pkt->stream_index != _streamIndex || _pkt->pts < 0)
        {
          //ffmpeg.av_packet_unref(_pkt);
          continue;
        }

        if (_pkt->pts <= lastTimestamp)
        {
          //ffmpeg.av_packet_unref(_pkt);
          Console.WriteLine("WRONG TS ... " + _cam.Id);
          continue;
        }

        lastTimestamp = _pkt->pts;

        _pkt->duration = _pkt->duration == 0 ? (long) (90000 / _frameRate) : _pkt->duration;

        var seconds = _pkt->duration *
                 (_stream->time_base.num / (float) _stream->time_base.den);

        OnNextFrame?.Invoke(new AVPacketHandle(_pkt,
          _stream->codecpar->width,
          _stream->codecpar->height,
          seconds,
          this.TimeBase,
          this.CodecParameters));

        ffmpeg.av_packet_unref(_pkt);
      }
    }

    public void Dispose()
    {
      this.Reset();
      this.OnDispose?.Invoke();
    }

    public IDisposable Subscribe(IObserver<AVPacketHandle> observer)
    {
      void Write(AVPacketHandle pkt)
      {
        observer.OnNext(pkt);
      }

      void Close()
      {
        observer.OnCompleted();
        this.OnNextFrame -= Write;
      }

      this.OnNextFrame += Write;
      this.OnDispose += Close;
      
      return Disposable.Create(Close);
    }
  }
}