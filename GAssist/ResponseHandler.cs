using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Assistant.Embedded.V1Alpha2;
using Scar.Common;
using Tizen.Multimedia;

namespace GAssist
{
    public static class ResponseHandler
    {
        private static readonly RateLimiter rl = new RateLimiter();
        public static readonly AudioPlayer player = new AudioPlayer();


        public static void HandleResponse(byte[] dataBytes)
        {
            var ar = AssistResponse.Parser.ParseFrom(dataBytes);

            //if (ar.DialogStateOut.MicrophoneMode == DialogStateOut.Types.MicrophoneMode.CloseMicrophone)
            //{
            //    return;
            //}
            if (ar.ScreenOut != null)
            {
                var parsedResponse = HtmlResponseParser.ParseHtmlResponse2(ar.ScreenOut.Data.ToStringUtf8());
                MainPage.SetHtmlView(parsedResponse);
            }

            if (ar.EventType == AssistResponse.Types.EventType.EndOfUtterance)
            {
                AudioRecorder.StopRecording();
                MainPage.DismissProgressPopup();
                player.Prepare();
                return;
            }

            if (MainPage.pref.GetRawVoiceRecognitionText() && (ar.SpeechResults?.Any() ?? false))
            {
                if (ar.SpeechResults.Any(i => i.Stability == 1)) return;
                MainPage.UpdateProgressPopupText(ar.SpeechResults.First().Transcript);
                return;
            }

            if (!MainPage.pref.GetRawVoiceRecognitionText() &&
                (ar.SpeechResults?.Any(i => i.Stability > 0.01) ?? false))
            {
                if (ar.SpeechResults.Any(i => i.Stability == 1)) return;
                MainPage.UpdateProgressPopupText(ar.SpeechResults.First().Transcript);
                return;
            }

            if (!string.IsNullOrEmpty(ar.DialogStateOut?.SupplementalDisplayText))
                MainPage.SetLabelText(ar.DialogStateOut.SupplementalDisplayText);
            //return;


            if ((ar.DialogStateOut?.VolumePercentage ?? 0) != 0)
            {
                var newVolumeLevel = Convert.ToInt32(15 * ar.DialogStateOut.VolumePercentage / 100);
                AudioManager.VolumeController.Level[AudioVolumeType.Media] = newVolumeLevel;
                MainPage.SetButtonImage("listen_blue.png");
                MainPage.SetActionButtonIsEnabled(true);
                return;
            }


            if (ar.AudioOut?.AudioData.Length > 0)
            {
                player.WriteBuffer(ar.AudioOut.AudioData.ToByteArray());

                if (!player.IsPlaying && player.Buffered >= 1600)
                    rl.Throttle(TimeSpan.FromMilliseconds(2000), () =>
                    {
                        player.IsPlaying = true;
                        Task.Run(player.Play);
                    }, false, true);
            }
        }
    }
}