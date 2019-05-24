using System;
using Tizen.Multimedia;

namespace GAssist
{
    internal class AudioPlayer
    {
        private Player player;
        private Action OnStopCallback;
        public MediaBufferSource mediaBufferSource;

        public AudioPlayer(Action OnStopCallback)
        {
            this.OnStopCallback = OnStopCallback;
            player = new Player();
            player.PlaybackCompleted += Player_PlaybackCompleted;
            AudioManager.VolumeController.Level[AudioVolumeType.Media] = 15;
        }

        public void Play(String fileName)
        {
            player.SetSource(new MediaUriSource(fileName));
            player.PrepareAsync().Wait();
            player.Start();
        }

        public void Stop()
        {
            if (player.State == PlayerState.Playing)
            {
                player.Stop();
                player.Unprepare();
                mediaBufferSource = null;
                OnStopCallback();
            };
        }

        private void Player_PlaybackCompleted(object sender, EventArgs e)
        {
            Stop();
        }
    }
}