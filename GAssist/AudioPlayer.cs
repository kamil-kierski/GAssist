using System;
using System.Threading;
using System.Threading.Tasks;
using Tizen.Multimedia;

namespace GAssist
{
    class AudioPlayer
    {
        private AudioPlayback audioPlayback;
        public CircularBuffer<byte[]> buffer;
        public volatile bool isStopped;
        private Task player;
        CancellationTokenSource source;
        CancellationToken token;

        public AudioPlayer()
        {
            audioPlayback = new AudioPlayback(16000, AudioChannel.Mono, AudioSampleType.S16Le);
            AudioManager.VolumeController.Level[AudioVolumeType.Media] = 15;
            buffer = new CircularBuffer<byte[]>(1000);
            isStopped = true;
        }
        //Action<String> OnStopCallback
        public void StartPlaying()
        {
            audioPlayback.Prepare();
            source = new CancellationTokenSource();
            token = source.Token;

            player = new Task(() =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }
                    if (!buffer.IsEmpty)
                    {
                        audioPlayback.Write(buffer.Get());
                    }
                    else
                    {
                        Stop();
                    }
                }
            }, token);

            if (player.Status != TaskStatus.Running)
            {
                player.Start();
            }
        }

        public void Stop()
        {
            source.Cancel();
            audioPlayback.Unprepare();
            ClearBuffer();
            isStopped = true;
            //audioPlayback = null;
        }

        public void ClearBuffer()
        {
            buffer.Clear();
        }


    }
}
