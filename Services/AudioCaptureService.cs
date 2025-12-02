using Plugin.Maui.Audio;

namespace Proximity.Services
{
    public class AudioCaptureService
    {
        private IAudioRecorder? _recorder;

        public async Task StartAsync()
        {
            if (_recorder == null)
                _recorder = AudioManager.Current.CreateRecorder();

            await _recorder.StartAsync();
        }

        public async Task<byte[]> StopAsync()
        {
            if (_recorder == null)
                return Array.Empty<byte>();

            var audioFile = await _recorder.StopAsync();
            return File.ReadAllBytes(audioFile);
        }

        public bool IsRecording =>
            _recorder?.IsRecording ?? false;
    }
}
