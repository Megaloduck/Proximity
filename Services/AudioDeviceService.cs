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

            // NON-WINDOWS (Android, iOS, MacCatalyst) use defaults
            InputDevices.Add("Default Microphone");
            OutputDevices.Add("Default Speakers");
        }


        public string GetDefaultInputDevice() =>
            InputDevices.FirstOrDefault() ?? "Default Microphone";

        public string GetDefaultOutputDevice() =>
            OutputDevices.FirstOrDefault() ?? "Default Speakers";
    }
}
