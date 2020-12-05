using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using monakS.BackgroundServices;
using monakS.Data;
using monakS.FFMPEG;
using monakS.Hubs;
using monakS.Models;

namespace monakS.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class CameraController : ControllerBase
  {
    private readonly CameraStreamPool _cameraStreamPool;
    private readonly MessageEventBus _eventBus;
    private readonly AppDbContext _ctx;

    public CameraController(CameraStreamPool cameraStreamPool, MessageEventBus eventBus, AppDbContext ctx)
    {
      _cameraStreamPool = cameraStreamPool;
      _eventBus = eventBus;
      _ctx = ctx;
    }
    
    [HttpGet("image/{id}")]
    public IActionResult GetImage(int id)
    {
      // var bytes = Startup.ThumbnailJPEG;
      // return File(bytes, "image/webp");
      return Ok();
    }
    
    [HttpGet("{id}/active")]
    public IActionResult ActiveCaptures(int id)
    {
      Console.WriteLine(id);
      
      var result = _ctx.CaptureInfos
        .AsQueryable()
        .Include(c => c.Cam)
        .Where(c => c.Cam.Id == id && c.IsActive)
        .ToArray();
      
      return Ok(result);
    }

    [HttpGet()]
    public IActionResult Index()
    {
      return Ok(_ctx.Cameras.ToArray());
    }

    [HttpPost()]
    public IActionResult Add(Camera cam)
    {
      cam.SetupMode = true;
      _ctx.Cameras.Add(cam);
      _ctx.SaveChanges();

      return Ok(cam);
    }

    [HttpGet("record")]
    public IActionResult Record()
    {
      var cam = _ctx.Cameras.First(); 
      _eventBus.Publish(new CaptureStartRequestMessage() { Cam = cam, Trigger = CaptureTrigger.Manual});
      return Ok();
    }
    
    [HttpGet("recordc")]
    public IActionResult RecordC()
    {
      var cam = _ctx.Cameras.First(); 
      _eventBus.Publish(new CaptureStopRequestMessage() { Cam = cam, Trigger = CaptureTrigger.Manual});
      return Ok();
    }
    
    [HttpGet("cancel")]
    public IActionResult Cancel()
    {
      _cameraStreamPool.Stop(_ctx.Cameras.First());
      return Ok();
    }
  }
}