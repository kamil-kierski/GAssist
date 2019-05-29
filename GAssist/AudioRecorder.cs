using System.Threading;
using System.Threading.Tasks;
using Tizen;
using Tizen.Multimedia;

namespace GAssist
{
    internal static class AudioRecorder
    {
        private static readonly int _bufferSize = 1600;

        private static readonly AudioCapture AudioCapture =
            new AudioCapture(16000, AudioChannel.Mono, AudioSampleType.S16Le);

        public static volatile bool IsRecording;
        private static CancellationTokenSource _source;
        private static CancellationToken _token;


        public static void StartRecording()
        {
            if (IsRecording) Log.Debug("AUDIORECORDER", "BAD!:RECORDING FLAG TRUE ON START RECORDING");
            IsRecording = true;
            _source = new CancellationTokenSource();
            _token = _source.Token;
            AudioCapture.Prepare();
            Record();
        }

        public static void StopRecording()
        {
            _source.Cancel();
            AudioCapture.Flush();
            AudioCapture.Unprepare();
            IsRecording = false;
        }

        private static void Record()
        {
            Task.Factory.StartNew(() =>
            {
                while (IsRecording)
                {
                    if (_token.IsCancellationRequested) return;
                    SapService.SendData(AudioCapture.Read(_bufferSize));
                }
            }, _token);
        }
    }
}