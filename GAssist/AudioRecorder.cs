using System.Threading;
using System.Threading.Tasks;
using Tizen;
using Tizen.Multimedia;

namespace GAssist
{
    internal static class AudioRecorder
    {
        private static readonly int _bufferSize = 1600;

        private static AudioCapture _audioCapture;
        public static volatile bool IsRecording;
        private static CancellationTokenSource _source;
        private static CancellationToken _token;


        public static void StartRecording()
        {
            if (IsRecording) Log.Debug("AUDIORECORDER", "BAD!:RECORDING FLAG TRUE ON START RECORDING");
            _audioCapture = new AudioCapture(16000, AudioChannel.Mono, AudioSampleType.S16Le);
            IsRecording = true;
            _source = new CancellationTokenSource();
            _token = _source.Token;
            _audioCapture.Prepare();
            Record();
           //MainPage.createProgressPopup();
        }

        public static void StopRecording()
        {
            _source.Cancel();
            _audioCapture.Flush();
            _audioCapture.Unprepare();
            _audioCapture.Dispose();
            IsRecording = false;
        }

        private static void Record()
        {
            Task.Factory.StartNew(() =>
            {
                while (IsRecording)
                {
                    if (_token.IsCancellationRequested) return;
                    SapService.SendData(_audioCapture.Read(_bufferSize));
                }
            }, _token);
        }
    }
}