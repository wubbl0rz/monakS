using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using monakS.BackgroundServices;
using monakS.Data;
using monakS.FFMPEG;
using monakS.Models;
using SIPSorcery.Net;
using SIPSorcery.SIP.App;
using SIPSorceryMedia.Abstractions.V1;

namespace monakS.Hubs
{
  public abstract class EventMessage
  {
    //public string Topic { get; set; }
    public virtual bool ForwardToClients { get; set; } = false;
  }

  public class DetectionResultMessage : EventMessage
  {
    public Camera Cam { get; set; }
    public ObjectDetectorSummary Summary { get; set; }
    public override bool ForwardToClients { get; set; } = true;
  }

  public class CaptureStartRequestMessage : EventMessage
  {
    public Camera Cam { get; set; }
    public CaptureTrigger Trigger { get; set; }
  }

  public class CaptureStopRequestMessage : EventMessage
  {
    public Camera Cam { get; set; }
    public CaptureTrigger Trigger { get; set; } = CaptureTrigger.Manual;
  }

  public class CaptureStatusMessage : EventMessage
  {
    public Camera Cam => this.Info.Cam;
    public CaptureInfo Info { get; set; }

    public override bool ForwardToClients { get; set; } = true;
    //public override bool SaveToDb { get; set; } = true;
  }

  public class CameraStartedMessage : EventMessage
  {
    public Camera Cam { get; set; }
    public CameraStreamInfo Info { get; set; }
    public IObservable<AVPacketHandle> Output { get; set; }
  }

  public class MessageEventBus : IObservable<EventMessage>
  {
    private readonly IHubContext<MessageHub> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITopicEventSender _eventSender;
    private readonly Subject<EventMessage> _subject = new Subject<EventMessage>();

    public MessageEventBus(IHubContext<MessageHub> hubContext,
      IServiceProvider serviceProvider,
      ITopicEventSender eventSender)
    {
      _hubContext = hubContext;
      _serviceProvider = serviceProvider;
      _eventSender = eventSender;
    }

    public void Publish<T>(T msg) where T : EventMessage
    {
      _subject.OnNext(msg);

      Console.WriteLine("================");
      Console.WriteLine(msg);
      Console.WriteLine("================");
      
      if (msg.ForwardToClients)
      {
        var typeName = msg.GetType().Name;
        var topic = $"On{typeName.Replace("Message", "")}";
        _hubContext.Clients.All.SendAsync(topic, msg);
      }
    }

    public async IAsyncEnumerable<T> ToAsyncEnumerable<T>([EnumeratorCancellation] CancellationToken token) where T : EventMessage
    {
      var enumerator = this.OfType<T>()
        .ToAsyncEnumerable()
        .WithCancellation(token)
        .ConfigureAwait(false)
        .GetAsyncEnumerator();

      while (true)
      {
        try
        {
          var result = await enumerator.MoveNextAsync();
        }
        catch (OperationCanceledException e)
        {
          break;
        }

        yield return enumerator.Current;
      }
    }

    public IDisposable Subscribe<T>(Action<T> callback) where T : EventMessage
    {
      return _subject.OfType<T>().Subscribe(callback);
    }

    public IDisposable Subscribe(IObserver<EventMessage> observer)
    {
      return _subject.Subscribe(observer);
    }
  }

  public class PeerConnectionCollection
  {
    private ConcurrentDictionary<string, RTCPeerConnection> _connections =
      new ConcurrentDictionary<string, RTCPeerConnection>();

    public bool TryAdd(RTCPeerConnection pc)
    {
      return _connections.TryAdd(pc.SessionID, pc);
    }

    public bool TryGet(string id, out RTCPeerConnection pc)
    {
      return _connections.TryGetValue(id, out pc);
    }

    public bool TryRemove(string id)
    {
      return _connections.TryRemove(id, out _);
    }
  }

  public class MessageHub : Hub
  {
    private readonly MessageEventBus _eventBus;
    private readonly CameraStreamPool _cameraStreamPool;
    private readonly AppDbContext _ctx;
    private readonly ILogger<MessageHub> _log;

