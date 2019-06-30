using Google.Assistant.Embedded.V1Alpha2;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tizen.Multimedia;

namespace GAssist
{
    public class ResponseHandler
    {
        public static readonly AudioPlayer Player = new AudioPlayer();


        public async Task HandleResponse(byte[] dataBytes)
        {
            var ar = AssistResponse.Parser.ParseFrom(dataBytes);

            //if (ar.DialogStateOut.MicrophoneMode == DialogStateOut.Types.MicrophoneMode.CloseMicrophone)
            //{
            //    return;
            //}


            if (ar.EventType == AssistResponse.Types.EventType.EndOfUtterance)
            {

                AudioRecorder.StopRecording();
                MainPage.ProgressPopup.Dismiss();
                MainPage.ProgressPopup.UpdateText(string.Empty);
                Player.Prepare();
                if (!string.IsNullOrEmpty(ar.DialogStateOut?.SupplementalDisplayText))
                {
                    MainPage.SetLabelText(ar.DialogStateOut.SupplementalDisplayText);
                    MainPage.SetActionButtonIsEnabled(true);
                }
            }

            if (ar.SpeechResults != null && !ar.SpeechResults.Any(i => (int)i.Stability == 1))
            {
                if (MainPage.Pref.GetRawVoiceRecognitionText())
                {
                    foreach (string transcript in ar.SpeechResults.Select(x => x.Transcript))
                    {
                        MainPage.ProgressPopup.UpdateText(transcript);
                    }

                    //if (ar.SpeechResults.Any(i => (int)i.Stability == 1)) return;
                    //MainPage.ProgressPopup.UpdateText(ar.SpeechResults.First().Transcript);
                    //return;
                }

                if (!MainPage.Pref.GetRawVoiceRecognitionText() && ar.SpeechResults.Any(i => i.Stability > 0.01))
                {
                    foreach (string transcript in ar.SpeechResults.Where(x => x.Stability > 0.01).Select(x => x.Transcript))
                    {
                        MainPage.ProgressPopup.UpdateText(transcript);
                    }
                    //if (ar.SpeechResults.Any(i => (int)i.Stability == 1)) return;
                    //MainPage.ProgressPopup.UpdateText(ar.SpeechResults.First().Transcript);
                    //return;
                }
            }


            if (!string.IsNullOrEmpty(ar.DialogStateOut?.SupplementalDisplayText))
            {
                MainPage.SetLabelText(ar.DialogStateOut.SupplementalDisplayText);
                //return;
            }

            if (ar.ScreenOut != null)
            {
                //var parsedResponse = HtmlResponseParser.ParseHtmlResponse(ar.ScreenOut.Data.ToStringUtf8());
                MainPage.SetHtmlView(ar.ScreenOut.Data.ToStringUtf8());
            }


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
                await Player.WriteBuffer(ar.AudioOut.AudioData.ToByteArray());

                if (AudioPlayer.IsPrepared && !AudioPlayer.IsPlaying && Player.Buffered >= 1600)
                {
                    AudioPlayer.IsPlaying = true;
                    await Task.Run(Player.Play);
                }
                //Rl.Throttle(TimeSpan.FromMilliseconds(1500), () =>
                //{

                //}, false, true);
            }
        }
    }
}