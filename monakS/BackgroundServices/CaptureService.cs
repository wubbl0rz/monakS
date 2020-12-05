using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Subscriptions;
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
    public CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();
    public Task<bool> Task { get; set; }
    public Camera Cam { get; set; }
    public CaptureTrigger Trigger { get; set; }
    public CaptureInfo Info { get; set; }

    public CaptureJob(Camera cam, CaptureTrigger trigger)
    {
      this.Cam = cam;
      this.Trigger = trigger;

      if (trigger == CaptureTrigger.Motion)
      {
        this.TokenSource.CancelAfter(30000);
      }

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
    private readonly ILogger<CaptureService> _log;
    private readonly IServiceProvider _serviceProvider;
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

    public CaptureService(MessageEventBus eventBus,
      ILogger<CaptureService> log, IServiceProvider serviceProvider)
    {
      _eventBus = eventBus;
      _log = log;
      _serviceProvider = serviceProvider;
    }

    private async void HandleCameraStarted(CameraStartedMessage msg)
    {
      var output = msg.Output;
      var cam = msg.Cam;

      var replay = output
        .Replay(TimeSpan.FromSeconds(11), Scheduler.Default);

      replay.Connect();

      _timeshiftBuffers.TryAdd(cam.Id, replay);

      // geht das ?
      await replay.LastOrDefaultAsync();

      _timeshiftBuffers.TryRemove(cam.Id, out _);
    }

    private async void HandleStartRequest(CaptureStartRequestMessage msg)
    {
      var cam = msg.Cam;
      var trigger = msg.Trigger;
      var key = (cam.Id, trigger);

      if (!_timeshiftBuffers.TryGetValue(cam.Id, out var buffer))
        return;
      
      //src.Token.Register()

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

      //todo: include this in capturejob
      job.Task = this.StartCapture(captureInfo.Name, buffer, job.TokenSource.Token);

      this.SendEvent(captureInfo);

      var result = await job.Task;

      _active.TryRemove(key, out _);

      captureInfo.HasError = !result;
      captureInfo.End = DateTime.Now;
      captureInfo.IsActive = false;

      this.SendEvent(captureInfo);
    }

    private void SendEvent(CaptureInfo info)
    {
      using var scope = _serviceProvider.CreateScope();
      using var ctx = scope.ServiceProvider.GetService<AppDbContext>();
      
      var entity = ctx.Update(info);
      ctx.SaveChanges();

      _eventBus.Publish(new CaptureStatusMessage()
      {
        Info = info,
      });
    }

    private async void HandleStopRequest(CaptureStopRequestMessage msg)
    {
      var cam = msg.Cam;
      var trigger = msg.Trigger;
      var key = (cam.Id, trigger);

      if (_active.TryGetValue(key, out var job))
      {
        await job.Cancel();
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