    public static readonly RTCConfiguration CONF = new RTCConfiguration
    {
      certificates = new List<RTCCertificate>()
      {
        new RTCCertificate()
        {
#pragma warning disable 618
          Certificate = DtlsUtils.CreateSelfSignedCert()
#pragma warning restore 618
        }
      }
    };

    private static ConcurrentDictionary<string, PeerConnectionCollection> _sessions =
      new ConcurrentDictionary<string, PeerConnectionCollection>();

    public static ConcurrentQueue<RTCPeerConnection> POOL = new ConcurrentQueue<RTCPeerConnection>();

    public MessageHub(MessageEventBus eventBus,
      CameraStreamPool cameraStreamPool,
      AppDbContext ctx,
      ILogger<MessageHub> log)
    {
      _eventBus = eventBus;
      _cameraStreamPool = cameraStreamPool;
      _ctx = ctx;
      _log = log;
    }

    public override Task OnConnectedAsync()
    {
      var cams = _ctx.Cameras.ToArray();
      var captures = _ctx.CaptureInfos.AsQueryable()
        .Where(c => c.IsActive);
      this.Clients.Caller.SendAsync("update", new
      {
        cameras = cams,
        captures
      });
      return base.OnConnectedAsync();
    }

    public void StartCapture(Camera cam)
    {
      cam = _ctx.Cameras.Find(cam.Id);
      _eventBus.Publish(new CaptureStartRequestMessage()
      {
        Trigger = CaptureTrigger.Manual,
        Cam = cam
      });
    }

    public void StopCapture(Camera cam)
    {
      cam = _ctx.Cameras.Find(cam.Id);
      _eventBus.Publish(new CaptureStopRequestMessage()
      {
        Trigger = CaptureTrigger.Manual,
        Cam = cam
      });
    }

    public string GetSessionId(Camera cam)
    {
      var connectionId = this.Context.ConnectionId;

      if (!POOL.TryDequeue(out var pc))
        pc = new RTCPeerConnection(CONF);

      var session = _sessions.GetOrAdd(connectionId, new PeerConnectionCollection());
      var sessionId = pc.SessionID;
      session.TryAdd(pc);

      IDisposable disposeHandle = null;

      pc.onconnectionstatechange += state =>
      {
        switch (state)
        {
          case RTCPeerConnectionState.connected:
            var output = _cameraStreamPool.GetOutput(cam);

            _log.LogInformation($"RTCPeerConnection {pc.SessionID} CONNECTED.");
            disposeHandle = output.Subscribe(pkt => { pc?.SendVideo((uint) pkt.Duration, pkt.Data); });
            break;
          case RTCPeerConnectionState.closed:
          case RTCPeerConnectionState.failed:
            if (session.TryRemove(sessionId))
            {
              _log.LogInformation($"RTCPeerConnection {pc.SessionID} CLOSED.");
              disposeHandle?.Dispose();
              pc.close();
              pc = null;
            }

            break;
        }
      };

      return pc.SessionID;
    }

    public string GetOffer(string id)
    {
      var session = _sessions[this.Context.ConnectionId];
      session.TryGet(id, out var pc);

      var format = new SDPAudioVideoMediaFormat(new VideoFormat(102, "H264"));
      format = format.WithUpdatedFmtp("level-asymmetry-allowed=1;packetization-mode=1;profile-level-id=42e01f", format);

      var track = new MediaStreamTrack(SDPMediaTypesEnum.video,
        false,
        new List<SDPAudioVideoMediaFormat> {format},
        MediaStreamStatusEnum.SendRecv);

      pc.addTrack(track);

      var offer = pc.createOffer(null);
      pc.setLocalDescription(offer);

      return offer.sdp;
    }

    public void SetCandidate(string id, RTCIceCandidateInit candidate)
    {
      var session = _sessions[this.Context.ConnectionId];
      session.TryGet(id, out var pc);
      pc.addIceCandidate(candidate);
    }

    public void SetAnswer(string id, string sdp)
    {
      var session = _sessions[this.Context.ConnectionId];
      session.TryGet(id, out var pc);
      pc.SetRemoteDescription(SdpType.answer, SDP.ParseSDPDescription(sdp));
    }
  }
}