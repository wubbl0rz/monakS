using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using monakS.BackgroundServices;
using monakS.FFMPEG;
using monakS.Models;
using SIPSorcery.Net;
using SIPSorcery.SIP.App;
using SIPSorceryMedia.Abstractions.V1;

namespace monakS.Hubs
{
  // public class WebRtcConnectionStore
  // {
  //   private static ConcurrentDictionary<string, WebRtcSignalSession> _sessions =
  //     new ConcurrentDictionary<string, WebRtcSignalSession>();
  //
  //   public void Add(Camera cam, RTCPeerConnection pc)
  //   {
  //   }
  //   
  //   public void Remove(Camera cam, RTCPeerConnection pc)
  //   {
  //   }
  // }

  // public class SignalPublisher
  // {
  //   public SignalPublisher(RXEVENT)
  //   {
  //     
  //   }
  // }

  public class WebRtcSignalSession
  {
    public ConcurrentDictionary<string, RTCPeerConnection> PeerConnections { get; }
      = new ConcurrentDictionary<string, RTCPeerConnection>();
  }

  public class SignalHub : Hub
  {
    private readonly CameraStreamPool _cameraStreamPool;
    
    private static ConcurrentDictionary<string, WebRtcSignalSession> _sessions =
      new ConcurrentDictionary<string, WebRtcSignalSession>();

    public static ConcurrentQueue<RTCPeerConnection> POOL = new ConcurrentQueue<RTCPeerConnection>();

    private static ConcurrentBag<IClientProxy> _clients = new ConcurrentBag<IClientProxy>();

    // public static void SendUpdate()
    // {
    //   foreach (var client in _clients)
    //   {
    //     client.SendAsync("KEKW");
    //   }
    // }

    public SignalHub(CameraStreamPool cameraStreamPool)
    {
      _cameraStreamPool = cameraStreamPool;
    }

    public override Task OnConnectedAsync()
    {
      _clients.Add(this.Clients.Caller);
      return base.OnConnectedAsync();
    }

    public string GetSessionId(Camera cam)
    {
      var connectionId = this.Context.ConnectionId;

      if (!POOL.TryDequeue(out var pc))
        pc = new RTCPeerConnection(null);

      var session = _sessions.GetOrAdd(connectionId, new WebRtcSignalSession());
      session.PeerConnections.TryAdd(pc.SessionID, pc);
      var sessionId = pc.SessionID;

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
            if (session.PeerConnections.TryRemove(sessionId, out _))
            {
              Console.WriteLine("CLOSED: " + sessionId);
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
      var pc = session.PeerConnections[id];

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
      var pc = session.PeerConnections[id];
      pc.addIceCandidate(candidate);
    }

    public void SetAnswer(string id, string sdp)
    {
      var session = _sessions[this.Context.ConnectionId];
      var pc = session.PeerConnections[id];
      pc.SetRemoteDescription(SdpType.answer, SDP.ParseSDPDescription(sdp));
    }
  }
}