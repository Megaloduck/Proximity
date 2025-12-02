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
        private const int FrameSize = 960; // 20ms @ 48kHz
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
                    _isTransmitting = value;
            }
        }

        public VoiceService(int port = 9003)
        {
            _port = port;
        }

        // ---------------------------------------------------
        // INITIALIZATION
        // ---------------------------------------------------
        public void Initialize()
        {
            try
            {
                // Initialize PortAudio (safe check)
                try { var x = PortAudio.DeviceCount; }
                catch { PortAudio.Initialize(); }

                // Initialize encoder/decoder
                _encoder = new OpusEncoder(SampleRate, Channels, OpusApplication.OPUS_APPLICATION_VOIP)
                {
                    Bitrate = 24000
                };
                _decoder = new OpusDecoder(SampleRate, Channels);

                // UDP
                _udpClient = new UdpClient(_port);
                _udpClient.EnableBroadcast = true;

                // Load device preferences
                _inputDeviceIndex = Preferences.Get("InputDeviceIndex", PortAudio.DefaultInputDevice);
                _outputDeviceIndex = Preferences.Get("OutputDeviceIndex", PortAudio.DefaultOutputDevice);
                _isPushToTalk = Preferences.Get("IsPushToTalk", true);

                InitializeAudioStreams();

                _cts = new CancellationTokenSource();
                _isActive = true;

                _ = ReceiveLoop(_cts.Token);
                _ = CaptureLoop(_cts.Token);

                System.Diagnostics.Debug.WriteLine("VoiceService initialized");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VoiceService init error: {ex}");
            }
        }

        // ---------------------------------------------------
        // AUDIO STREAM SETUP
        // ---------------------------------------------------
        private void InitializeAudioStreams()
        {
            try
            {
                if (_inputDeviceIndex < 0 || _inputDeviceIndex >= PortAudio.DeviceCount)
                    _inputDeviceIndex = PortAudio.DefaultInputDevice;

                if (_outputDeviceIndex < 0 || _outputDeviceIndex >= PortAudio.DeviceCount)
                    _outputDeviceIndex = PortAudio.DefaultOutputDevice;

                var inputParams = new StreamParameters
                {
                    device = _inputDeviceIndex,
                    channelCount = Channels,
                    sampleFormat = SampleFormat.Int16,
                    suggestedLatency = PortAudio.GetDeviceInfo(_inputDeviceIndex).defaultLowInputLatency
                };

                var outputParams = new StreamParameters
                {
                    device = _outputDeviceIndex,
                    channelCount = Channels,
                    sampleFormat = SampleFormat.Int16,
                    suggestedLatency = PortAudio.GetDeviceInfo(_outputDeviceIndex).defaultLowOutputLatency
                };

                _inputStream = new PaStream(
                    inParams: inputParams,
                    outParams: null,
                    sampleRate: SampleRate,
                    framesPerBuffer: FrameSize
                );

                _outputStream = new PaStream(
                    inParams: null,
                    outParams: outputParams,
                    sampleRate: SampleRate,
                    framesPerBuffer: FrameSize
                );

                _outputStream.Start();
                System.Diagnostics.Debug.WriteLine("Audio streams initialized");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Stream init error: {ex}");
            }
        }

        public void SetInputDevice(int deviceIndex)
        {
            _inputDeviceIndex = deviceIndex;
            Preferences.Set("InputDeviceIndex", deviceIndex);
            ReinitializeStreams();
        }

        public void SetOutputDevice(int deviceIndex)
        {
            _outputDeviceIndex = deviceIndex;
            Preferences.Set("OutputDeviceIndex", deviceIndex);
            ReinitializeStreams();
        }

        private void ReinitializeStreams()
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
                System.Diagnostics.Debug.WriteLine($"Reinit error: {ex}");
            }
        }

        // ---------------------------------------------------
        // CAPTURE LOOP (MIC)
        // ---------------------------------------------------
        private async Task CaptureLoop(CancellationToken ct)
        {
            short[] buffer = new short[FrameSize];

            while (!ct.IsCancellationRequested && _isActive)
            {
                try
                {
                    if (_isTransmitting && _inputStream != null)
                    {
                        _inputStream.Read(buffer, FrameSize);

                        // Encode Opus
                        var encoded = new byte[4000];
                        int len = _encoder!.Encode(buffer, 0, FrameSize, encoded, 0, encoded.Length);

                        if (len > 0)
                        {
                            byte[] packet = new byte[len];
                            Array.Copy(encoded, packet, len);
                            await SendVoicePacket(packet);
                        }
                    }
                    else
                    {
                        await Task.Delay(20, ct);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Capture error: {ex}");
                }
            }
        }

        private async Task SendVoicePacket(byte[] packet)
        {
            if (_udpClient == null) return;

            var endpoint = new IPEndPoint(IPAddress.Broadcast, _port);
            await _udpClient.SendAsync(packet, packet.Length, endpoint);
        }

        // ---------------------------------------------------
        // RECEIVE LOOP (SPEAKER)
        // ---------------------------------------------------
        private async Task ReceiveLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _isActive)
            {
                try
                {
                    var result = await _udpClient!.ReceiveAsync();
                    ProcessVoicePacket(result.Buffer);
                }
                catch
                {
                    await Task.Delay(20, ct);
                }
            }
        }

        private void ProcessVoicePacket(byte[] packet)
        {
            if (_decoder == null || _outputStream == null) return;

            try
            {
                short[] decoded = new short[FrameSize];
                int len = _decoder.Decode(packet, 0, packet.Length, decoded, 0, FrameSize, false);

                if (len > 0)
                    _outputStream.Write(decoded, len);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Decode error: {ex}");
            }
        }

        // ---------------------------------------------------
        // CLEANUP
        // ---------------------------------------------------
        public void Dispose()
        {
            _isActive = false;
            _cts?.Cancel();

            _inputStream?.Stop();
            _inputStream?.Dispose();

            _outputStream?.Stop();
            _outputStream?.Dispose();

            _udpClient?.Dispose();

            System.Diagnostics.Debug.WriteLine("VoiceService disposed");
        }
    }
}
