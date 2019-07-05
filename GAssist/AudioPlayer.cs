using Google.Assistant.Embedded.V1Alpha2;
using Google.Protobuf;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Tizen.Multimedia;
using Tizen.System;

namespace GAssist
{
    public class AudioPlayer
    {
        private const string FileName = "temp.mp3";

        private static CancellationTokenSource _source;
        private static CancellationToken _token;
        private readonly string _bufferFilePath = Path.Combine(BufferFileDir, FileName);

        private readonly Player _player = new Player();

        public event EventHandler PlaybackStopped;

        private void Player_PlaybackCompleted(object sender, EventArgs e)
        {
            Stop();
        }

        private static string BufferFileDir { get; } =
            StorageManager.Storages.First().GetAbsolutePath(DirectoryType.Others);
        private FileStream BufferFileStream { get; set; }

        protected virtual void OnPlaybackStopped(EventArgs args)
        {
            PlaybackStopped?.Invoke(this, args);
        }

        public void Play()
        {
            IsPlaying = true;
            _player.PrepareAsync().Wait();
            _player.Start();


            MainPage.SetButtonImage("stop_red.png");
            MainPage.SetActionButtonIsEnabled(true);
        }

        public void Prepare()
        {
            Buffered = 0;
            //if (File.Exists(_bufferFilePath)) File.Delete(_bufferFilePath);
            //BufferFileStream = File.Open(_bufferFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            BufferFileStream = new FileStream(_bufferFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous);
            _source = new CancellationTokenSource();
            _token = _source.Token;


            if (_player.State != PlayerState.Idle) _player.Unprepare();
            _player.SetSource(new MediaUriSource(_bufferFilePath));
            _player.PlaybackCompleted += Player_PlaybackCompleted;
            IsPrepared = true;
        }

        public void Stop()
        {
            var ar = new AssistRequest
            {
                Config = new AssistConfig
                {
                    AudioInConfig = new AudioInConfig
                    {
                        SampleRateHertz = 16000,
                        Encoding = AudioInConfig.Types.Encoding.Flac
                    }
                }
            };

            SapService.SendData(ar.ToByteArray());

            IsPlaying = false;
            IsPrepared = false;
            if (_player.State != PlayerState.Playing) return;
            _player.Stop();

            _source.Cancel();
            BufferFileStream.Close();

            //if (File.Exists(BufferFilePath)) File.Delete(BufferFilePath);
            _player.PlaybackCompleted -= Player_PlaybackCompleted;
            OnPlaybackStopped(EventArgs.Empty);

            MainPage.SetButtonImage("listen_blue.png");
            MainPage.SetActionButtonIsEnabled(true);
        }

        public async Task WriteBuffer(byte[] dataBytes)
        {
            if (IsPrepared && BufferFileStream.CanWrite && dataBytes.Length != 0)
            {
                await BufferFileStream.WriteAsync(dataBytes, 0, dataBytes.Length, _token);
                Buffered += dataBytes.LongLength;
                await BufferFileStream.FlushAsync(_token);
            }
        }

        public long Buffered { get; private set; }

        public static bool IsPlaying { get; [MethodImpl(MethodImplOptions.Synchronized)] set; }
        public static bool IsPrepared { get; [MethodImpl(MethodImplOptions.Synchronized)]private set; }
    }
}