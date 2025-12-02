using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using PortAudioSharp;
using Proximity.Services;
using PaStream = PortAudioSharp.Stream;

namespace Proximity.PageModels
{
    public class SettingsPageModel : BasePageModel
    {
        private readonly DiscoveryService _discoveryService;
        private readonly VoiceService? _voiceService;
        private readonly ThemeService _themeService;

        private PaStream? _loopbackInput;
        private PaStream? _loopbackOutput;
        private bool _isLoopbackActive;
        private System.Threading.CancellationTokenSource? _loopbackCts;

        private string _username = string.Empty;
        private bool _isDarkMode;
        private bool _isPushToTalk = true;
        private string _localPeerId = string.Empty;
        private string _loopbackButtonText = "Start Test";
        private string _loopbackStatus = string.Empty;
        private int _selectedInputDeviceIndex = -1;
        private int _selectedOutputDeviceIndex = -1;

        public ObservableCollection<AudioDeviceInfo> InputDevices { get; } = new ObservableCollection<AudioDeviceInfo>();
        public ObservableCollection<AudioDeviceInfo> OutputDevices { get; } = new ObservableCollection<AudioDeviceInfo>();

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (SetProperty(ref _isDarkMode, value))
                {
                    _themeService.IsDarkMode = value;
                }
            }
        }

        public bool IsPushToTalk
        {
            get => _isPushToTalk;
            set
            {
                if (SetProperty(ref _isPushToTalk, value) && _voiceService != null)
                {
                    _voiceService.IsPushToTalk = value;
                    Preferences.Set("IsPushToTalk", value);
                }
            }
        }

        public string LocalPeerId
        {
            get => _localPeerId;
            set => SetProperty(ref _localPeerId, value);
        }

        public AudioDeviceInfo? SelectedInputDevice
        {
            get => InputDevices.FirstOrDefault(d => d.Index == _selectedInputDeviceIndex);
            set
            {
                if (value != null)
                {
                    _selectedInputDeviceIndex = value.Index;
                    _voiceService?.SetInputDevice(value.Index);
                    Preferences.Set("InputDeviceIndex", value.Index);
                    OnPropertyChanged();
                }
            }
        }

        public AudioDeviceInfo? SelectedOutputDevice
        {
            get => OutputDevices.FirstOrDefault(d => d.Index == _selectedOutputDeviceIndex);
            set
            {
                if (value != null)
                {
                    _selectedOutputDeviceIndex = value.Index;
                    _voiceService?.SetOutputDevice(value.Index);
                    Preferences.Set("OutputDeviceIndex", value.Index);
                    OnPropertyChanged();
                }
            }
        }

        public string LoopbackButtonText
        {
            get => _loopbackButtonText;
            set => SetProperty(ref _loopbackButtonText, value);
        }

        public string LoopbackStatus
        {
            get => _loopbackStatus;
            set => SetProperty(ref _loopbackStatus, value);
        }

        public ICommand SaveUsernameCommand { get; }
        public ICommand ToggleLoopbackCommand { get; }

        public SettingsPageModel(DiscoveryService discoveryService, VoiceService? voiceService)
        {
            _discoveryService = discoveryService;
            _voiceService = voiceService;
            _themeService = ThemeService.Instance;

            SaveUsernameCommand = new Command(SaveUsername);
            ToggleLoopbackCommand = new Command(ToggleLoopback);

            LoadAudioDevices();
            LoadSettings();
        }

        private void LoadAudioDevices()
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

                // Load input devices
                InputDevices.Clear();
                for (int i = 0; i < PortAudio.DeviceCount; i++)
                {
                    var info = PortAudio.GetDeviceInfo(i);
                    if (info.maxInputChannels > 0)
                    {
                        InputDevices.Add(new AudioDeviceInfo
                        {
                            Index = i,
                            Name = info.name,
                            IsDefault = i == PortAudio.DefaultInputDevice
                        });
                    }
                }

                // Load output devices
                OutputDevices.Clear();
                for (int i = 0; i < PortAudio.DeviceCount; i++)
                {
                    var info = PortAudio.GetDeviceInfo(i);
                    if (info.maxOutputChannels > 0)
                    {
                        OutputDevices.Add(new AudioDeviceInfo
                        {
                            Index = i,
                            Name = info.name,
                            IsDefault = i == PortAudio.DefaultOutputDevice
                        });
                    }
                }

                // Set saved or default devices
                _selectedInputDeviceIndex = Preferences.Get("InputDeviceIndex", PortAudio.DefaultInputDevice);
                _selectedOutputDeviceIndex = Preferences.Get("OutputDeviceIndex", PortAudio.DefaultOutputDevice);

                OnPropertyChanged(nameof(SelectedInputDevice));
                OnPropertyChanged(nameof(SelectedOutputDevice));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading audio devices: {ex.Message}");

                // Add fallback devices if PortAudio fails
                if (InputDevices.Count == 0)
                {
                    InputDevices.Add(new AudioDeviceInfo { Index = 0, Name = "Default Input", IsDefault = true });
                }
                if (OutputDevices.Count == 0)
                {
                    OutputDevices.Add(new AudioDeviceInfo { Index = 0, Name = "Default Output", IsDefault = true });
                }
            }
        }

        private void LoadSettings()
        {
            Username = Preferences.Get("UserName", "User");
            IsDarkMode = _themeService.IsDarkMode;
            IsPushToTalk = Preferences.Get("IsPushToTalk", true);

            // Update VoiceService if available
            if (_voiceService != null)
            {
                _voiceService.IsPushToTalk = IsPushToTalk;
            }

            LocalPeerId = _discoveryService.GetLocalId();
        }

        private async void SaveUsername()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Error",
                    "Username cannot be empty",
                    "OK"
                );
                return;
            }

            Preferences.Set("UserName", Username);

            // Also update display name for backward compatibility
            if (string.IsNullOrEmpty(Preferences.Get("ProfileDisplayName", "")))
            {
                Preferences.Set("ProfileDisplayName", Username);
            }

            await Application.Current!.MainPage!.DisplayAlert(
                "Success",
                "Username saved! Restart the app for changes to take full effect.",
                "OK"
            );
        }

        private void ToggleLoopback()
        {
            if (_isLoopbackActive)
            {
                StopLoopback();
            }
            else
            {
                StartLoopback();
            }
        }

        private void StartLoopback()
        {
            try
            {
                const int SampleRate = 44100;
                const int FrameSize = 441; // 10ms at 44.1kHz

                if (_selectedInputDeviceIndex < 0 || _selectedOutputDeviceIndex < 0)
                {
                    LoopbackStatus = "❌ Please select audio devices first";
                    return;
                }

                // Input parameters
                var inputParams = new StreamParameters
                {
                    device = _selectedInputDeviceIndex,
                    channelCount = 1,
                    sampleFormat = SampleFormat.Int16,
                    suggestedLatency = PortAudio.GetDeviceInfo(_selectedInputDeviceIndex).defaultLowInputLatency
                };

                // Output parameters
                var outputParams = new StreamParameters
                {
                    device = _selectedOutputDeviceIndex,
                    channelCount = 1,
                    sampleFormat = SampleFormat.Int16,
                    suggestedLatency = PortAudio.GetDeviceInfo(_selectedOutputDeviceIndex).defaultLowOutputLatency
                };

                // Open streams
                _loopbackInput = new PaStream(
                    inParams: inputParams,
                    outParams: null,
                    sampleRate: SampleRate,
                    framesPerBuffer: FrameSize,
                    streamFlags: StreamFlags.ClipOff,
                    callback: null,
                    userData: null
                );

                _loopbackOutput = new PaStream(
                    inParams: null,
                    outParams: outputParams,
                    sampleRate: SampleRate,
                    framesPerBuffer: FrameSize,
                    streamFlags: StreamFlags.ClipOff,
                    callback: null,
                    userData: null
                );

                _loopbackInput.Start();
                _loopbackOutput.Start();

                _isLoopbackActive = true;
                LoopbackButtonText = "Stop Test";
                LoopbackStatus = "🎤 Listening... Speak to test";

                // Start loopback thread
                _loopbackCts = new System.Threading.CancellationTokenSource();
                _ = LoopbackThread(_loopbackCts.Token, FrameSize);

                System.Diagnostics.Debug.WriteLine("Loopback test started");
            }
            catch (Exception ex)
            {
                LoopbackStatus = $"❌ Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Loopback error: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoopbackThread(System.Threading.CancellationToken ct, int frameSize)
        {
            var buffer = new short[frameSize];

            while (!ct.IsCancellationRequested && _isLoopbackActive)
            {
                try
                {
                    if (_loopbackInput != null && _loopbackOutput != null)
                    {
                        _loopbackInput.Read(buffer, frameSize);
                        _loopbackOutput.Write(buffer, frameSize);
                    }
                }
                catch
                {
                    await System.Threading.Tasks.Task.Delay(10, ct);
                }
            }
        }

        private void StopLoopback()
        {
            try
            {
                _loopbackCts?.Cancel();

                _loopbackInput?.Stop();
                _loopbackInput?.Dispose();
                _loopbackInput = null;

                _loopbackOutput?.Stop();
                _loopbackOutput?.Dispose();
                _loopbackOutput = null;

                _isLoopbackActive = false;
                LoopbackButtonText = "Start Test";
                LoopbackStatus = "✅ Test stopped";

                System.Diagnostics.Debug.WriteLine("Loopback test stopped");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping loopback: {ex.Message}");
            }
        }
    }

    // Helper class for audio device info
    public class AudioDeviceInfo
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDefault { get; set; }

        public override string ToString()
        {
            return IsDefault ? $"{Name} (Default)" : Name;
        }
    }
}