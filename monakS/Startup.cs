using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using monakS.FFMPEG;
using Newtonsoft.Json.Converters;
using SIPSorcery.Net;
using monakS.Hubs;
using monakS.Models;

namespace monakS
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddControllers().AddNewtonsoftJson(options =>
        options.SerializerSettings.Converters.Add(new StringEnumConverter()));
      services.AddSingleton<CameraStreamPool>();
      services.AddControllers();
      services.AddSignalR().AddNewtonsoftJsonProtocol();
    }
    
    public static List<Camera> CAMERAS = new List<Camera>()
    {
      new Camera()
      {
        Id = 0,
        Name = "Wohnzimmer",
        StreamUrl = @"rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov"
      },
      new Camera()
      {
        Id = 1,
        Name = "Flur",
        StreamUrl = @"rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov"
      },
      new Camera()
      {
        Id = 2,
        Name = "Hof",
        StreamUrl = @"rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov"
      },
      new Camera()
      {
        Id = 3,
        Name = "Keller",
        StreamUrl = @"rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov"
      },
    };

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app,
      IWebHostEnvironment env, CameraStreamPool cameraStreamPool)
    {
      // var pc = new RTCPeerConnection(null);
      // var r = new WeakReference(pc);
      // WebRtcSignalHub.POOL.Enqueue(pc);
      // pc = null;

      Parallel.For(0, 10, i =>
      {
        WebRtcSignalHub.POOL.Enqueue(new RTCPeerConnection(null));
      });
      
      foreach (var camera in CAMERAS)
      {
        cameraStreamPool.Start(camera);
        Console.WriteLine("STARTED");
      }

      Task.Run(() =>
      {
        while (true)
        {
          //GC.Collect();
          var proc = Process.GetCurrentProcess();
          Console.WriteLine("Working set {0} KB", proc.WorkingSet64 / 1024);
          // Console.WriteLine("Active WEBRTC peers: {0}", WebRtcSignalHub.ACTIVE_CONNECTIONS);
          Console.WriteLine($"WebRtc pool count: {WebRtcSignalHub.POOL.Count}");
          //Console.WriteLine($"REF: {r.IsAlive}");

          if (WebRtcSignalHub.POOL.Count < 500)
          {
            WebRtcSignalHub.POOL.Enqueue(new RTCPeerConnection(null));
          }

          Thread.Sleep(2500);
        }
      });

      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.UseFileServer();

      app.UseRouting();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
        endpoints.MapHub<WebRtcSignalHub>("/msg");
      });
    }
  }
}