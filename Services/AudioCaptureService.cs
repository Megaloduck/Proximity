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

            var audioSource = await _recorder.StopAsync();

            // Get the file path from the audio source
            var stream = audioSource.GetAudioStream();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        public bool IsRecording =>
            _recorder?.IsRecording ?? false;
    }
}
