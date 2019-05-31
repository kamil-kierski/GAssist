using System.Threading;
using System.Threading.Tasks;
using Google.Assistant.Embedded.V1Alpha2;
using Google.Protobuf;
using Tizen.Multimedia;

namespace GAssist
{
    internal static class AudioRecorder
    {
        private static readonly int _bufferSize = 1600;

        private static AudioCapture _audioCapture =
            new AudioCapture(16000, AudioChannel.Mono, AudioSampleType.S16Le);

        public static volatile bool IsRecording;
        private static CancellationTokenSource _source;
        private static CancellationToken _token;


        public static void StartRecording()
        {
            _audioCapture = new AudioCapture(16000, AudioChannel.Mono, AudioSampleType.S16Le);
            _source = new CancellationTokenSource();
            _token = _source.Token;
            _audioCapture.Prepare();
            IsRecording = true;
            Record();

            AudioInConfig aic = new AudioInConfig();
            aic.SampleRateHertz = 16000;
            aic.Encoding = AudioInConfig.Types.Encoding.Linear16;

            SapService.SendData(aic.ToByteArray());
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
                    //SapService.SendData(_audioCapture.Read(_bufferSize));
                    byte[] chunk = _audioCapture.Read(_bufferSize);

                    AssistRequest ar = new AssistRequest
                    {
                        AudioIn = ByteString.CopyFrom(chunk)
                    };
                    SapService.SendData(ar.ToByteArray());
                    Tizen.Log.Debug("RECORDER", "SENT " + ar.AudioIn);
                }
            }, _token);
        }
    }
}