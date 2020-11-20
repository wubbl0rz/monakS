using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using monakS.BackgroundServices;
using monakS.FFMPEG;
using monakS.Hubs;

namespace monakS.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class CameraController : ControllerBase
  {
    private readonly CameraStreamPool _cameraStreamPool;
    private readonly MessageEventBus _eventBus;

    public CameraController(CameraStreamPool cameraStreamPool, MessageEventBus eventBus)
    {
      _cameraStreamPool = cameraStreamPool;
      _eventBus = eventBus;
    }
    
    [HttpGet("image/{id}")]
    public IActionResult GetImage(int id)
    {
      // var bytes = Startup.ThumbnailJPEG;
      // return File(bytes, "image/webp");
      return Ok();
    }

    [HttpGet()]
    public IActionResult Index()
    {
      return Ok(Startup.CAMERAS);
    }
    
    [HttpGet("record")]
    public IActionResult Record()
    {
      var cam = Startup.CAMERAS.First();
      Console.WriteLine($"CONTROLLER: {cam.Id}");
      _eventBus.Publish(new CaptureStartRequestMessage() { Cam = cam, Trigger = CaptureTrigger.Motion});
      return Ok();
    }
    
    [HttpGet("cancel")]
    public IActionResult Cancel()
    {
      _cameraStreamPool.Stop(Startup.CAMERAS.First());
      return Ok();
    }
  }
}