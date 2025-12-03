using Org.Webrtc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using WebRTCme; 
using WebRTCme. Maui;

namespace Proximity.Services
{
    public interface ISignalingTransport
    {
        Task SendSignalingAsync(string targetPeerId, string json);
        event Action<string> OnSignalingMessage; // raw JSON messages received
        string LocalPeerId { get; }
    }

    public class WebRtcService : IDisposable
    {
        private readonly ISignalingTransport _signaling;
        private RTCPeerConnection _pc;
        private MediaStream _localStream;
        private MediaStream _remoteStream;
        private readonly List<RTCIceCandidateInit> _queuedRemoteCandidates = new();
        public event Action OnRemoteStreamAvailable;
        public event Action OnCallEnded;
        public WebRtcService(ISignalingTransport signaling)
        {
            _signaling = signaling ?? throw new ArgumentNullException(nameof(signaling));
            _signaling.OnSignalingMessage += OnSignalingMessageReceived;
        }
        public async Task InitializeAsync()
        {
            if (_pc != null) return;
            var config = new RTCConfiguration
            {
                iceServers = new RTCIceServer[0] // empty for LAN-only
            };

            _pc = new RTCPeerConnection(config);

            _pc.OnIceCandidate += async (candidate) =>
            {
                if (candidate == null) return;
                // Candidate.targetPeerId is conceptual; your binding may not include it so include target in JSON when sending.
                var msg = new
                {
                    type = "webrtc.ice",
                    from = _signaling.LocalPeerId,
                    candidate = candidate.candidate,
                    sdpMid = candidate.sdpMid,
                    sdpMLineIndex = candidate.sdpMLineIndex
                };
                // You must decide which peer to send to; for calls, your UI will call StartCallAsync(remotePeerId) and store the remoteId
                if (!string.IsNullOrEmpty(_currentRemotePeerId))
                {
                    await _signaling.SendSignalingAsync(_currentRemotePeerId, JsonSerializer.Serialize(msg));
                }
            };

            _pc.OnTrack += (e) =>
            {
                _remoteStream = e.Streams != null && e.Streams.Length > 0 ? e.Streams[0] : null;
                OnRemoteStreamAvailable?.Invoke();
            };

            // Acquire microphone stream (binding provides MediaDevices API)
            _localStream = await MediaDevices.GetUserMedia(new MediaStreamConstraints { audio = true, video = false });
            foreach (var t in _localStream.GetAudioTracks())
            {
                _pc.AddTrack(t, _localStream);
            }
        }

        private string _currentRemotePeerId;

        public async Task StartCallAsync(string remotePeerId)
        {
            if (string.IsNullOrEmpty(remotePeerId)) throw new ArgumentNullException(nameof(remotePeerId));
            _currentRemotePeerId = remotePeerId;

            if (_pc == null) await InitializeAsync();

            var offer = await _pc.CreateOffer();
            await _pc.SetLocalDescription(offer);

            var msg = new
            {
                type = "webrtc.sdp",
                subtype = "offer",
                from = _signaling.LocalPeerId,
                to = remotePeerId,
                sdp = offer.sdp
            };

            await _signaling.SendSignalingAsync(remotePeerId, JsonSerializer.Serialize(msg));
        }

        private async Task HandleRemoteOfferAsync(string remotePeerId, string sdp)
        {
            _currentRemotePeerId = remotePeerId;
            if (_pc == null) await InitializeAsync();

            var desc = new RTCSessionDescriptionInit { type = RTCSdpType.offer, sdp = sdp };
            await _pc.SetRemoteDescription(desc);

            var answer = await _pc.CreateAnswer();
            await _pc.SetLocalDescription(answer);

            var msg = new
            {
                type = "webrtc.sdp",
                subtype = "answer",
                from = _signaling.LocalPeerId,
                to = remotePeerId,
                sdp = answer.sdp
            };
            await _signaling.SendSignalingAsync(remotePeerId, JsonSerializer.Serialize(msg));
        }
        private async Task HandleRemoteAnswerAsync(string remotePeerId, string sdp)
        {
            var desc = new RTCSessionDescriptionInit { type = RTCSdpType.answer, sdp = sdp };
            await _pc.SetRemoteDescription(desc);

            // Apply queued ICE
            foreach (var c in _queuedRemoteCandidates)
            {
                await _pc.AddIceCandidate(c);
            }
            _queuedRemoteCandidates.Clear();
        }
        private async Task HandleRemoteIceAsync(string remotePeerId, string candidateStr, string sdpMid, int sdpMLineIndex)
        {
            var candidate = new RTCIceCandidateInit { candidate = candidateStr, sdpMid = sdpMid, sdpMLineIndex = sdpMLineIndex };
            if (_pc.RemoteDescription == null)
            {
                _queuedRemoteCandidates.Add(candidate);
            }
            else
            {
                await _pc.AddIceCandidate(candidate);
            }
        }
        private async void OnSignalingMessageReceived(string json)
        {
            try
            {using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (!root.TryGetProperty("type", out var typeEl)) return;
                var type = typeEl.GetString();


                if (type == "webrtc.sdp")
                {
                    var subtype = root.GetProperty("subtype").GetString();
                    var from = root.GetProperty("from").GetString();
                    var sdp = root.GetProperty("sdp").GetString();
                    if (subtype == "offer") await HandleRemoteOfferAsync(from, sdp);
                    else if (subtype == "answer") await HandleRemoteAnswerAsync(from, sdp);
                }
                else if (type == "webrtc.ice")
                {
                    var from = root.GetProperty("from").GetString();
                    var candidate = root.GetProperty("candidate").GetString();
                    var sdpMid = root.TryGetProperty("sdpMid", out var mid) ? mid.GetString() : null;
                    var sdpMLineIndex = root.TryGetProperty("sdpMLineIndex", out var idx) ? idx.GetInt32() : 0;
                    await HandleRemoteIceAsync(from, candidate, sdpMid, sdpMLineIndex);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Signaling parse error: " + ex);
            }
        }
        public async Task EndCallAsync()
        {
            try
            { _pc?.Close();
                _pc?.Dispose();
                _pc = null;
                _localStream?.Dispose();
                _localStream = null;
                _remoteStream = null;
                _currentRemotePeerId = null;
                OnCallEnded?.Invoke();
            }
            catch { }
        }
        public void Dispose()
        {
            _signaling.OnSignalingMessage -= OnSignalingMessageReceived;
            _ = EndCallAsync();
        }
    }
}
