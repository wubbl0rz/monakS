using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Timers;
using FFmpeg.AutoGen;
using HotChocolate.AspNetCore;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
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
using Path = HotChocolate.Path;

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
        .AddMutationType<Mutation>() 
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
      AppDbContext ctx)
    {
      // ctx.Database.EnsureDeleted();
      ctx.Database.EnsureCreated();

      foreach (var capture in ctx.CaptureInfos)
      {
        capture.IsActive = false;
      }

      ctx.SaveChanges();

      // var pc = new RTCPeerConnection(null);
      // var r = new WeakReference(pc);
      // WebRtcSignalHub.POOL.Enqueue(pc);
      // pc = null;

      ffmpeg.av_log_set_level(ffmpeg.AV_LOG_QUIET);


      //Parallel.For(0, 200, i => { MessageHub.POOL.Enqueue(new RTCPeerConnection(conf)); });

      // ctx.Cameras.Add(new Camera()
      // {
      //   Name = "Test",
      //   StreamUrl = "rtsp://192.168.2.120:8554/mystream",
      //   IsObjectDetectionEnabled = true
      // });
      // ctx.SaveChanges();

      foreach (var camera in ctx.Cameras.ToArray())
      {
        cameraStreamPool.Start(camera);
      }

      var timer = new Timer {AutoReset = false, Interval = 1000};
      timer.Elapsed += (_, args) =>
      {
        GC.Collect();
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
      
      app.UseStaticFiles(new StaticFileOptions()
      {
        FileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory()+"/captures"),
        RequestPath = "/captures"
      });
      
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