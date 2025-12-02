using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PortAudioSharp;
using Concentus.Structs;
using Concentus.Enums;
using PaStream = PortAudioSharp.Stream;

namespace Proximity.Services
{
    public class VoiceService : IDisposable
    {
        private const int SampleRate = 48000;
        private const int FrameSize = 960; // 20ms at 48kHz
        private const int Channels = 1;
        private readonly int _port;

        private UdpClient? _udpClient;
        private PaStream? _inputStream;
        private PaStream? _outputStream;

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
                // Initialize PortAudio if needed
                try
                {
                    // Try to get device count - if it throws, PortAudio isn't initialized
                    var deviceCount = PortAudio.DeviceCount;
                }
                catch
                {
                    PortAudio.Initialize();
                }

                // Initialize Opus codec
                _encoder = new OpusEncoder(SampleRate, Channels, OpusApplication.OPUS_APPLICATION_VOIP);
                _encoder.Bitrate = 24000; // 24 kbps

                _decoder = new OpusDecoder(SampleRate, Channels);

                // Initialize UDP
                _udpClient = new UdpClient(_port);
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // Get saved or default devices
                _inputDeviceIndex = Preferences.Get("InputDeviceIndex", PortAudio.DefaultInputDevice);
                _outputDeviceIndex = Preferences.Get("OutputDeviceIndex", PortAudio.DefaultOutputDevice);

                // Initialize audio streams
                InitializeAudioStreams();

                _isActive = true;
                _cts = new CancellationTokenSource();

                // Load push-to-talk preference
                _isPushToTalk = Preferences.Get("IsPushToTalk", true);

                // Start receiving
                _ = ReceiveLoop(_cts.Token);
                _ = CaptureLoop(_cts.Token);

                System.Diagnostics.Debug.WriteLine("VoiceService initialized successfully");
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
                // Validate device indices
                if (_inputDeviceIndex < 0 || _inputDeviceIndex >= PortAudio.DeviceCount)
                {
                    _inputDeviceIndex = PortAudio.DefaultInputDevice;
                    System.Diagnostics.Debug.WriteLine($"Invalid input device, using default: {_inputDeviceIndex}");
                }

                if (_outputDeviceIndex < 0 || _outputDeviceIndex >= PortAudio.DeviceCount)
                {
                    _outputDeviceIndex = PortAudio.DefaultOutputDevice;
                    System.Diagnostics.Debug.WriteLine($"Invalid output device, using default: {_outputDeviceIndex}");
                }

                // Input stream parameters
                var inputInfo = PortAudio.GetDeviceInfo(_inputDeviceIndex);
                var inputParams = new StreamParameters
                {
                    device = _inputDeviceIndex,
                    channelCount = Channels,
                    sampleFormat = SampleFormat.Int16,
                    suggestedLatency = inputInfo.defaultLowInputLatency
                };

                // Output stream parameters
                var outputInfo = PortAudio.GetDeviceInfo(_outputDeviceIndex);
                var outputParams = new StreamParameters
                {
                    device = _outputDeviceIndex,
                    channelCount = Channels,
                    sampleFormat = SampleFormat.Int16,
                    suggestedLatency = outputInfo.defaultLowOutputLatency
                };

                // Open input stream (microphone)
                _inputStream = new PaStream(
                    inParams: inputParams,
                    outParams: null,
                    sampleRate: SampleRate,
                    framesPerBuffer: FrameSize,
                    streamFlags: StreamFlags.ClipOff,
                    callback: null,
                    userData: null
                );

                // Open output stream (speaker)
                _outputStream = new PaStream(
                    inParams: null,
                    outParams: outputParams,
                    sampleRate: SampleRate,
                    framesPerBuffer: FrameSize,
                    streamFlags: StreamFlags.ClipOff,
                    callback: null,
                    userData: null
                );

                _outputStream.Start();

