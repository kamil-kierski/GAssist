using Google.Protobuf;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Tizen.Multimedia;

namespace GAssist
{
    public static class AudioRecorder
    {
        private static readonly int _bufferSize = 1600;

        private static readonly AudioCapture _audioCapture =
            new AudioCapture(16000, AudioChannel.Mono, AudioSampleType.S16Le);

        private static CancellationTokenSource _source;
        private static CancellationToken _token;

        public static bool IsRecording { get; [MethodImpl(MethodImplOptions.Synchronized)]private set; }

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
            IsRecording = false;
            _source.Cancel();

        }

        private static void Record()
        {
            _source = new CancellationTokenSource();
            _token = _source.Token;


            Task.Run(() =>
            {
                while (!_token.IsCancellationRequested)
                {
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