using Plugin.Maui.Audio;
using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Services
{
    public class AudioPlaybackService
    {
        public async Task PlayAudioAsync(byte[] data)
        {
            using var stream = new MemoryStream(data);
            var player = AudioManager.Current.CreatePlayer(stream);
            await player.PlayAsync();
        }
    }
}
