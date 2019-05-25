using Samsung.Sap;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tizen.Multimedia;

namespace GAssist
{
    internal class AudioRecorder
    {
        private Agent agent;
        private Connection connection;
        private byte[] chunk;
        public volatile bool isRecording;
        private int bufferSize;
        private CancellationTokenSource source;
        private CancellationToken token;

        private AudioCapture audioCapture;

        public AudioRecorder(Connection connection, Agent agent, int bufferSize)
        {
            this.connection = connection;
            this.agent = agent;
            this.bufferSize = bufferSize;
        }

        public void StartRecording()
        {
            if (isRecording)
            {
                Tizen.Log.Debug("AUDIORECORDER", "BAD!:RECORDING FLAG TRUE ON START RECORDING");
            }
            audioCapture = new AudioCapture(16000, AudioChannel.Mono, AudioSampleType.S16Le);
            isRecording = true;
            source = new CancellationTokenSource();
            token = source.Token;
            audioCapture.Prepare();
            Record();
        }

        public void StopRecording()
        {
            source.Cancel();
            chunk = null;
            audioCapture.Flush();
            audioCapture.Unprepare();
            audioCapture.Dispose();
            isRecording = false;
        }

        public void Record()
        {
            Task.Factory.StartNew(() =>
            {
                while (isRecording)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    //chunk = new byte[bufferSize];
                    chunk = audioCapture.Read(bufferSize);

                    if (connection != null && agent != null && agent.Channels.Count > 0)
                    {
                        connection.Send(agent.Channels.First().Value, chunk);
                    }
                }
            }, token);
        }
    }
}