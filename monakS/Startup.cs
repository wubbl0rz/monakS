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
using Newtonsoft.Json.Converters;
using SIPSorcery.Net;
using monakS.Hubs;

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
      services.AddSignalR();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      Task.Run(Loop);

      Task.Run(() =>
      {
        while (true)
        {
          GC.Collect();
          var proc = Process.GetCurrentProcess();
          Console.WriteLine("Working set {0} KB", proc.WorkingSet64 / 1024);

          if (WebRtcSignalHub.POOL.Count < 10)
          { 
            for (var i = 0; i < 20; i++)
            {
              WebRtcSignalHub.POOL.Enqueue(new RTCPeerConnection(null));
            }
          }
          
          //TODO: clean unused and closed connections

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

    //TODO: connection noch schneller machen... 1x letzter keyframe als preview für den video player
    //TODO: und alle frames zwischen jetzt und letztem keyframe an video player senden und vorspulen

    private unsafe void Loop()
    {
      //var url = @"rtsp://localhost:8554/mystream2";
      var url = @"rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov";
      AVDictionary* dict = null;
      ffmpeg.av_dict_set(&dict, "rtsp_transport", "tcp", 0);
      ffmpeg.av_dict_set(&dict, "stimeout", $"{TimeSpan.FromSeconds(5).TotalMilliseconds * 1000}", 0);

      var ctx = ffmpeg.avformat_alloc_context();

      //tmp->interrupt_callback.callback = this.timeoutCallback;
      //this.inputCtx = tmp;

      if (ffmpeg.avformat_open_input(&ctx, url, null, &dict) != 0)
      {
        ffmpeg.avformat_close_input(&ctx);
        throw new Exception("Cannot connect to: " + url);
      }

      ffmpeg.avformat_find_stream_info(ctx, null);

      var videoStreamIndex = -1;
      var rate = 0d;

      for (var i = 0; i < ctx->nb_streams; i++)
      {
        var stream = ctx->streams[i];
        if (stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
        {
          videoStreamIndex = i;
          rate = ffmpeg.av_q2d(stream->r_frame_rate);
        }
      }

      if (videoStreamIndex < 0)
      {
        ffmpeg.avformat_close_input(&ctx);
        throw new Exception("No VIDEO STREAM found");
      }

      //ffmpeg.av_read_play(ctx);

      var pkt = ffmpeg.av_packet_alloc();

      Console.WriteLine("Connected: " + url);

      var tasks = new List<Task>();

      long timestamp = 0;

      while (true)
      {
        ffmpeg.av_init_packet(pkt);

        if (ffmpeg.av_read_frame(ctx, pkt) != 0)
        {
          ffmpeg.avformat_close_input(&ctx);
          ffmpeg.av_packet_free(&pkt);
          throw new Exception("Error reading STREAM: " + url);
        }

        if (pkt->stream_index != videoStreamIndex || pkt->pts < 0 || pkt->pts <= timestamp)
        {
          ffmpeg.av_packet_unref(pkt);
          continue;
        }

        timestamp = pkt->pts;

        foreach (var t in WebRtcSignalHub.CONNECTIONS)
        {
          var buffer = new Span<byte>(pkt->data, pkt->size).ToArray();
          var ts = pkt->duration == 0 ? (long) (90000 / rate) : pkt->duration;
          var pc = t;

          tasks.Add(Task.Run(() => { pc.SendVideo((uint) ts, buffer); }));
        }

        Task.WaitAll(tasks.ToArray());
        tasks.Clear();

        ffmpeg.av_packet_unref(pkt);
      }
    }
  }
}