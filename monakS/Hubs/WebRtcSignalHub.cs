using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SIPSorcery.Net;
using SIPSorcery.SIP.App;
using SIPSorceryMedia.Abstractions.V1;
using TinyJson;

namespace monakS.Hubs
{
  public class WebRtcSignalHub : Hub
  {
    public static IEnumerable<RTCPeerConnection> CONNECTIONS => _sessions.Values;
    
    public static ConcurrentQueue<RTCPeerConnection> POOL = new ConcurrentQueue<RTCPeerConnection>();
    
    private static ConcurrentDictionary<string, RTCPeerConnection> _sessions =
      new ConcurrentDictionary<string, RTCPeerConnection>();

    private static ConcurrentDictionary<string, string> _users = new ConcurrentDictionary<string, string>();

    private static string CreateConnection(string connectionId)
    {
      var sw = new Stopwatch();
      sw.Start();

      POOL.TryDequeue(out var pc);
      _sessions.TryAdd(pc.SessionID, pc);
      _users.TryAdd(pc.SessionID, connectionId);

      // pc.onicecandidate += (candidate) =>
      // {
      //   Console.WriteLine(candidate.address);
      // };
 
      // pc.onconnectionstatechange += state =>
      // {
      //   Console.WriteLine(state);
      // };

      Console.WriteLine(sw.ElapsedMilliseconds);

      return pc.SessionID;
    }

    public string GetSessionId()
    {
      return CreateConnection(this.Context.ConnectionId);
    }

    public string GetOffer(string id)
    {
      _sessions.TryGetValue(id, out var pc);

      var format = new SDPAudioVideoMediaFormat(new VideoFormat(102, "H264"));
      format = format.WithUpdatedFmtp("level-asymmetry-allowed=1;packetization-mode=1;profile-level-id=42e01f", format);

      var track = new MediaStreamTrack(SDPMediaTypesEnum.video,
        false,
        new List<SDPAudioVideoMediaFormat> {format},
        MediaStreamStatusEnum.SendOnly);

      pc.addTrack(track);

      var offer = pc.createOffer(null);

      pc.setLocalDescription(offer).Wait();

      return offer.sdp;
    }

    public void SetCandidate(string id, RTCIceCandidateInit candidate)
    {
      var pc = _sessions[id];
      pc.addIceCandidate(candidate);
    }

    public void SetAnswer(string id, string sdp)
    {
      var pc = _sessions[id];
      pc.SetRemoteDescription(SdpType.answer, SDP.ParseSDPDescription(sdp));
    }
  }
}