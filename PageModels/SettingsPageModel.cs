using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using PortAudioSharp;
using Proximity.Services;
using Proximity.Models;
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

        public ObservableCollection<AudioDeviceInfo> InputDevices { get; } = new();
        public ObservableCollection<AudioDeviceInfo> OutputDevices { get; } = new();

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
                    _themeService.IsDarkMode = value;
            }
        }

        public bool IsPushToTalk
        {
            get => _isPushToTalk;
            set
            {
                if (SetProperty(ref _isPushToTalk, value))
                {
                    _voiceService!.IsPushToTalk = value;
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
            get => InputDevices.FirstOrDefault(x => x.Index == _selectedInputDeviceIndex);
            set
            {
                if (value != null)
                {
                    _selectedInputDeviceIndex = value.Index;
                    Preferences.Set("InputDeviceIndex", value.Index);
                    _voiceService?.SetInputDevice(value.Index);
                    OnPropertyChanged();
                }
            }
        }

        public AudioDeviceInfo? SelectedOutputDevice
        {
            get => OutputDevices.FirstOrDefault(x => x.Index == _selectedOutputDeviceIndex);
            set
            {
                if (value != null)
                {
                    _selectedOutputDeviceIndex = value.Index;
                    Preferences.Set("OutputDeviceIndex", value.Index);
                    _voiceService?.SetOutputDevice(value.Index);
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

        // ---------------------------------------------------
        // LOAD DEVICES
        // ---------------------------------------------------
        private void LoadAudioDevices()
        {
            try
            {
                try { var x = PortAudio.DeviceCount; }
                catch { PortAudio.Initialize(); }

                InputDevices.Clear();
                OutputDevices.Clear();

                for (int i = 0; i < PortAudio.DeviceCount; i++)
                {
                    var info = PortAudio.GetDeviceInfo(i);

                    if (info.maxInputChannels > 0)
                        InputDevices.Add(new AudioDeviceInfo { Index = i, Name = info.name, IsDefault = i == PortAudio.DefaultInputDevice });

                    if (info.maxOutputChannels > 0)
                        OutputDevices.Add(new AudioDeviceInfo { Index = i, Name = info.name, IsDefault = i == PortAudio.DefaultOutputDevice });
                }

                _selectedInputDeviceIndex = Preferences.Get("InputDeviceIndex", PortAudio.DefaultInputDevice);
                _selectedOutputDeviceIndex = Preferences.Get("OutputDeviceIndex", PortAudio.DefaultOutputDevice);

                OnPropertyChanged(nameof(SelectedInputDevice));
                OnPropertyChanged(nameof(SelectedOutputDevice));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Device load error: {ex}");
            }
        }

        // ---------------------------------------------------
        // LOAD SETTINGS
        // ---------------------------------------------------
        private void LoadSettings()
        {
            Username = Preferences.Get("UserName", "User");
            IsDarkMode = _themeService.IsDarkMode;

            IsPushToTalk = Preferences.Get("IsPushToTalk", true);
            if (_voiceService != null)
                _voiceService.IsPushToTalk = IsPushToTalk;

            LocalPeerId = _discoveryService.GetLocalId();
        }

        // ---------------------------------------------------
        // SAVE USERNAME
        // ---------------------------------------------------
        private async void SaveUsername()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                await Application.Current!.MainPage!.DisplayAlert("Error", "Username cannot be empty", "OK");
                return;
            }

            Preferences.Set("UserName", Username);

            await Application.Current!.MainPage!.DisplayAlert("Success", "Username saved.", "OK");
        }

        // ---------------------------------------------------
        // LOOPBACK TEST
        // ---------------------------------------------------
        private void ToggleLoopback()
        {
            if (_isLoopbackActive)
                StopLoopback();
            else
                StartLoopback();
        }

        private void StartLoopback()
        {
            try
            {
                const int SampleRate = 44100;
                const int FrameSize = 441;

                var inputParams = new StreamParameters
                {
                    device = _selectedInputDeviceIndex,
                    channelCount = 1,
                    sampleFormat = SampleFormat.Int16,
                    suggestedLatency = PortAudio.GetDeviceInfo(_selectedInputDeviceIndex).defaultLowInputLatency
                };

                var outputParams = new StreamParameters
                {
                    device = _selectedOutputDeviceIndex,
                    channelCount = 1,
                    sampleFormat = SampleFormat.Int16,
                    suggestedLatency = PortAudio.GetDeviceInfo(_selectedOutputDeviceIndex).defaultLowOutputLatency
                };

                _loopbackInput = new PaStream(inputParams, null, SampleRate, FrameSize);
                _loopbackOutput = new PaStream(null, outputParams, SampleRate, FrameSize);

                _loopbackInput.Start();
                _loopbackOutput.Start();

                _isLoopbackActive = true;
                LoopbackButtonText = "Stop Test";
                LoopbackStatus = "🎤 Speak to test...";

                _loopbackCts = new System.Threading.CancellationTokenSource();
                _ = LoopbackThread(_loopbackCts.Token, FrameSize);
            }
            catch (Exception ex)
            {
                LoopbackStatus = "❌ Error starting loopback";
                System.Diagnostics.Debug.WriteLine($"Loopback start error: {ex}");
            }
        }

        private async System.Threading.Tasks.Task LoopbackThread(System.Threading.CancellationToken ct, int frameSize)
        {
            short[] buffer = new short[frameSize];

            while (!ct.IsCancellationRequested && _isLoopbackActive)
            {
                try
                {
                    _loopbackInput!.Read(buffer, frameSize);
                    _loopbackOutput!.Write(buffer, frameSize);
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
                LoopbackStatus = "Loopback test stopped";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Loopback stop error: {ex}");
            }
        }
    }

    
}
