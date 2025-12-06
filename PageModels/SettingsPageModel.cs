using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Proximity.Services;
using Proximity.Models;
using Plugin.Maui.Audio;

namespace Proximity.PageModels
{
    public class SettingsPageModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly DiscoveryService _discoveryService;
        private readonly VoiceService _voiceService;
        private IAudioRecorder _loopbackRecorder;
        private IAudioPlayer _loopbackPlayer;
        private bool _isLoopbackRunning = false;
        private bool _disposed = false;

        // Appearance
        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                _isDarkMode = value;
                OnPropertyChanged();
                ApplyTheme();
            }
        }

        // Audio Devices
        public ObservableCollection<AudioDeviceInfo> InputDevices { get; }
        public ObservableCollection<AudioDeviceInfo> OutputDevices { get; }

        private AudioDeviceInfo _selectedInputDevice;
        public AudioDeviceInfo SelectedInputDevice
        {
            get => _selectedInputDevice;
            set { _selectedInputDevice = value; OnPropertyChanged(); }
        }

        private AudioDeviceInfo _selectedOutputDevice;
        public AudioDeviceInfo SelectedOutputDevice
        {
            get => _selectedOutputDevice;
            set { _selectedOutputDevice = value; OnPropertyChanged(); }
        }

        // Loopback Test
        private string _loopbackButtonText = "Start Test";
        public string LoopbackButtonText
        {
            get => _loopbackButtonText;
            set { _loopbackButtonText = value; OnPropertyChanged(); }
        }

        private string _loopbackStatus = "";
        public string LoopbackStatus
        {
            get => _loopbackStatus;
            set { _loopbackStatus = value; OnPropertyChanged(); }
        }

        // Push-to-Talk
        private bool _isPushToTalk;
        public bool IsPushToTalk
        {
            get => _isPushToTalk;
            set
            {
                _isPushToTalk = value;
                OnPropertyChanged();
                Preferences.Set("IsPushToTalk", value);
            }
        }

        // Network Info
        public string LocalPeerId => _discoveryService?.MyDeviceId ?? "Not Available";

        // Commands
        public ICommand ToggleLoopbackCommand { get; }

        public SettingsPageModel(DiscoveryService discoveryService, VoiceService voiceService)
        {
            _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
            _voiceService = voiceService ?? throw new ArgumentNullException(nameof(voiceService));

            InputDevices = new ObservableCollection<AudioDeviceInfo>();
            OutputDevices = new ObservableCollection<AudioDeviceInfo>();

            // Load settings
            LoadSettings();
            LoadAudioDevices();

            // Initialize commands
            ToggleLoopbackCommand = new Command(async () => await ToggleLoopbackAsync());
        }

        private void LoadSettings()
        {
            IsDarkMode = Preferences.Get("IsDarkMode", false);
            IsPushToTalk = Preferences.Get("IsPushToTalk", false);
        }

        private void LoadAudioDevices()
        {
            // Add default devices
            InputDevices.Clear();
            OutputDevices.Clear();

            InputDevices.Add(new AudioDeviceInfo { Name = "Default Microphone", Id = "default_input" });
            OutputDevices.Add(new AudioDeviceInfo { Name = "Default Speakers", Id = "default_output" });

#if WINDOWS
            // On Windows, you could enumerate actual devices using NAudio
            // For now, just add defaults
            InputDevices.Add(new AudioDeviceInfo { Name = "Microphone (USB)", Id = "usb_mic" });
            OutputDevices.Add(new AudioDeviceInfo { Name = "Speakers (Realtek)", Id = "realtek_speakers" });
#endif

            SelectedInputDevice = InputDevices[0];
            SelectedOutputDevice = OutputDevices[0];
        }

        private async Task ToggleLoopbackAsync()
        {
            try
            {
                if (_isLoopbackRunning)
                {
                    // Stop loopback
                    if (_loopbackRecorder?.IsRecording == true)
                    {
                        var audioSource = await _loopbackRecorder.StopAsync();
                    }

                    _loopbackPlayer?.Stop();
                    _loopbackPlayer?.Dispose();
                    _loopbackPlayer = null;

                    _isLoopbackRunning = false;
                    LoopbackButtonText = "Start Test";
                    LoopbackStatus = "";
                }
                else
                {
                    // Start loopback
                    _loopbackRecorder = AudioManager.Current.CreateRecorder();
                    await _loopbackRecorder.StartAsync();

                    _isLoopbackRunning = true;
                    LoopbackButtonText = "Stop Test";
                    LoopbackStatus = "Recording... Speak into your microphone";

                    // Note: Real loopback requires platform-specific implementation
                    // This is just a placeholder UI update
                    await Task.Delay(3000);
                    if (_isLoopbackRunning)
                    {
                        LoopbackStatus = "Test running - you should hear yourself";
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Loopback Error", ex.Message, "OK");
                _isLoopbackRunning = false;
                LoopbackButtonText = "Start Test";
                LoopbackStatus = "";
            }
        }

        private void ApplyTheme()
        {
            try
            {
                Preferences.Set("IsDarkMode", IsDarkMode);

                // Apply theme using UserAppTheme
                if (Application.Current != null)
                {
                    Application.Current.UserAppTheme = IsDarkMode ? AppTheme.Dark : AppTheme.Light;
                    System.Diagnostics.Debug.WriteLine($"SettingsPageModel: Theme changed to {(IsDarkMode ? "Dark" : "Light")}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme apply error: {ex.Message}");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Cleanup()
        {
            if (_disposed) return;

            try
            {
                // Stop and cleanup loopback test if running
                if (_isLoopbackRunning)
                {
                    if (_loopbackRecorder?.IsRecording == true)
                    {
                        Task.Run(async () => await _loopbackRecorder.StopAsync()).Wait();
                    }
                    _loopbackPlayer?.Stop();
                }

                _loopbackRecorder = null;
                _loopbackPlayer?.Dispose();
                _loopbackPlayer = null;
                _isLoopbackRunning = false;

                System.Diagnostics.Debug.WriteLine("SettingsPageModel: Cleanup completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsPageModel cleanup error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Cleanup();
                _disposed = true;
            }
        }
    }
}