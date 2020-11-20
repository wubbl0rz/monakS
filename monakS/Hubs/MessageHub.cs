using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.AspNetCore.SignalR;
using monakS.BackgroundServices;
using monakS.FFMPEG;
using monakS.Models;
using SIPSorcery.Net;
using SIPSorcery.SIP.App;
using SIPSorceryMedia.Abstractions.V1;

namespace monakS.Hubs
{
  public abstract class EventMessage
  {
    public virtual bool SaveToDb { get; set; } = false;
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

  public class CaptureResultMessage : EventMessage
  {
    public Camera Cam { get; set; }
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
    private readonly Subject<EventMessage> _subject = new Subject<EventMessage>();

    public MessageEventBus(IHubContext<MessageHub> hubContext)
    {
      _hubContext = hubContext;
    }

    public void Publish<T>(T msg) where T : EventMessage
    {
      _subject.OnNext(msg);

      if (msg.ForwardToClients)
      {
        _hubContext.Clients.All.SendAsync(msg.GetType().Name, msg);
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
    private readonly CameraStreamPool _cameraStreamPool;
    
    private static ConcurrentDictionary<string, PeerConnectionCollection> _sessions =
      new ConcurrentDictionary<string, PeerConnectionCollection>();

    public static ConcurrentQueue<RTCPeerConnection> POOL = new ConcurrentQueue<RTCPeerConnection>();

    public MessageHub(CameraStreamPool cameraStreamPool)
    {
      _cameraStreamPool = cameraStreamPool;
    }

    public string GetSessionId(Camera cam)
    {
      var connectionId = this.Context.ConnectionId;

      if (!POOL.TryDequeue(out var pc))
        pc = new RTCPeerConnection(null);

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
            disposeHandle = output.Subscribe(pkt =>
            {
              pc?.SendVideo((uint) pkt.Duration, pkt.Data);
            });
            break;
          case RTCPeerConnectionState.closed:
          case RTCPeerConnectionState.failed:
            if (session.TryRemove(sessionId))
            {
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