using System;
using System.Linq;
using System.Net;
using System.Security;
using Microsoft.AspNetCore.Mvc;
using monakS.FFMPEG;
using monakS.Models;
using Newtonsoft.Json;

namespace monakS.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class CameraController : ControllerBase
  {
    private readonly CameraStreamPool _cameraStreamPool;

    public CameraController(CameraStreamPool cameraStreamPool)
    {
      _cameraStreamPool = cameraStreamPool;
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
    
    [HttpGet("cancel")]
    public IActionResult Cancel()
    {
      _ = _cameraStreamPool.Stop(Startup.CAMERAS.First());
      return Ok();
    }
  }
}