using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Models;

namespace Proximity.Services
{

    public class ChatService : IDisposable
    {
        private const int MESSAGE_PORT = 9001;
        private const int MAX_MESSAGE_SIZE = 65536; // 64KB

        private TcpListener _tcpServer;
        private CancellationTokenSource _cts;
        private Task _serverTask;
        private bool _isRunning = false;

        public string MyDeviceId { get; private set; }
        public string MyDeviceName { get; private set; }

        public event Action<ChatMessage> OnMessageReceived;
        public event Action<string> OnTypingStatus; // deviceId of typing peer

        public ChatService(string deviceId, string deviceName)
        {
            MyDeviceId = deviceId;
            MyDeviceName = deviceName;
        }

        public async Task StartAsync()
        {
            if (_isRunning) return;

            try
            {
                _cts = new CancellationTokenSource();

                // Start TCP server to receive messages
                _tcpServer = new TcpListener(IPAddress.Any, MESSAGE_PORT);
                _tcpServer.Start();

                _serverTask = Task.Run(() => AcceptClientsLoopAsync(_cts.Token));

                _isRunning = true;
                Debug.WriteLine($"[Messaging] Server started on port {MESSAGE_PORT}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Messaging] Start failed: {ex.Message}");
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;

            try
            {
                _cts?.Cancel();
                await (_serverTask ?? Task.CompletedTask);

                _tcpServer?.Stop();

                _isRunning = false;
                Debug.WriteLine("[Messaging] Server stopped");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Messaging] Stop error: {ex.Message}");
            }
        }

        private async Task AcceptClientsLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _tcpServer.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client, token));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Messaging] Accept error: {ex.Message}");
                    await Task.Delay(100, token);
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            try
            {
                using (client)
                {
                    var stream = client.GetStream();
                    var buffer = new byte[MAX_MESSAGE_SIZE];

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                    cts.CancelAfter(TimeSpan.FromSeconds(30)); // Timeout per message

                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);

                    if (bytesRead > 0)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Debug.WriteLine($"[Messaging] Received: {json}");

                        var message = JsonSerializer.Deserialize<ChatMessage>(json);

                        if (message != null)
                        {
                            // Invoke on main thread for UI updates
                            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                if (message.Type == "text")
                                {
                                    OnMessageReceived?.Invoke(message);
                                }
                                else if (message.Type == "typing")
                                {
                                    OnTypingStatus?.Invoke(message.FromDeviceId);
                                }
                            });
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[Messaging] Client handler timeout");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Messaging] Handle client error: {ex.Message}");
            }
        }

        public async Task<bool> SendMessageAsync(PeerInfo targetPeer, string text)
        {
            if (targetPeer == null || string.IsNullOrEmpty(text))
                return false;

            var message = new ChatMessage
            {
                MessageId = Guid.NewGuid().ToString("N"),
                Type = "text",
                FromDeviceId = MyDeviceId,
                FromDeviceName = MyDeviceName,
                ToDeviceId = targetPeer.DeviceId,
                Text = text,
                Timestamp = DateTime.UtcNow
            };

            return await SendMessageAsync(targetPeer.IpAddress, targetPeer.Port, message);
        }

        public async Task<bool> SendTypingStatusAsync(PeerInfo targetPeer, bool isTyping)
        {
            if (targetPeer == null)
                return false;

            var message = new ChatMessage
            {
                Type = "typing",
                FromDeviceId = MyDeviceId,
                FromDeviceName = MyDeviceName,
                ToDeviceId = targetPeer.DeviceId,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object> { { "isTyping", isTyping } }
            };

            return await SendMessageAsync(targetPeer.IpAddress, targetPeer.Port, message);
        }

        private async Task<bool> SendMessageAsync(string targetIp, int targetPort, ChatMessage message)
        {
            TcpClient client = null;
            try
            {
                client = new TcpClient();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await client.ConnectAsync(targetIp, targetPort, cts.Token);

                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);

                var stream = client.GetStream();
                await stream.WriteAsync(bytes, 0, bytes.Length, cts.Token);
                await stream.FlushAsync(cts.Token);

                Debug.WriteLine($"[Messaging] Sent to {targetIp}:{targetPort} - Type: {message.Type}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Messaging] Send error to {targetIp}:{targetPort} - {ex.Message}");
                return false;
            }
            finally
            {
                client?.Close();
            }
        }

        public void Dispose()
        {
            StopAsync().Wait();
            _cts?.Dispose();
        }
    }
}