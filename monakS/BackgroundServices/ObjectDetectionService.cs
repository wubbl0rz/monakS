using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
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
      _cameraStreamPool.Started += (cam, output) =>
      {
        if (cam.IsObjectDetectionEnabled)
        {
          var objectDetector = new ObjectDetector();
          objectDetector.Detected += (summary, pkt) =>
          {
            _eventBus.Publish(new DetectionResultMessage() { Cam = cam, Summary = summary});
          };

          var handle = output.Subscribe(pkt =>
          {
            var checkTime = objectDetector.LastDetectionCheck.AddMilliseconds(1800);
            if (pkt.IsKeyframe && checkTime <= DateTime.Now)
            {
              objectDetector.Detect(pkt);
            }
          });
        }
      };
      
      return Task.CompletedTask;
    }
  }
}