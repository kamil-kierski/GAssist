using System;
using System.IO;
using System.Linq;
using Tizen.Multimedia;
using Tizen.System;

namespace GAssist
{
    public class AudioPlayer
    {
        private const string FileName = "temp.mp3";
        private readonly string _bufferFilePath = Path.Combine(BufferFileDir, FileName);

        private readonly Player _player = new Player();
        public long Buffered;
        public volatile bool IsPlaying;

        private static string BufferFileDir { get; } =
            StorageManager.Storages.First().GetAbsolutePath(DirectoryType.Others);

        private FileStream BufferFileStream { get; set; }
        public event EventHandler PlaybackStopped;

        public void Prepare()
        {
            Buffered = 0;
            if (File.Exists(_bufferFilePath)) File.Delete(_bufferFilePath);
            BufferFileStream = File.Open(_bufferFilePath, FileMode.Create, FileAccess.ReadWrite);

            if (_player.State != PlayerState.Idle) _player.Unprepare();
            _player.SetSource(new MediaUriSource(_bufferFilePath));
            _player.PlaybackCompleted += Player_PlaybackCompleted;
        }

        public async void WriteBuffer(byte[] dataBytes)
        {
            if (BufferFileStream.CanWrite && dataBytes.Length != 0)
            {
                BufferFileStream.Write(dataBytes, 0, dataBytes.Length);
                Buffered = BufferFileStream.Length;
                await BufferFileStream.FlushAsync();
            }
        }

        public void Play()
        {
            _player.PrepareAsync().Wait();
            _player.Start();
            IsPlaying = true;

            MainPage.SetButtonImage("stop_red.png");
            MainPage.SetActionButtonIsEnabled(true);
        }

        public void Stop()
        {
            if (_player.State != PlayerState.Playing) return;
            _player.Pause();

            IsPlaying = false;

            //if (File.Exists(BufferFilePath)) File.Delete(BufferFilePath);
            _player.PlaybackCompleted -= Player_PlaybackCompleted;
            OnPlaybackStopped(EventArgs.Empty);

            BufferFileStream.Dispose();


            MainPage.SetButtonImage("listen_blue.png");
            MainPage.SetActionButtonIsEnabled(true);
        }

        private void Player_PlaybackCompleted(object sender, EventArgs e)
        {
            Stop();
        }

        protected virtual void OnPlaybackStopped(EventArgs args)
        {
            PlaybackStopped?.Invoke(this, args);
        }
    }
}