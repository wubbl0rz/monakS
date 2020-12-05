using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using monakS.FFMPEG;
using monakS.Hubs;

namespace monakS.BackgroundServices
{
  public class ObjectDetectionService : BackgroundService
  {
    private readonly CameraStreamPool _cameraStreamPool;
    private readonly MessageEventBus _eventBus;
    private readonly ILogger<ObjectDetectionService> _log;

    public ObjectDetectionService(CameraStreamPool cameraStreamPool, 
      MessageEventBus eventBus, ILogger<ObjectDetectionService> log)
    {
      _cameraStreamPool = cameraStreamPool;
      _eventBus = eventBus;
      _log = log;
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _eventBus.Subscribe<CameraStartedMessage>(async msg =>
      {
        var cam = msg.Cam;
      
        if (cam.IsObjectDetectionEnabled)
        {
          var output = msg.Output.ObserveOn(Scheduler.Default);
          
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
            await foreach (var pkt in output.ToAsyncEnumerable().WithCancellation(stoppingToken))
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
        }
      });

      return Task.CompletedTask;
    }
  }
}