using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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

    [HttpGet()]
    public IActionResult Index()
    {
      return Ok(_ctx.Cameras.ToArray());
    }

    [HttpDelete]
    public IActionResult Remove(Camera cam)
    {
      _cameraStreamPool.Stop(cam);
      _ctx.Remove(cam);
      _ctx.SaveChanges();
      _eventBus.Publish(new CameraUpdatedMessage()
      {
        OldCam = cam,
        UpdatedCam = null
      });
      return Ok();
    }

    [HttpPut("{id}")]
    public IActionResult Update(Camera updatedCam)
    {
      if (updatedCam == null)
        return BadRequest();

      var cam = _ctx.Cameras.Find(updatedCam.Id);

      if (cam == null)
        return BadRequest();
      
      _ctx.Entry(cam).State = EntityState.Detached;

      _ctx.Update(updatedCam);
      _ctx.SaveChanges();

      _eventBus.Publish(new CameraUpdatedMessage()
      {
        OldCam = cam,
        UpdatedCam = updatedCam
      });
      
      return Ok(cam);
    }

    [HttpPost]
    public async Task<IActionResult> Add(Camera cam)
    {
      cam = new Camera()
      {
        Name = cam.Name,
        User = cam.User,
        StreamUrl = cam.StreamUrl,
        SetupMode = true,
        IsObjectDetectionEnabled = false,
      };

      await _ctx.AddAsync(cam);
      await _ctx.SaveChangesAsync();

      var tcs = new TaskCompletionSource<bool>();

      _eventBus.Subscribe<CameraFailedMessage>(msg =>
      {
        if (msg.Cam.Id == cam.Id)
          tcs.TrySetResult(false);
      });

      _eventBus.Subscribe<CameraStartedMessage>(msg =>
      {
        if (msg.Cam.Id == cam.Id)
          tcs.TrySetResult(true);
      });

      if (_cameraStreamPool.Start(cam) && await tcs.Task)
      {
        cam.SetupMode = false;
        await _ctx.SaveChangesAsync();
        _eventBus.Publish(new CameraUpdatedMessage()
        {
          UpdatedCam = cam
        });
        return Ok(cam);
      }

      _cameraStreamPool.Stop(cam);
      _ctx.Remove(cam);
      await _ctx.SaveChangesAsync();

      return BadRequest();
    }
  }
}