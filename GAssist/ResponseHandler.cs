using System;
using System.Linq;
using Google.Assistant.Embedded.V1Alpha2;
using Tizen.Multimedia;

namespace GAssist
{
    internal static class ResponseHandler
    {
        private static bool _first = true;

        public static void HandleResponse(byte[] dataBytes)
        {
            var ar = AssistResponse.Parser.ParseFrom(dataBytes);


            if (ar.SpeechResults?.Any(i => i.Stability > 0.01) ?? false)
            {
                if (_first)
                {
                    MainPage.CreateProgressPopup();
                    _first = false;
                }

                MainPage.UpdateProgressPopupText(ar.SpeechResults.First().Transcript);

                if (ar.SpeechResults.Any(i => i.Stability == 1))
                {
                    AudioRecorder.StopRecording();
                    MainPage.DismissProgressPopup();
                    AudioPlayer.Prepare();
                    _first = true;
                }
            }

            if (!string.IsNullOrEmpty(ar.DialogStateOut?.SupplementalDisplayText))
                MainPage.SetLabelText(ar.DialogStateOut.SupplementalDisplayText);


            if ((ar.DialogStateOut?.VolumePercentage ?? 0) != 0)
            {
                var newVolumeLevel = Convert.ToInt32(15 * ar.DialogStateOut.VolumePercentage / 100);
                AudioManager.VolumeController.Level[AudioVolumeType.Media] = newVolumeLevel;
                MainPage.SetActionButtonIsEnabled(true);
            }

            if (ar.ScreenOut != null) MainPage.SetLabelText(ar.ScreenOut.Data.ToStringUtf8());

            if (ar.AudioOut?.AudioData.Length > 0)
            {
                AudioPlayer.WriteBuffer(ar.AudioOut.AudioData.ToByteArray());

                if (!AudioPlayer.IsPlaying && AudioPlayer.BufferFileStream.Length >= 1600)
                {
                    AudioPlayer.IsPlaying = true;
                    AudioPlayer.Play();

                    MainPage.SetActionButtonIsEnabled(true);
                    MainPage.SetActionButtonText("Stop");
                }
            }
        }
    }
}