using Plugin.Maui.Audio;

namespace Proximity.Services
{
    public class AudioPlaybackService
    {
        public async Task PlayAudioAsync(byte[] data)
        {
            using var stream = new MemoryStream(data);
            var player = AudioManager.Current.CreatePlayer(stream);
            player.Play();

            // Wait for playback to complete
            while (player.IsPlaying)
            {
                await Task.Delay(100);
            }
        }
    }
}