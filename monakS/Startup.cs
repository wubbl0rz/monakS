using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using monakS.Controllers;
using Newtonsoft.Json.Converters;

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

      services.AddControllers();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      Task.Run(Loop);

      Task.Run(() =>
      {
        var proc = Process.GetCurrentProcess();
        while (true)
        {
          Console.WriteLine(proc.PrivateMemorySize64 / 1024 / 1024);
          Thread.Sleep(5000);
        }
      });

      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.UseRouting();

      app.UseFileServer();

      app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }

    private unsafe void Loop()
    {
      var url = @"rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov";

      AVDictionary* dict = null;
      ffmpeg.av_dict_set(&dict, "stimeout", $"{TimeSpan.FromSeconds(5).TotalMilliseconds * 1000}", 0);

      var ctx = ffmpeg.avformat_alloc_context();

      if (ffmpeg.avformat_open_input(&ctx, url, null, &dict) != 0)
      {
        ffmpeg.avformat_close_input(&ctx);
        throw new Exception("Cannot connect to: " + url);
      }
      
      ffmpeg.avformat_find_stream_info(ctx, null);
      
      var videoStreamIndex = -1;
      for (var i = 0; i < ctx->nb_streams; i++)
      {
        var stream = ctx->streams[i];
        if (stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
        {
          videoStreamIndex = i;
        }
      }
 
      if (videoStreamIndex < 0)
      {
        ffmpeg.avformat_close_input(&ctx);
        throw new Exception("No VIDEO STREAM found");
      }
      
      ffmpeg.av_read_play(ctx);
      
      var pkt = ffmpeg.av_packet_alloc();

      Console.WriteLine("Connected: " + url);

      long lastTS = 0;

      while (true)
      {
        ffmpeg.av_init_packet(pkt);

        if (ffmpeg.av_read_frame(ctx, pkt) != 0)
        {
          ffmpeg.avformat_close_input(&ctx);
          ffmpeg.av_packet_free(&pkt);
          throw new Exception("Error reading STREAM: " + url);
        }

        if (pkt->stream_index != videoStreamIndex || pkt->pts < 0)
        {
          ffmpeg.av_packet_unref(pkt);
          continue;
        }

        if (pkt->pts <= lastTS)
        {
          ffmpeg.av_packet_unref(pkt);
          continue;
        }

        lastTS = pkt->pts;

        Console.WriteLine("==");
        Console.WriteLine(pkt->pts);
        Console.WriteLine(pkt->dts);
        Console.WriteLine("==");
        
        // zeug machen mit video frames

        foreach (var pc in SignalController._connections.Values)
        {
          //pc.SendJpegFrame();
          //dts ?
          
          var mem = new Span<byte>(pkt->data, pkt->size);

          pc.SendVideo((uint)pkt->dts, mem.ToArray());
        }
        
        ffmpeg.av_packet_unref(pkt);
      }
    }
  }
}