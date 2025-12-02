using Plugin.Maui.Audio;
using System.Collections.ObjectModel;

namespace Proximity.Services
{
    public class AudioDeviceService
    {
        public ObservableCollection<string> InputDevices { get; private set; }
        public ObservableCollection<string> OutputDevices { get; private set; }

        public AudioDeviceService()
        {
            InputDevices = new ObservableCollection<string>();
            OutputDevices = new ObservableCollection<string>();

            LoadDevices();
        }

        private void LoadDevices()
        {
            InputDevices.Clear();
            OutputDevices.Clear();

#if WINDOWS
            // WINDOWS ONLY – uses NAudio CoreAudioAPI
            try
            {
                var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();

                // INPUT DEVICES
                foreach (var device in enumerator.EnumerateAudioEndPoints(
                    NAudio.CoreAudioApi.DataFlow.Capture,
                    NAudio.CoreAudioApi.DeviceState.Active))
                {
                    InputDevices.Add(device.FriendlyName);
                }

                // OUTPUT DEVICES
                foreach (var device in enumerator.EnumerateAudioEndPoints(
                    NAudio.CoreAudioApi.DataFlow.Render,
                    NAudio.CoreAudioApi.DeviceState.Active))
                {
                    OutputDevices.Add(device.FriendlyName);
                }
            }
            catch
            {
                InputDevices.Add("Default Microphone");
                OutputDevices.Add("Default Speakers");
            }
#else
            // NON-WINDOWS (Android, iOS, MacCatalyst) use defaults
            InputDevices.Add("Default Microphone");
            OutputDevices.Add("Default Speakers");
#endif
        }

        public string GetDefaultInputDevice() =>
            InputDevices.FirstOrDefault() ?? "Default Microphone";

        public string GetDefaultOutputDevice() =>
            OutputDevices.FirstOrDefault() ?? "Default Speakers";
    }
}
