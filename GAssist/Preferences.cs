using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tizen.Applications;

namespace GAssist
{
    public class Preferences
    {
        public IList<Setting> Settings { get; set; } = new ObservableCollection<Setting>();

        public bool GetRecordOnStart()
        {
            if (Preference.Contains("record_on_start"))
                return Preference.Get<bool>("record_on_start");
            return false;
        }

        public void SetRecordOnResume(bool setting)
        {
            Preference.Set("record_on_resume", setting);
        }

        public bool GetRecordOnResume()
        {
            if (Preference.Contains("record_on_resume"))
                return Preference.Get<bool>("record_on_resume");
            return false;
        }

        public void SetRawVoiceRecognitionText(bool setting)
        {
            Preference.Set("raw_voice_recognition", setting);
        }

        public bool GetRawVoiceRecognitionText()
        {
            if (Preference.Contains("raw_voice_recognition"))
                return Preference.Get<bool>("raw_voice_recognition");
            return false;
        }

        public void SetLargerFont(bool setting)
        {
            Preference.Set("larger_font", setting);
        }

        public bool GetLargerFont()
        {
            if (Preference.Contains("larger_font"))
                return Preference.Get<bool>("larger_font");
            return false;
        }

        public void SetHtmlResponse(bool setting)
        {
            Preference.Set("html_response", setting);
        }

        public bool GetHtmlResponse()
        {
            if (Preference.Contains("html_response"))
                return Preference.Get<bool>("html_response");
            return false;
        }

    }
}