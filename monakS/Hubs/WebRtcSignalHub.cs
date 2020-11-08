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
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
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

  public class ClientMessages
  {
  }

  public class WebRtcSignalSession
  {
    public ConcurrentDictionary<string, RTCPeerConnection> PeerConnections { get; }
      = new ConcurrentDictionary<string, RTCPeerConnection>();
  }

  public class WebRtcSignalHub : Hub
  {
    private readonly CameraStreamPool _cameraStreamPool;

    private static ConcurrentDictionary<string, WebRtcSignalSession> _sessions =
      new ConcurrentDictionary<string, WebRtcSignalSession>();

    public static ConcurrentQueue<RTCPeerConnection> POOL = new ConcurrentQueue<RTCPeerConnection>();

    public WebRtcSignalHub(CameraStreamPool cameraStreamPool)
    {
      _cameraStreamPool = cameraStreamPool;
    }

    public string GetSessionId(Camera cam)
    {
      var connectionId = this.Context.ConnectionId;

      if (!POOL.TryDequeue(out var pc))
        pc = new RTCPeerConnection(null);

      var session = _sessions.GetOrAdd(connectionId, new WebRtcSignalSession());
      session.PeerConnections.TryAdd(pc.SessionID, pc);
      var sessionId = pc.SessionID;

      var channel = Channel.CreateBounded<AVPacketHandle>(new BoundedChannelOptions(5)
      {
        SingleWriter = true,
        SingleReader = true,
        FullMode = BoundedChannelFullMode.DropOldest
      });
      
      Task.Run(async () =>
      {
        await foreach (var pkt in channel.Reader.ReadAllAsync())
        {
          pc?.SendVideo((uint) pkt.Duration, pkt.Data);
        }
      });

      pc.onconnectionstatechange += state =>
      {
        switch (state)
        {
          case RTCPeerConnectionState.connected:
            // var frame = _cameraStreamPool.GetLastKeyframe(cam);
            // SendVideo(frame);
            _cameraStreamPool.GetOutput(cam, pkt => channel.Writer.TryWrite(pkt));
            break;
          case RTCPeerConnectionState.closed:
          case RTCPeerConnectionState.failed:
          {
            if (session.PeerConnections.TryRemove(sessionId, out _))
            {
              Console.WriteLine("CLOSED: " + sessionId);
              //channel.Writer.TryComplete();
              pc.close();
              pc = null;
            }

            break;
          }
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