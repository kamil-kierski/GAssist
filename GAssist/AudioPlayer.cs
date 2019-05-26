using System;
using System.IO;
using System.Linq;
using Tizen;
using Tizen.Multimedia;
using Tizen.System;

namespace GAssist
{
    internal static class AudioPlayer
    {
        public static Action OnStopCallback;
        private static Player _player;
        public static bool IsPlaying;


        public static FileStream BufferFileStream { get; set; }

        private static string BufferFilePath { get; } =
            StorageManager.Storages.First().GetAbsolutePath(DirectoryType.Others)
            + @"temp.mp3";

        public static void Prepare()
        {
            if (File.Exists(BufferFilePath)) File.Delete(BufferFilePath);
            BufferFileStream = File.Create(BufferFilePath);

            _player = new Player();
            _player.PlaybackCompleted += Player_PlaybackCompleted;
        }

        public static void WriteBuffer(byte[] dataBytes)
        {
            try
            {
                BufferFileStream.Write(dataBytes, 0, dataBytes.Length);
                if (BufferFileStream.Length != 0) BufferFileStream.Flush();
            }

            catch (ObjectDisposedException)
            {
                Log.Debug("AUDIO RESPONSE", "Tried to write to closed FileStream, Knownbug");
            }
        }

        public static void Play()
        {
            _player.SetSource(new MediaUriSource(BufferFilePath));
            _player.PrepareAsync().Wait();
            _player.Start();
        }

        public static void Stop()
        {
            try
            {
                if (_player.State == PlayerState.Playing)
                {
                    _player.Stop();
                    _player.Unprepare();
                    _player.Dispose();

                    BufferFileStream.Close();
                    BufferFileStream.Dispose();

                    OnStopCallback();

                    if (File.Exists(BufferFilePath)) File.Delete(BufferFilePath);
                    BufferFileStream = File.Create(BufferFilePath);
                }
            }
            catch (ObjectDisposedException)
            {
                OnStopCallback();
            }
        }

        private static void Player_PlaybackCompleted(object sender, EventArgs e)
        {
            Stop();
        }
    }
}