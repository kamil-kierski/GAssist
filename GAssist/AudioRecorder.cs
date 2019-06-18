using System.Threading;
using System.Threading.Tasks;
using Google.Assistant.Embedded.V1Alpha2;
using Google.Protobuf;
using Tizen.Multimedia;

namespace GAssist
{
    public static class AudioRecorder
    {
        private static readonly int _bufferSize = 1600;

        private static readonly AudioCapture _audioCapture =
            new AudioCapture(16000, AudioChannel.Mono, AudioSampleType.S16Le);

        public static volatile bool IsRecording;
        private static CancellationTokenSource _source;
        private static CancellationToken _token;


        public static void StartRecording(bool htmlResponse)
        {
            //_audioCapture = new AudioCapture(16000, AudioChannel.Mono, AudioSampleType.S16Le);
            IsRecording = true;

            var ar = new AssistRequest
            {
                Config = new AssistConfig
                {
                    AudioInConfig = new AudioInConfig
                    {
                        SampleRateHertz = 16000,
                        Encoding = AudioInConfig.Types.Encoding.Linear16
                    },
                    ScreenOutConfig = new ScreenOutConfig
                    {
                        ScreenMode = htmlResponse
                            ? ScreenOutConfig.Types.ScreenMode.Playing
                            : ScreenOutConfig.Types.ScreenMode.Unspecified
                    }
                }
            };

            SapService.SendData(ar.ToByteArray());

            _audioCapture.Prepare();
            Record();
        }

        public static void StopRecording()
        {
            _source.Cancel();
            IsRecording = false;
        }

        private static void Record()
        {
            _source = new CancellationTokenSource();
            _token = _source.Token;


            Task.Factory.StartNew(() =>
            {
                while (IsRecording)
                {
                    if (_token.IsCancellationRequested) break;

                    var ar2 = new AssistRequest
                    {
                        AudioIn = ByteString.CopyFrom(_audioCapture.Read(_bufferSize))
                    };
                    SapService.SendData(ar2.ToByteArray());
                }

                _audioCapture.Flush();
                _audioCapture.Unprepare();
            }, _token);
        }
    }
}