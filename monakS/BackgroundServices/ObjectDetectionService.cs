using System;
using System.IO;
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
    private readonly IHubContext<SignalHub> _hubContext;
    private readonly ILogger<ObjectDetectionService> _log;

    public ObjectDetectionService(CameraStreamPool cameraStreamPool, 
      IHubContext<SignalHub> hubContext, ILogger<ObjectDetectionService> log)
    {
      _cameraStreamPool = cameraStreamPool;
      _hubContext = hubContext;
      _log = log;
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _cameraStreamPool.Started += async cam =>
      {
        if (cam.IsObjectDetectionEnabled)
        {
          var objectDetector = new ObjectDetector();
          objectDetector.Detected += (summary, pkt) =>
          {
            _hubContext.Clients.All.SendAsync("detected", cam, summary);
          };

          var output = _cameraStreamPool.GetOutput(cam);
      
          await foreach (var pkt in output.ReadAllAsync(stoppingToken))
          {
            // subscribe camera changed event
            // mal mit rx extension ausprobieren
            // await 
            if (pkt.IsKeyframe)
            {
              objectDetector.Detect(pkt);
            }
          }
          
          output.Close();
        }
      };
      
      return Task.CompletedTask;
    }
  }
}