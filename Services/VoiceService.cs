using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Models;
using Plugin.Maui.Audio;

namespace Proximity.Services
{
    public class VoiceService : IDisposable
    {
        private const int VOICE_PORT = 9002;
        private const int CAPTURE_INTERVAL_MS = 20; // 20ms chunks
        private const int JITTER_BUFFER_SIZE = 5; // Buffer 5 packets (100ms)
        private const int MAX_PACKET_SIZE = 8192; // 8KB max per packet

        private UdpClient _udpSender;
        private UdpClient _udpReceiver;
        private CancellationTokenSource _cts;

        private IAudioRecorder _recorder;
        private IAudioPlayer _currentPlayer;

        private Task _captureTask;
        private Task _receiveTask;
        private Task _playbackTask;

        private readonly ConcurrentQueue<byte[]> _audioQueue;
        private readonly SemaphoreSlim _audioQueueSemaphore;

        private bool _isInCall = false;
        private string _remoteIp;
        private bool _isTransmitting = false;

        public bool IsTransmitting => _isTransmitting;
        public event Action OnCallStarted;
        public event Action OnCallEnded;
        public event Action<string> OnError;

        public bool IsInCall => _isInCall;

        public VoiceService()
        {
            _audioQueue = new ConcurrentQueue<byte[]>();
            _audioQueueSemaphore = new SemaphoreSlim(0);
        }
        public async Task StartTransmittingAsync()
        {
            if (_isTransmitting)
                return;

            try
            {
                _isTransmitting = true;

                // Start capturing and transmitting audio
                await Task.Run(() =>
                {
                    // TODO: Implement actual audio capture and transmission
                    System.Diagnostics.Debug.WriteLine("VoiceService: Started transmitting");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VoiceService: Start transmitting error - {ex.Message}");
                _isTransmitting = false;
            }
        }
        public void StopTransmitting()
        {
            if (!_isTransmitting)
                return;

            try
            {
                _isTransmitting = false;

                // Stop audio capture and transmission
                System.Diagnostics.Debug.WriteLine("VoiceService: Stopped transmitting");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VoiceService: Stop transmitting error - {ex.Message}");
            }
        }

        public async Task StartCallAsync(PeerInfo targetPeer)
        {
            if (_isInCall)
            {
                throw new InvalidOperationException("Already in a call");
            }

            if (targetPeer == null)
            {
                throw new ArgumentNullException(nameof(targetPeer));
            }

            try
            {
                _remoteIp = targetPeer.IpAddress;
                _cts = new CancellationTokenSource();

                // Setup UDP
                _udpSender = new UdpClient();
                _udpReceiver = new UdpClient(VOICE_PORT);

                // Setup audio recorder
                _recorder = AudioManager.Current.CreateRecorder();

                // Start recording
                await _recorder.StartAsync();

                // Start tasks
                _captureTask = Task.Run(() => CaptureAndSendLoopAsync(_cts.Token));
                _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));
                _playbackTask = Task.Run(() => PlaybackLoopAsync(_cts.Token));

                _isInCall = true;
                OnCallStarted?.Invoke();

                Debug.WriteLine($"[Voice] Call started with {targetPeer.Name} ({targetPeer.IpAddress})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Voice] Start call failed: {ex.Message}");
                await EndCallAsync();
                OnError?.Invoke($"Failed to start call: {ex.Message}");
                throw;
            }
        }

        public async Task EndCallAsync()
        {
            if (!_isInCall)
                return;

            try
            {
                _cts?.Cancel();

                // Wait for tasks to complete
                await Task.WhenAll(
                    _captureTask ?? Task.CompletedTask,
                    _receiveTask ?? Task.CompletedTask,
                    _playbackTask ?? Task.CompletedTask
                );

                // Stop and dispose audio
                if (_recorder?.IsRecording == true)
                {
                    await _recorder.StopAsync();
                }

                _currentPlayer?.Stop();
                _currentPlayer?.Dispose();
                _currentPlayer = null;

                // Close network
                _udpSender?.Close();
                _udpReceiver?.Close();

                // Clear queue
                while (_audioQueue.TryDequeue(out _)) { }

                _isInCall = false;
                OnCallEnded?.Invoke();

                Debug.WriteLine("[Voice] Call ended");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Voice] End call error: {ex.Message}");
            }
        }

