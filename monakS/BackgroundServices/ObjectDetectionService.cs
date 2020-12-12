using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using monakS.FFMPEG;
using monakS.Hubs;
using monakS.Models;

namespace monakS.BackgroundServices
{
  public class ObjectDetectionService : BackgroundService
  {
    private readonly CameraStreamPool _cameraStreamPool;
    private readonly MessageEventBus _eventBus;
    private readonly ILogger<ObjectDetectionService> _log;

    private CancellationToken shutdownToken;

    public ObjectDetectionService(CameraStreamPool cameraStreamPool, 
      MessageEventBus eventBus, ILogger<ObjectDetectionService> log)
    {
      _cameraStreamPool = cameraStreamPool;
      _eventBus = eventBus;
      _log = log;
    }
    
    private ConcurrentDictionary<Camera, CancellationTokenSource> _active = 
      new ConcurrentDictionary<Camera, CancellationTokenSource>();

    private void StopDetection(int camId)
    {
      var (cam, src) = 
        _active.FirstOrDefault(c => c.Key.Id == camId);
      
      if (cam != null)
      {
        src.Cancel();
      }
    }

    private async void StartDetection(Camera cam)
    {
      if (cam.IsObjectDetectionEnabled)
      {
        var src = CancellationTokenSource.CreateLinkedTokenSource(this.shutdownToken);
        if (!_active.TryAdd(cam, src))
          return;
        
        var output = 
          _cameraStreamPool.GetOutput(cam).ObserveOn(Scheduler.Default);
          
        using var objectDetector = new ObjectDetector();
          
        objectDetector.Detected += (summary, pkt) =>
        {
          _eventBus.Publish(new DetectionResultMessage() { Cam = cam, Summary = summary});
          // _eventBus.Publish(new CaptureStartRequestMessage()
          // {
          //   Trigger = CaptureTrigger.Motion,
          //   Cam = cam
          // });
        };
      
        objectDetector.OnError += error =>
        {
          _log.LogError(error);
        };
      
        try
        { 
          await foreach (var pkt in output.
            ToAsyncEnumerable().WithCancellation(src.Token))
          {
            if (pkt.IsKeyframe && 
                objectDetector.LastDetectionCheck.AddMilliseconds(1800) <= DateTime.Now)
            {
              objectDetector.Detect(pkt);
            }
          }
        }
        catch (Exception e) when 
          (e is TaskCanceledException || e is OperationCanceledException)
        {
        }

        _active.TryRemove(cam, out _);
      }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
      this.shutdownToken = stoppingToken;
      
      _eventBus.Subscribe<CameraStartedMessage>(msg =>
      {
        if (msg.Cam.IsObjectDetectionEnabled)
          this.StartDetection(msg.Cam);
      });
      
      _eventBus.Subscribe<CameraUpdatedMessage>(msg =>
      {
        var oldEnabled = msg.OldCam?.IsObjectDetectionEnabled ?? false; 
        var updatedEnabled = msg.UpdatedCam?.IsObjectDetectionEnabled ?? false;
        
        if (oldEnabled && !updatedEnabled)
        {
          this.StopDetection(msg.OldCam.Id);
        }
        else if (!oldEnabled && updatedEnabled)
        {
          this.StartDetection(msg.UpdatedCam);
        }
      });
      
      return Task.CompletedTask;
    }
  }
}