using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Concentus.Structs;
using Concentus.Enums;

namespace Proximity.Services
{
    public class VoiceService : IDisposable
    {
        private const int SampleRate = 48000;
        private const int FrameSize = 960; // 20ms at 48kHz
        private const int Channels = 1;
        private readonly int _port;

        private UdpClient? _udpClient;
        private WaveInEvent? _waveIn;
        private WaveOutEvent? _waveOut;
        private BufferedWaveProvider? _waveProvider;

        private OpusEncoder? _encoder;
        private OpusDecoder? _decoder;

        private CancellationTokenSource? _cts;
        private bool _isActive;
        private bool _isPushToTalk = true;
        private bool _isTransmitting;

        public event Action<string>? VoiceDataReceived;

        public bool IsPushToTalk
        {
            get => _isPushToTalk;
            set => _isPushToTalk = value;
        }

        public bool IsTransmitting
        {
            get => _isTransmitting;
            set
            {
                if (_isPushToTalk)
                {
                    _isTransmitting = value;
                }
            }
        }

        public VoiceService(int port = 9003)
        {
            _port = port;
        }

        public void Initialize()
        {
            try
            {
                // Initialize Opus codec
                _encoder = OpusEncoder.Create(SampleRate, Channels, OpusApplication.OPUS_APPLICATION_VOIP);
                _encoder.Bitrate = 24000; // 24 kbps
                _decoder = OpusDecoder.Create(SampleRate, Channels);

                // Initialize UDP
                _udpClient = new UdpClient(_port);
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // Initialize audio output
                _waveOut = new WaveOutEvent();
                _waveProvider = new BufferedWaveProvider(new WaveFormat(SampleRate, 16, Channels))
                {
                    BufferDuration = TimeSpan.FromSeconds(2),
                    DiscardOnBufferOverflow = true
                };
                _waveOut.Init(_waveProvider);
                _waveOut.Play();

                // Initialize audio input
                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(SampleRate, 16, Channels),
                    BufferMilliseconds = 20
                };
                _waveIn.DataAvailable += OnAudioDataAvailable;

                _isActive = true;
                _cts = new CancellationTokenSource();

                // Start receiving
                _ = ReceiveLoop(_cts.Token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VoiceService init error: {ex.Message}");
            }
        }

        public void StartCapture()
        {
            if (_waveIn != null && !_isPushToTalk)
            {
                _isTransmitting = true;
                _waveIn.StartRecording();
            }
        }

        public void StopCapture()
        {
            if (_waveIn != null && !_isPushToTalk)
            {
                _isTransmitting = false;
                _waveIn.StopRecording();
            }
        }

        public void StartPushToTalk()
        {
            if (_waveIn != null && _isPushToTalk)
            {
                _isTransmitting = true;
                _waveIn.StartRecording();
            }
        }

        public void StopPushToTalk()
        {
            if (_waveIn != null && _isPushToTalk)
            {
                _isTransmitting = false;
                _waveIn.StopRecording();
            }
        }

        private void OnAudioDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (!_isTransmitting || _encoder == null) return;

            try
            {
                // Convert bytes to shorts
                var shorts = new short[e.BytesRecorded / 2];
                Buffer.BlockCopy(e.Buffer, 0, shorts, 0, e.BytesRecorded);

                // Process in frames
                for (int i = 0; i < shorts.Length; i += FrameSize)
                {
                    if (i + FrameSize > shorts.Length) break;

                    var frame = new short[FrameSize];
                    Array.Copy(shorts, i, frame, 0, FrameSize);

                    // Encode with Opus
                    var encoded = new byte[4000];
                    var encodedLength = _encoder.Encode(frame, 0, FrameSize, encoded, 0, encoded.Length);

                    if (encodedLength > 0)
                    {
                        var packet = new byte[encodedLength];
                        Array.Copy(encoded, packet, encodedLength);

                        // Send to all peers (broadcast)
                        _ = SendVoicePacket(packet);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audio encode error: {ex.Message}");
            }
        }

        private async Task SendVoicePacket(byte[] packet)
        {
            if (_udpClient == null) return;

            try
            {
                // Broadcast to LAN
                var endpoint = new IPEndPoint(IPAddress.Broadcast, _port);
                await _udpClient.SendAsync(packet, packet.Length, endpoint);
            }
            catch { }
        }

        public async Task SendVoiceToEndpoint(byte[] packet, IPEndPoint endpoint)
        {
            if (_udpClient == null) return;

            try
            {
                await _udpClient.SendAsync(packet, packet.Length, endpoint);
            }
            catch { }
        }

        private async Task ReceiveLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _isActive)
            {
                try
                {
                    var result = await _udpClient!.ReceiveAsync();
                    ProcessVoicePacket(result.Buffer);
                }
                catch when (ct.IsCancellationRequested) { }
                catch { }
            }
        }

        private void ProcessVoicePacket(byte[] packet)
        {
            if (_decoder == null || _waveProvider == null) return;

            try
            {
                // Decode with Opus
                var decoded = new short[FrameSize];
                var decodedLength = _decoder.Decode(packet, 0, packet.Length, decoded, 0, FrameSize, false);

                if (decodedLength > 0)
                {
                    // Convert shorts to bytes
                    var bytes = new byte[decodedLength * 2];
                    Buffer.BlockCopy(decoded, 0, bytes, 0, bytes.Length);

                    // Add to playback buffer
                    _waveProvider.AddSamples(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audio decode error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _isActive = false;
            _cts?.Cancel();

            _waveIn?.StopRecording();
            _waveIn?.Dispose();

            _waveOut?.Stop();
            _waveOut?.Dispose();

            _udpClient?.Close();
        }
    }
}