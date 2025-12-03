using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Proximity.Services;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace Proximity.PageModels
{
    public class SettingsPageModel : INotifyPropertyChanged 
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly VoiceService _voiceService;
        public ObservableCollection<string> AvailableDevices { get; } = new();  

        private string _remoteIp = "255.255.255.255";
        public string RemoteIp
        {
            get => _remoteIp;
            set { _remoteIp = value; OnPropertyChanged(); }
        }
        private int _localPort = 9003;
        public int LocalPort
        {
            get => _localPort;
            set { _localPort = value; OnPropertyChanged(); }
        }

        private int _remotePort = 9003;
        public int RemotePort
        {
            get => _remotePort;
            set { _remotePort = value; OnPropertyChanged(); }
        }

        private bool _isVoiceRunning = false;
        public bool IsVoiceRunning
        {
            get => _isVoiceRunning;
            set { _isVoiceRunning = value; OnPropertyChanged(); }
        }
        public ICommand StartVoiceCommand { get; }
        public ICommand StopVoiceCommand { get; }
        public ICommand RefreshDevicesCommand { get; }
        public SettingsPageModel()
        {
            _voiceService = new VoiceService(localPort: LocalPort, remotePort: RemotePort);


            StartVoiceCommand = new Command(async () => await StartVoiceAsync());
            StopVoiceCommand = new Command(() => StopVoice());
            RefreshDevicesCommand = new Command(() => RefreshDevices());


            // Populate simple device list placeholder (Platform-specific implementation recommended)
            AvailableDevices.Add("Default Device");
        }
        private async Task StartVoiceAsync()
        {
            try
            {
                _voiceService.SetRemoteEndpoint(RemoteIp);
                _voiceService.Start();
                IsVoiceRunning = true;
            }
            catch (Exception ex)
            {
                // Show alert on UI main thread
                await Application.Current.MainPage.DisplayAlert("Voice Error", ex.Message, "OK");
            }
        }
        private void StopVoice()
        {
            try
            {_voiceService.Stop();
                IsVoiceRunning = false;
            }
            catch (Exception ex)
            {
                Application.Current.MainPage.Dispatcher.Dispatch(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Stop Error", ex.Message, "OK");
                });
            }
        }
        private void RefreshDevices()
        {
            // In this simplified example we don't enumerate native audio devices.
            // For production, implement platform-specific audio device enumeration and populate AvailableDevices.
            AvailableDevices.Clear();
            AvailableDevices.Add("Default Device");
            AvailableDevices.Add("Speaker (Simulated)");
        }
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
    
    