                System.Diagnostics.Debug.WriteLine($"Audio streams initialized - Input: {inputInfo.name}, Output: {outputInfo.name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audio stream init error: {ex.Message}");
            }
        }

        public void SetInputDevice(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= PortAudio.DeviceCount)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid input device index: {deviceIndex}");
                return;
            }

            _inputDeviceIndex = deviceIndex;
            Preferences.Set("InputDeviceIndex", deviceIndex);
            ReinitializeAudioStreams();

            var info = PortAudio.GetDeviceInfo(deviceIndex);
            System.Diagnostics.Debug.WriteLine($"Input device changed to: {info.name}");
        }

        public void SetOutputDevice(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= PortAudio.DeviceCount)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid output device index: {deviceIndex}");
                return;
            }

            _outputDeviceIndex = deviceIndex;
            Preferences.Set("OutputDeviceIndex", deviceIndex);
            ReinitializeAudioStreams();

            var info = PortAudio.GetDeviceInfo(deviceIndex);
            System.Diagnostics.Debug.WriteLine($"Output device changed to: {info.name}");
        }

        private void ReinitializeAudioStreams()
        {
            try
            {
                // Stop current transmission if active
                var wasTransmitting = _isTransmitting;
                if (wasTransmitting)
                {
                    _isTransmitting = false;
                }

                // Close existing streams
                _inputStream?.Stop();
                _inputStream?.Dispose();
                _outputStream?.Stop();
                _outputStream?.Dispose();

                // Reinitialize with new devices
                InitializeAudioStreams();

                // Restore transmission state
                if (wasTransmitting && !_isPushToTalk)
                {
                    _isTransmitting = true;
                    _inputStream?.Start();
                }
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
                if (!_inputStream.IsActive)
                {
                    _inputStream.Start();
                }
                System.Diagnostics.Debug.WriteLine("Voice capture started (continuous mode)");
            }
        }

        public void StopCapture()
        {
            if (_inputStream != null && !_isPushToTalk)
            {
                _isTransmitting = false;
                if (_inputStream.IsActive)
                {
                    _inputStream.Stop();
                }
                System.Diagnostics.Debug.WriteLine("Voice capture stopped");
            }
        }

        public void StartPushToTalk()
        {
            if (_inputStream != null && _isPushToTalk)
            {
                _isTransmitting = true;
                if (!_inputStream.IsActive)
                {
                    _inputStream.Start();
                }
                System.Diagnostics.Debug.WriteLine("Push-to-talk activated");
            }
        }

        public void StopPushToTalk()
        {
            if (_inputStream != null && _isPushToTalk)
            {
                _isTransmitting = false;
                if (_inputStream.IsActive)
                {
                    _inputStream.Stop();
                }
                System.Diagnostics.Debug.WriteLine("Push-to-talk deactivated");
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
                        // Read audio from microphone using ReadStream
                        unsafe
                        {
                            fixed (short* ptr = buffer)
                            {
                                _inputStream.ReadStream((IntPtr)ptr, (uint)FrameSize);
                            }
                        }

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Send voice packet error: {ex.Message}");
            }
        }

        public async Task SendVoiceToEndpoint(byte[] packet, IPEndPoint endpoint)
        {
            if (_udpClient == null) return;

            try
            {
                await _udpClient.SendAsync(packet, packet.Length, endpoint);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Send to endpoint error: {ex.Message}");
            }
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Receive error: {ex.Message}");
                    await Task.Delay(100, ct);
                }
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
                    // Play audio using WriteStream
                    unsafe
                    {
                        fixed (short* ptr = decoded)
                        {
                            _outputStream.WriteStream((IntPtr)ptr, (uint)decodedLength);
                        }
                    }
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

            try
            {
                _inputStream?.Stop();
                _inputStream?.Dispose();

                _outputStream?.Stop();
                _outputStream?.Dispose();

                _udpClient?.Close();

                // Don't terminate PortAudio - it may still be used by other components
                // Only terminate if you're sure nothing else needs it

                System.Diagnostics.Debug.WriteLine("VoiceService disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dispose error: {ex.Message}");
            }
        }
    }
}