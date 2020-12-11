using System.IO;
using System.Threading.Tasks;
using HotChocolate;
using monakS.FFMPEG;
using monakS.Hubs;
using monakS.Models;

namespace monakS.Data.GraphQL
{
  public class Mutation
  {
    public async Task<Camera> AddCameraAsync(
      [Service]AppDbContext ctx, 
      [Service]CameraStreamPool cameraStreamPool, 
      [Service]MessageEventBus eventBus, 
      string name, string url, string user, string password)
    {
      var cam = new Camera()
      {
        Name = name,
        User = user,
        Password = password,
        StreamUrl = url,
        SetupMode = true,
        IsObjectDetectionEnabled = false,
      };

      await ctx.AddAsync(cam);
      await ctx.SaveChangesAsync();

      var tcs = new TaskCompletionSource<bool>();

      eventBus.Subscribe<CameraFailedMessage>(msg =>
      {
        if (msg.Cam.Id == cam.Id)
          tcs.TrySetResult(false);
      });

      eventBus.Subscribe<CameraStartedMessage>(msg =>
      {
        if (msg.Cam.Id == cam.Id)
          tcs.TrySetResult(true);
      });

      if (cameraStreamPool.Start(cam) && await tcs.Task)
      {
        return cam;
      }

      cameraStreamPool.Stop(cam);
      ctx.Remove(cam);
      await ctx.SaveChangesAsync();

      throw new IOException("No connection to camera!");
    }
  }
}