using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Google.Assistant.Embedded.V1Alpha2;
using Google.Protobuf;
using Tizen.Multimedia;
using Tizen.System;

namespace GAssist
{
    internal class AudioPlayer
    {
        public readonly Player Player = new Player();
        public volatile bool IsPlaying = false;
        public event EventHandler PlaybackStopped;

        public FileStream BufferFileStream { get; set; }

        private static string BufferFilePath { get; } =
            StorageManager.Storages.First().GetAbsolutePath(DirectoryType.Others)
            + @"temp.mp3";

        public void Prepare()
        {
            if (File.Exists(BufferFilePath)) File.Delete(BufferFilePath);
            BufferFileStream = File.Create(BufferFilePath);

            Player.SetSource(new MediaUriSource(BufferFilePath));
            Player.PlaybackCompleted += Player_PlaybackCompleted;
        }

        public async void WriteBuffer(byte[] dataBytes)
        {
            if (BufferFileStream != null && dataBytes.Length != 0)
            {
                await BufferFileStream.WriteAsync(dataBytes, 0, dataBytes.Length);
                if (BufferFileStream.Length != 0) await BufferFileStream.FlushAsync();
            }
        }

        public void Play()
        {
            Player.PrepareAsync().Wait();
            Player.Start(); 
        }

        public void Stop()
        {
            if (Player.State != PlayerState.Playing) return;
            Player.Stop();
            Player.Unprepare();

            AudioInConfig aic = new AudioInConfig();
            aic.SampleRateHertz = 16000;
            aic.Encoding = AudioInConfig.Types.Encoding.Linear16;

            SapService.SendData(aic.ToByteArray());

            //BufferFileStream.FlushAsync();
            BufferFileStream.Dispose();
            BufferFileStream = null;

            //if (File.Exists(BufferFilePath)) File.Delete(BufferFilePath);
            Player.PlaybackCompleted -= Player_PlaybackCompleted;
            OnPlaybackStopped(EventArgs.Empty);
        }

        private void Player_PlaybackCompleted(object sender, EventArgs e)
        {
            Stop();
        }

        protected virtual void OnPlaybackStopped(EventArgs args)
        {
            this.PlaybackStopped?.Invoke(this, args);
        }
    }
}