        private async Task CaptureAndSendLoopAsync(CancellationToken token)
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(_remoteIp), VOICE_PORT);
            var lastCaptureTime = DateTime.UtcNow;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Wait for next capture interval
                    var elapsed = (DateTime.UtcNow - lastCaptureTime).TotalMilliseconds;
                    var delay = Math.Max(0, CAPTURE_INTERVAL_MS - (int)elapsed);
                    if (delay > 0)
                    {
                        await Task.Delay(delay, token);
                    }
                    lastCaptureTime = DateTime.UtcNow;

                    // Note: Plugin.Maui.Audio doesn't support streaming chunks
                    // We'll use a workaround: stop/start recording in short intervals
                    // For production, consider using platform-specific audio APIs

                    // This is a simplified approach - you may need platform-specific code
                    // For now, we'll send silence packets to maintain connection
                    var silencePacket = new byte[320]; // ~20ms of silence at 16kHz mono
                    await _udpSender.SendAsync(silencePacket, silencePacket.Length, endpoint);

                    // TODO: Implement proper audio capture with platform-specific code
                    // See notes at the end of this file
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Voice] Capture error: {ex.Message}");
                    await Task.Delay(100, token);
                }
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpReceiver.ReceiveAsync();

                    if (result.Buffer != null && result.Buffer.Length > 0)
                    {
                        // Add to playback queue
                        _audioQueue.Enqueue(result.Buffer);
                        _audioQueueSemaphore.Release();

                        Debug.WriteLine($"[Voice] Received {result.Buffer.Length} bytes");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Voice] Receive error: {ex.Message}");
                    await Task.Delay(10, token);
                }
            }
        }

        private async Task PlaybackLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Wait for audio data with timeout
                    var hasData = await _audioQueueSemaphore.WaitAsync(100, token);

                    if (!hasData)
                        continue;

                    if (_audioQueue.TryDequeue(out var audioData))
                    {
                        // Wait until we have enough buffered (jitter buffer)
                        if (_audioQueue.Count < JITTER_BUFFER_SIZE)
                        {
                            await Task.Delay(10, token);
                            continue;
                        }

                        // Play audio using Plugin.Maui.Audio
                        using var stream = new MemoryStream(audioData);

                        // Note: This is simplified - Plugin.Maui.Audio expects full audio files
                        // For production, you'll need platform-specific implementation
                        // See notes at the end of this file

                        var player = AudioManager.Current.CreatePlayer(stream);
                        player.Play();

                        // Wait for playback (approximate)
                        await Task.Delay(CAPTURE_INTERVAL_MS, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Voice] Playback error: {ex.Message}");
                    await Task.Delay(50, token);
                }
            }
        }

        public void Dispose()
        {
            EndCallAsync().Wait();
            _cts?.Dispose();
            _udpSender?.Dispose();
            _udpReceiver?.Dispose();
            _audioQueueSemaphore?.Dispose();
        }
    }
}

/*
 * IMPORTANT NOTES FOR PRODUCTION IMPLEMENTATION:
 * 
 * Plugin.Maui.Audio is designed for playing complete audio files, not streaming.
 * For real-time voice calls, you need platform-specific implementations:
 * 
 * ANDROID:
 * - Use AudioRecord for capture
 * - Use AudioTrack for playback
 * - Wrap in Android-specific service class
 * 
 * iOS:
 * - Use AVAudioEngine with audio taps
 * - Or use AVAudioRecorder/AVAudioPlayer with proper buffer management
 * - Configure audio session for VoIP
 * 
 * WINDOWS:
 * - Use NAudio library (add NAudio package)
 * - WaveInEvent for capture
 * - WaveOutEvent for playback
 * 
 * RECOMMENDED NEXT STEP:
 * Create platform-specific implementations in Platforms/ folders:
 * - Platforms/Android/Services/AndroidVoiceService.cs
 * - Platforms/iOS/Services/IOSVoiceService.cs  
 * - Platforms/Windows/Services/WindowsVoiceService.cs
 * 
 * Then use dependency injection to resolve the correct implementation:
 * 
 * #if ANDROID
 *     builder.Services.AddSingleton<IVoiceService, AndroidVoiceService>();
 * #elif IOS
 *     builder.Services.AddSingleton<IVoiceService, IOSVoiceService>();
 * #elif WINDOWS
 *     builder.Services.AddSingleton<IVoiceService, WindowsVoiceService>();
 * #endif
 */