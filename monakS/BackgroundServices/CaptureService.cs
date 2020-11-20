using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using monakS.Data;
using monakS.FFMPEG;
using monakS.Hubs;
using monakS.Models;

namespace monakS.BackgroundServices
{
  public enum CaptureTrigger
  {
    Manual,
    Timer,
    Motion
  }

  public class CaptureInfo
  {
    public int Id { get; set; }
    public Camera Cam { get; set; }
    public string Name => $"{this.Cam.Name}_{this.Trigger.ToString()}_{this.Start:yyyy_MM_dd_HH_mm_ss}.mp4";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public CaptureTrigger Trigger { get; set; }
    public bool HasError { get; set; }
    public string ErrorMessage { get; set; }
    public bool IsActive { get; set; }
  }

  public class CaptureJob
  {
    public CancellationTokenSource TokenSource { get; set; }
    public Task Task { get; set; }
    public Camera Cam { get; set; }
    public CaptureTrigger Trigger { get; set; }
    public CaptureInfo Info { get; set; }

    public CaptureJob(Camera cam, CaptureTrigger trigger)
    {
      this.Cam = cam;
      this.Trigger = trigger;

      this.Info = new CaptureInfo()
      {
        Cam = cam,
        Start = DateTime.Now,
        Trigger = trigger,
        IsActive = true,
      };
    }

    public async Task Cancel()
    {
      this.TokenSource.Cancel();
      await this.Task;
    }
  }

  public class CaptureService : BackgroundService
  {
    private readonly MessageEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CaptureService> _log;
    private CancellationToken _shutdownToken;

    private readonly ConcurrentDictionary<int, IObservable<AVPacketHandle>> _timeshiftBuffers =
      new ConcurrentDictionary<int, IObservable<AVPacketHandle>>();

    private readonly ConcurrentDictionary<(int id, CaptureTrigger trigger), CaptureJob> _active =
      new ConcurrentDictionary<(int id, CaptureTrigger trigger), CaptureJob>();

    private async Task<bool> StartCapture(string fileName, IObservable<AVPacketHandle> buffer, CancellationToken token)
    {
      _log.LogInformation($"Start capture: {fileName}");

      using var outputFile = new VideoFile(fileName);

      var pkt = await buffer.FirstAsync();

      if (!outputFile.Open(pkt.CodecParameters, pkt.TimeBase))
      {
        _log.LogWarning($"Cannot open file {fileName}");
        return false;
      }

      try
      {
        var outputStream = buffer
          .Buffer(TimeSpan.FromSeconds(1))
          .ToAsyncEnumerable()
          .WithCancellation(token);

        await foreach (var packets in outputStream)
        {
          outputFile.Write(packets, token);
        }
      }
      catch (Exception e) when (e is TaskCanceledException || e is OperationCanceledException)
      {
        // ignored
      }

      _log.LogInformation($"Finished capture: {fileName}");

      return true;
    }

    public CaptureService(MessageEventBus eventBus, IServiceProvider serviceProvider, ILogger<CaptureService> log)
    {
      _eventBus = eventBus;
      _serviceProvider = serviceProvider;
      _log = log;
    }

    private async void HandleCameraStarted(CameraStartedMessage msg)
    {
      var output = msg.Output;
      var cam = msg.Cam;

      var replay = output
        .Replay(TimeSpan.FromSeconds(11), Scheduler.Default);

      replay.Connect();

      _timeshiftBuffers.TryAdd(cam.Id, replay);

      await replay.LastAsync();

      _timeshiftBuffers.TryRemove(cam.Id, out _);
    }

    private async void HandleStartRequest(CaptureStartRequestMessage msg)
    {
      var cam = msg.Cam;
      var trigger = msg.Trigger;
      var key = (cam.Id, trigger);

      if (!_timeshiftBuffers.TryGetValue(cam.Id, out var buffer))
        return;

      var src = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken);

      if (trigger == CaptureTrigger.Motion)
        src.CancelAfter(2000);
      
      var job = new CaptureJob(cam, trigger);
      var captureInfo = job.Info;

      if (!_active.TryAdd(key, job))
      {
        _active.TryGetValue(key, out var activeJob);
        //same cam with same trigger already active
        if (trigger == CaptureTrigger.Motion)
          activeJob?.TokenSource?.CancelAfter(TimeSpan.FromSeconds(30));
        _log.LogWarning($"Capture {cam.Name} {key} already in progress.");
        return;
      }

      var result = await this.StartCapture(captureInfo.Name, buffer, src.Token);

      _active.TryRemove(key, out _);

      captureInfo.HasError = !result;
      captureInfo.End = DateTime.Now;
      captureInfo.IsActive = false;

      this.SendAndSaveResult(captureInfo);
    }

    private void SendAndSaveResult(CaptureInfo info)
    {
      using var scope = _serviceProvider.CreateScope();
      using var ctx = scope.ServiceProvider.GetService<AppDbContext>();

      _eventBus.Publish(new CaptureResultMessage()
      {
        Cam = info.Cam,
        Info = info,
      });

      //ctx.CaptureInfos.Add(info); // lol changes id
      //ctx.SaveChanges();
    }

    private void HandleStopRequest(CaptureStopRequestMessage msg)
    {
      var cam = msg.Cam;
      var trigger = msg.Trigger;
      var key = (cam.Id, trigger);

      if (_active.TryGetValue(key, out var captureInfo))
      {
        captureInfo.TokenSource.Cancel();
      }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _shutdownToken = stoppingToken;

      _eventBus.Subscribe<CameraStartedMessage>(this.HandleCameraStarted);

      _eventBus.Subscribe<CaptureStartRequestMessage>(this.HandleStartRequest);

      _eventBus.Subscribe<CaptureStopRequestMessage>(this.HandleStopRequest);

      return Task.CompletedTask;
    }
  }
}