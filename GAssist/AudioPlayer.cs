using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tizen.Multimedia;
using Tizen.System;

namespace GAssist
{
    public class AudioPlayer
    {
        private const string FileName = "temp.mp3";
        private readonly string _bufferFilePath = Path.Combine(BufferFileDir, FileName);

        private readonly Player _player = new Player();

        public static bool IsPlaying { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
        public static bool IsPrepared { get; [MethodImpl(MethodImplOptions.Synchronized)]private set; }

        private static string BufferFileDir { get; } =
            StorageManager.Storages.First().GetAbsolutePath(DirectoryType.Others);
        private FileStream BufferFileStream { get; set; }
        public long Buffered { get; private set; }

        public event EventHandler PlaybackStopped;

        public void Prepare()
        {
            Buffered = 0;
            //if (File.Exists(_bufferFilePath)) File.Delete(_bufferFilePath);
            //BufferFileStream = File.Open(_bufferFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            BufferFileStream = new FileStream(_bufferFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous);
            if (_player.State != PlayerState.Idle) _player.Unprepare();
            _player.SetSource(new MediaUriSource(_bufferFilePath));
            _player.PlaybackCompleted += Player_PlaybackCompleted;
            IsPrepared = true;
        }

        public async Task WriteBuffer(byte[] dataBytes)
        {
            if (IsPrepared && BufferFileStream.CanWrite && dataBytes.Length != 0)
            {
                await BufferFileStream.WriteAsync(dataBytes, 0, dataBytes.Length);
                Buffered += dataBytes.LongLength;
                await BufferFileStream.FlushAsync();
            }
        }

        public void Play()
        {
            IsPlaying = true;
            _player.PrepareAsync().Wait();
            _player.Start();
            

            MainPage.SetButtonImage("stop_red.png");
            MainPage.SetActionButtonIsEnabled(true);
        }

        public void Stop()
        {
            IsPlaying = false;
            IsPrepared = false;
            if (_player.State != PlayerState.Playing) return;
            _player.Pause();

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