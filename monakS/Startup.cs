using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Timers;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using monakS.BackgroundServices;
using monakS.Data;
using monakS.Data.GraphQL;
using monakS.FFMPEG;
using monakS.Hubs;
using monakS.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SIPSorcery.Net;

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
      // services.AddSingleton<BackgroundWorkerService>();
      // services.AddSingleton<IHostedService>(p => p.GetService<BackgroundWorkerService>());

      services
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .AddFiltering()
        .AddSorting()
        .AddSubscriptionType<Subscription>()
        .AddInMemorySubscriptions();

      services.AddControllers().AddNewtonsoftJson(options =>
        options.SerializerSettings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy())));
      services.AddSignalR().AddNewtonsoftJsonProtocol(options =>
        options.PayloadSerializerSettings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy())));

      services.AddSingleton<CameraStreamPool>();
      services.AddSingleton<MessageEventBus>();
      services.AddHostedService<ObjectDetectionService>();
      services.AddHostedService<CaptureService>();
      services.AddDbContext<AppDbContext>();
      
      services.AddCors(options =>
      {
        options.AddPolicy("allow_all",
          builder =>
          {
            builder.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed((host) => true)
              .AllowCredentials();
          });
      });

      services.AddControllers();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app,
      IWebHostEnvironment env, 
      CameraStreamPool cameraStreamPool, 
      AppDbContext ctx,
      [Service] ITopicEventReceiver eventReceiver,
      [Service] ITopicEventSender eventSender
      )
    {
      // var pc = new RTCPeerConnection(null);
      // var r = new WeakReference(pc);
      // WebRtcSignalHub.POOL.Enqueue(pc);
      // pc = null;

      //ffmpeg.av_log_set_level(ffmpeg.AV_LOG_QUIET);

      ctx.Database.EnsureDeleted();
      ctx.Database.EnsureCreated();

      var CAMERAS = new List<Camera>()
      {
        new Camera()
        {
          Name = "Wohnzimmer",
          StreamUrl = @"rtsp://192.168.2.120:8554/mystream",
          IsObjectDetectionEnabled = true,
        },
        new Camera()
        {
          Name = "Wohnzimmer",
          StreamUrl = @"rtsp://192.168.2.120:8554/mystream"
        },
        new Camera()
        {
          Name = "Wohnzimmer",
          StreamUrl = @"rtsp://192.168.2.120:8554/mystream"
        },
        new Camera()
        {
          Name = "Wohnzimmer",
          StreamUrl = @"rtsp://192.168.2.120:8554/mystream"
        },
        new Camera()
        {
          Name = "Wohnzimmer",
          StreamUrl = @"rtsp://192.168.2.120:8554/mystream"
        },
      };

      foreach (var cam in CAMERAS)
      {
        ctx.Cameras.Add(cam);
      }

      ctx.SaveChanges();
      
      
      //Parallel.For(0, 200, i => { MessageHub.POOL.Enqueue(new RTCPeerConnection(conf)); });

      foreach (var camera in ctx.Cameras.ToArray())
      {
        cameraStreamPool.Start(camera);
      }

      var timer = new Timer {AutoReset = false, Interval = 1000};
      timer.Elapsed += (_, args) =>
      {
        //GC.Collect();
        var proc = Process.GetCurrentProcess();
        Console.WriteLine("Working set {0} KB", proc.WorkingSet64 / 1024);
        // Console.WriteLine("Active WEBRTC peers: {0}", WebRtcSignalHub.ACTIVE_CONNECTIONS);
        Console.WriteLine($"WebRtc pool count: {MessageHub.POOL.Count}");

        //Console.WriteLine($"REF: {r.IsAlive}");

        // if (MessageHub.POOL.Count < 200)
        // {
        //   for (int i = 0; i < 10; i++)
        //   {
        //     MessageHub.POOL.Enqueue(new RTCPeerConnection(conf));
        //   }
        // }
        
        timer.Start();
      };
      timer.Start();

      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.UseFileServer();
      
      app.UseRouting();
      
      app.UseCors("allow_all");

      app.UseWebSockets();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapGraphQL();
        endpoints.MapControllers();
        endpoints.MapHub<MessageHub>("/msg");
      });
    }
  }
}