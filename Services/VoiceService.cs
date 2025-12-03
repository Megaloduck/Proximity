using PortAudioSharp;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Proximity.Services
{
    public sealed class VoiceService : IDisposable
    {
        // --- Configuration ---
        public const int SampleRate = 48000; // 48 kHz as intended
        public const int Channels = 1; // mono
        public const int FramesPerBuffer = 960; // 20 ms @ 48 kHz -> 960 samples
        public const int BytesPerSample = 2; // 16-bit PCM
        public const int PayloadBufferSize = FramesPerBuffer * BytesPerSample * Channels;

        private readonly UdpClient _udpSender;
        private readonly UdpClient _udpReceiver;
        private IPEndPoint _remoteEndPoint;

        private readonly CancellationTokenSource _cts = new();
        private Task _captureTask;
        private Task _playbackTask;
        private Task _receiveTask;
        // Jitter buffer queue for playback
        private readonly BlockingCollection<byte[]> _jitterBuffer = new(new ConcurrentQueue<byte[]>(), boundedCapacity: 256);

        // PortAudio stream handles
        private IntPtr _inputStream = IntPtr.Zero;
        private IntPtr _outputStream = IntPtr.Zero;
        private bool _initialized = false;

        // Optional: external encoder (e.g., Concentus Opus). If null, raw PCM is used.
        private readonly bool _useOpus;
        // If you wire Concentus, implement IOpusWrapper accordingly. For now we assume raw PCM.

        // Network settings
        public int LocalPort { get; }
        public int RemotePort { get; }
        public bool IsRunning { get; private set; } = false;

        public VoiceService(int localPort = 9003, int remotePort = 9003, bool useOpus = false)
        {
            LocalPort = localPort;
            RemotePort = remotePort;
            _useOpus = useOpus;
            _udpSender = new UdpClient();
            _udpReceiver = new UdpClient(new IPEndPoint(IPAddress.Any, LocalPort));

            // For multicast scenarios, you may need to join groups. For simple LAN broadcast/peer-to-peer we'll use direct IPs.
        }
        public void InitializeAudio()
        {
            if (_initialized) return;
            var err = PortAudio.Pa_Initialize();
            if (err != PortAudio.PaErrorCode.paNoError)
            {
                throw new InvalidOperationException($"PortAudio initialize failed: {err}");
            }
            _initialized = true;
        }
        public void SetRemoteEndpoint(string remoteIpOrHostname)
        {
            if (string.IsNullOrWhiteSpace(remoteIpOrHostname)) throw new ArgumentNullException(nameof(remoteIpOrHostname));
            var ip = IPAddress.Parse(remoteIpOrHostname);
            _remoteEndPoint = new IPEndPoint(ip, RemotePort);
        }
        public void Start()
        {
            if (IsRunning) return;
            InitializeAudio();

            // Open PortAudio streams
            OpenInputStream();
            OpenOutputStream();

            // Start streams
            StartInputStream();
            StartOutputStream();

            // Start network receive loop
            _receiveTask = Task.Run(ReceiveLoopAsync, _cts.Token);

            // Start capture and send loop
            _captureTask = Task.Run(CaptureLoopAsync, _cts.Token);

            // Start playback loop that takes from jitter buffer and writes to output device
            _playbackTask = Task.Run(PlaybackLoopAsync, _cts.Token);

            IsRunning = true;
        }
        public void Stop()
        {
            if (!IsRunning) return;
            _cts.Cancel();

            try { _captureTask?.Wait(2000); } catch { }
            try { _receiveTask?.Wait(2000); } catch { }
            try { _playbackTask?.Wait(2000); } catch { }

            StopInputStream();
            StopOutputStream();
            CloseInputStream();
            CloseOutputStream();

            _jitterBuffer?.CompleteAdding();

            try
            {
                if (_initialized)
                {
                    PortAudio.Pa_Terminate();
                    _initialized = false;
                }
            }
            catch { }

            IsRunning = false;
        }
        private async Task CaptureLoopAsync()
        {
            var token = _cts.Token;
            var frameBytes = PayloadBufferSize;
            var buffer = new byte[frameBytes];

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Read raw PCM frames from PortAudio input
                    var framesToRead = FramesPerBuffer;
                    var readErr = PortAudio.Pa_ReadStream(_inputStream, buffer, (ulong)framesToRead);
                    if (readErr != PortAudio.PaErrorCode.paNoError)
                    {
                        Debug.WriteLine($"Pa_ReadStream error: {readErr}");
                        await Task.Delay(10, token).ConfigureAwait(false);
                        continue;
                    }

                    // Optionally encode (Opus) - omitted here for brevity
                    // For now send raw PCM (16-bit little endian)

                    if (_remoteEndPoint != null)
                    {
                        await _udpSender.SendAsync(buffer, buffer.Length, _remoteEndPoint).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CaptureLoop exception: {ex}");
                    await Task.Delay(50, token).ConfigureAwait(false);
                }
            }
        }
        private async Task ReceiveLoopAsync()
        {
            var token = _cts.Token;
            while (!token.IsCancellationRequested)
            {
                try
                { var result = await _udpReceiver.ReceiveAsync().ConfigureAwait(false);
                    // We expect raw PCM frames of PayloadBufferSize
                    if (result.Buffer != null && result.Buffer.Length > 0)
                    {
                        // Enqueue to jitter buffer. If full, oldest frames will be dropped by bounded queue behavior.
                        if (!_jitterBuffer.IsAddingCompleted)
                        {
                            // Clone buffer to avoid reuse issues
                            var frame = new byte[result.Buffer.Length];
                            Buffer.BlockCopy(result.Buffer, 0, frame, 0, result.Buffer.Length);
                            // Non-blocking attempt
                            try { _jitterBuffer.Add(frame, token); } catch { /* drop if cannot */ }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ReceiveLoop exception: {ex}");
                    await Task.Delay(20, token).ConfigureAwait(false);
                }
            }
        }
        private async Task PlaybackLoopAsync()
        {
            var token = _cts.Token;
            while (!token.IsCancellationRequested)
            {
                try
                { byte[] frame = null;
                    try { frame = _jitterBuffer.Take(token); } catch { frame = null; }
                    if (frame == null) { await Task.Delay(10, token).ConfigureAwait(false); continue; }


                    // Optionally decode (Opus) - omitted here
                    // Write raw PCM to PortAudio output
                    var framesToWrite = FramesPerBuffer;
                    var writeErr = PortAudio.Pa_WriteStream(_outputStream, frame, (ulong)framesToWrite);
                    if (writeErr != PortAudio.PaErrorCode.paNoError)
                    {
                        Debug.WriteLine($"Pa_WriteStream error: {writeErr}");
                        await Task.Delay(10, token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Debug.WriteLine($"PlaybackLoop exception: {ex}");
                    await Task.Delay(20, token).ConfigureAwait(false);
                }
            }
        }

        // -------------------------
        // PortAudio stream helpers
        // -------------------------
        private void OpenInputStream()
        {
            if (_inputStream != IntPtr.Zero) return;
            var inputParameters = new PortAudio.PaStreamParameters
            {
                device = PortAudio.Pa_GetDefaultInputDevice(),
                channelCount = Channels,
                sampleFormat = PortAudio.PaSampleFormat.paInt16,
                suggestedLatency = PortAudio.Pa_GetDeviceInfo(PortAudio.Pa_GetDefaultInputDevice())?.defaultLowInputLatency ?? 0.05
            };

            IntPtr streamPtr;
            var err = PortAudio.Pa_OpenStream(
            out streamPtr,
            ref inputParameters,
            IntPtr.Zero,
            SampleRate,
            FramesPerBuffer,
            PortAudio.PaStreamFlags.paNoFlag,
            IntPtr.Zero,
            IntPtr.Zero);

            if (err != PortAudio.PaErrorCode.paNoError)
                throw new InvalidOperationException($"Unable to open input stream: {err}");

            _inputStream = streamPtr;
        }
        private void OpenOutputStream()
        {
            if (_outputStream != IntPtr.Zero) return;
            var outputParameters = new PortAudio.PaStreamParameters
            {
                device = PortAudio.Pa_GetDefaultOutputDevice(),
                channelCount = Channels,
                sampleFormat = PortAudio.PaSampleFormat.paInt16,
                suggestedLatency = PortAudio.Pa_GetDeviceInfo(PortAudio.Pa_GetDefaultOutputDevice())?.defaultLowOutputLatency ?? 0.05
            };

            IntPtr streamPtr;
            var err = PortAudio.Pa_OpenStream(
            out streamPtr,
            IntPtr.Zero,
            ref outputParameters,
            SampleRate,
            FramesPerBuffer,
            PortAudio.PaStreamFlags.paNoFlag,
            IntPtr.Zero,
            IntPtr.Zero);

            if (err != PortAudio.PaErrorCode.paNoError)
                throw new InvalidOperationException($"Unable to open output stream: {err}");

            _outputStream = streamPtr;
        }
        private void StartInputStream()
        {
            if (_inputStream == IntPtr.Zero) return;
            var err = PortAudio.Pa_StartStream(_inputStream);
            if (err != PortAudio.PaErrorCode.paNoError) throw new InvalidOperationException($"Pa_StartStream input failed: {err}");
        }
        private void StartOutputStream()
        {
            if (_outputStream == IntPtr.Zero) return;
            var err = PortAudio.Pa_StartStream(_outputStream);
            if (err != PortAudio.PaErrorCode.paNoError) throw new InvalidOperationException($"Pa_StartStream output failed: {err}");
        }
        private void StopInputStream()
        {
            if (_inputStream == IntPtr.Zero) return;
            try { PortAudio.Pa_StopStream(_inputStream); } catch { }
        }
        private void StopOutputStream()
        {
            if (_outputStream == IntPtr.Zero) return;
            try { PortAudio.Pa_StopStream(_outputStream); } catch { }
        }
        private void CloseInputStream()
        {
            if (_inputStream == IntPtr.Zero) return;
            try { PortAudio.Pa_CloseStream(_inputStream); } catch { }
            _inputStream = IntPtr.Zero;
        }
        private void CloseOutputStream()
        {
            if (_outputStream == IntPtr.Zero) return;
            try { PortAudio.Pa_CloseStream(_outputStream); } catch { }
            _outputStream = IntPtr.Zero;
        }


        public void Dispose()
        {
            Stop();
            _udpSender?.Dispose();
            _udpReceiver?.Dispose();
            _cts?.Dispose();
        }
    }
}
// ----------------------------
// Minimal PortAudio P/Invoke
// ----------------------------
internal static class PortAudio
{
    // Error codes (partial)
    public enum PaErrorCode : int
    {
        paNoError = 0,
        paNotInitialized = -10000,
        paUnanticipatedHostError = -9999,
        // add more if needed
    }
    [Flags]
    public enum PaSampleFormat : ulong
    {
        paFloat32 = 0x00000001,
        paInt32 = 0x00000002,
        paInt24 = 0x00000004,
        paInt16 = 0x00000008,
        paInt8 = 0x00000010,
        paUInt8 = 0x00000020
    }
    [Flags]
    public enum PaStreamFlags : ulong
    {
        paNoFlag = 0
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PaStreamParameters
    {
        public int device;
        public int channelCount;
        public PaSampleFormat sampleFormat;
        public double suggestedLatency;
        public IntPtr hostApiSpecificStreamInfo; // unused
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PaDeviceInfo
    {
        public int structVersion;
        public IntPtr name;
        public int hostApi;
        public int maxInputChannels;
        public int maxOutputChannels;
        public double defaultSampleRate;
        public double defaultLowInputLatency;
        public double defaultLowOutputLatency;
        public double defaultHighInputLatency;
        public double defaultHighOutputLatency;
    }

    // DllImport: library name might differ per platform. 'portaudio' is commonly used.
    private const string LIB = "portaudio";
    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern PaErrorCode Pa_Initialize();

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern PaErrorCode Pa_Terminate();

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Pa_GetDefaultInputDevice();

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Pa_GetDefaultOutputDevice();

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr Pa_GetDeviceInfoPtr(int device);

    // We wrap GetDeviceInfo to marshal properly
    public static PaDeviceInfo? Pa_GetDeviceInfo(int device)
    {
        try
        {
            var ptr = Pa_GetDeviceInfoPtr(device);
            if (ptr == IntPtr.Zero) return null;
            return Marshal.PtrToStructure<PaDeviceInfo>(ptr);
        }
        catch { return null; }
    }
    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern PaErrorCode Pa_OpenStream(out IntPtr stream,
        ref PaStreamParameters inputParameters,
        IntPtr outputParameters,
        int sampleRate,
        int framesPerBuffer,
        PaStreamFlags streamFlags,
        IntPtr streamCallback,
        IntPtr userData);

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern PaErrorCode Pa_OpenStream(out IntPtr stream,
        IntPtr inputParameters,
        ref PaStreamParameters outputParameters,
        int sampleRate,
        int framesPerBuffer,
        PaStreamFlags streamFlags,
        IntPtr streamCallback,
        IntPtr userData);

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern PaErrorCode Pa_CloseStream(IntPtr stream);

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern PaErrorCode Pa_StartStream(IntPtr stream);

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern PaErrorCode Pa_StopStream(IntPtr stream);

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern PaErrorCode Pa_ReadStream(IntPtr stream, [Out] byte[] buffer, ulong frames);

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern PaErrorCode Pa_WriteStream(IntPtr stream, [In] byte[] buffer, ulong frames);

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Pa_GetErrorText(PaErrorCode code);
    public static string PaErrorText(PaErrorCode code)
    {
        try
        {
            var ptr = Pa_GetErrorText(code);
            if (ptr == IntPtr.Zero) return code.ToString();
            return Marshal.PtrToStringAnsi(ptr) ?? code.ToString();
        }
        catch { return code.ToString(); }
    }
}
