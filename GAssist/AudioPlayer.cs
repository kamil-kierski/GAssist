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
        private Task player;
        CancellationTokenSource source;
        CancellationToken token;
        private System.Timers.Timer timer;
        private Action OnStopCallback;

        public AudioPlayer(Action OnStopCallback)
        {
            this.OnStopCallback = OnStopCallback;

            audioPlayback = new AudioPlayback(16000, AudioChannel.Mono, AudioSampleType.S16Le);
            AudioManager.VolumeController.Level[AudioVolumeType.Media] = 15;
            buffer = new CircularBuffer<byte[]>(1000);


            timer = new System.Timers.Timer(200);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = false;

        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            Tizen.Log.Debug("AUDIOPLAYER", "STOP: ");
            Stop();
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
                        timer.Interval += (double)audioPlayback.Write(buffer.Get()) / 16000.00 / 2.00;
                        if(!timer.Enabled) timer.Start();
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
            OnStopCallback();
        }

        public void ClearBuffer()
        {
            buffer.Clear();
        }


    }
}
