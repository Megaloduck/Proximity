using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PortAudioSharp;
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
        private PortAudio? _portAudio;
        private Stream? _inputStream;
        private Stream? _outputStream;

        private OpusEncoder? _encoder;
        private OpusDecoder? _decoder;

        private CancellationTokenSource? _cts;
        private bool _isActive;
        private bool _isPushToTalk = true;
        private bool _isTransmitting;

        private int _inputDeviceIndex = -1;
        private int _outputDeviceIndex = -1;

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
                // Initialize PortAudio
                PortAudio.Initialize();
                _portAudio = PortAudio.Instance;

                // Initialize Opus codec
                _encoder = new OpusEncoder(SampleRate, Channels, OpusApplication.OPUS_APPLICATION_VOIP);
                _encoder.Bitrate = 24000; // 24 kbps

                _decoder = new OpusDecoder(SampleRate, Channels);

                // Initialize UDP
                _udpClient = new UdpClient(_port);
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // Get default devices
                _inputDeviceIndex = PortAudio.DefaultInputDevice;
                _outputDeviceIndex = PortAudio.DefaultOutputDevice;

                // Initialize audio streams
                InitializeAudioStreams();

                _isActive = true;
                _cts = new CancellationTokenSource();

                // Start receiving
                _ = ReceiveLoop(_cts.Token);
                _ = CaptureLoop(_cts.Token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VoiceService init error: {ex.Message}");
            }
        }

        private void InitializeAudioStreams()
        {
            try
            {
                // Input stream parameters
                var inputParams = new StreamParameters
                {
                    device = _inputDeviceIndex,
                    channelCount = Channels,
                    sampleFormat = SampleFormat.Int16,
                    suggestedLatency = PortAudio.GetDeviceInfo(_inputDeviceIndex).defaultLowInputLatency
                };

                // Output stream parameters
                var outputParams = new StreamParameters
                {
                    device = _outputDeviceIndex,
                    channelCount = Channels,
                    sampleFormat = SampleFormat.Int16,
                    suggestedLatency = PortAudio.GetDeviceInfo(_outputDeviceIndex).defaultLowOutputLatency
                };

                // Open input stream (microphone)
                _inputStream = new Stream(
                    inParams: inputParams,
                    outParams: null,
                    sampleRate: SampleRate,
                    framesPerBuffer: FrameSize,
                    streamFlags: StreamFlags.ClipOff,
                    callback: null
                );

                // Open output stream (speaker)
                _outputStream = new Stream(
                    inParams: null,
                    outParams: outputParams,
                    sampleRate: SampleRate,
                    framesPerBuffer: FrameSize,
                    streamFlags: StreamFlags.ClipOff,
                    callback: null
                );

                _outputStream.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audio stream init error: {ex.Message}");
            }
        }

        public void SetInputDevice(int deviceIndex)
        {
            _inputDeviceIndex = deviceIndex;
            ReinitializeAudioStreams();
        }

        public void SetOutputDevice(int deviceIndex)
        {
            _outputDeviceIndex = deviceIndex;
            ReinitializeAudioStreams();
        }

        private void ReinitializeAudioStreams()
        {
            try
            {
                _inputStream?.Stop();
                _inputStream?.Dispose();
                _outputStream?.Stop();
                _outputStream?.Dispose();

                InitializeAudioStreams();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reinitializing streams: {ex.Message}");
            }
        }

        public void StartCapture()
        {
            if (_inputStream != null && !_isPushToTalk)
            {
                _isTransmitting = true;
                _inputStream.Start();
            }
        }

        public void StopCapture()
        {
            if (_inputStream != null && !_isPushToTalk)
            {
                _isTransmitting = false;
                _inputStream.Stop();
            }
        }

        public void StartPushToTalk()
        {
            if (_inputStream != null && _isPushToTalk)
            {
                _isTransmitting = true;
                _inputStream.Start();
            }
        }

        public void StopPushToTalk()
        {
            if (_inputStream != null && _isPushToTalk)
            {
                _isTransmitting = false;
                _inputStream.Stop();
            }
        }

        private async Task CaptureLoop(CancellationToken ct)
        {
            var buffer = new short[FrameSize];

            while (!ct.IsCancellationRequested && _isActive)
            {
                try
                {
                    if (_isTransmitting && _inputStream != null && _inputStream.IsActive)
                    {
                        // Read audio from microphone
                        _inputStream.Read(buffer, FrameSize);

                        // Encode with Opus
                        if (_encoder != null)
                        {
                            var encoded = new byte[4000];
                            var encodedLength = _encoder.Encode(buffer, 0, FrameSize, encoded, 0, encoded.Length);

                            if (encodedLength > 0)
                            {
                                var packet = new byte[encodedLength];
                                Array.Copy(encoded, packet, encodedLength);

                                // Send to all peers (broadcast)
                                await SendVoicePacket(packet);
                            }
                        }
                    }
                    else
                    {
                        await Task.Delay(20, ct);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Capture error: {ex.Message}");
                    await Task.Delay(100, ct);
                }
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
            if (_decoder == null || _outputStream == null) return;

            try
            {
                // Decode with Opus
                var decoded = new short[FrameSize];
                var decodedLength = _decoder.Decode(packet, 0, packet.Length, decoded, 0, FrameSize, false);

                if (decodedLength > 0 && _outputStream.IsActive)
                {
                    // Play audio
                    _outputStream.Write(decoded, decodedLength);
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

            _inputStream?.Stop();
            _inputStream?.Dispose();

            _outputStream?.Stop();
            _outputStream?.Dispose();

            _udpClient?.Close();

            PortAudio.Terminate();
        }
    }
}