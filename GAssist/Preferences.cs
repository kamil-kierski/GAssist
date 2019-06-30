using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tizen.Applications;
using Xamarin.Forms;

namespace GAssist
{
    public class Preferences
    {
        public IList<Setting> Settings { get; set; } = new ObservableCollection<Setting>();
        private readonly App _app;
        private readonly MainPage mainPage;
        public Preferences (App app, MainPage mainPage)
        {
            _app = app;
            this.mainPage = mainPage;
        }

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

        public void LoadSettings()
        {

            var autoListenOnResume = new Setting
            {
                IsToggled = GetRecordOnResume(),
                Text = "Auto Listen On Resume"
            };

            if (autoListenOnResume.IsToggled) _app.ResumeEvent += mainPage.App_ResumeEvent;

            autoListenOnResume.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled")
                {
                    SetRecordOnResume(autoListenOnResume.IsToggled);
                    if (autoListenOnResume.IsToggled) _app.ResumeEvent += mainPage.App_ResumeEvent;
                    else _app.ResumeEvent -= mainPage.App_ResumeEvent;
                }
            };

            var autoListenOnStart = new Setting
            { IsToggled = GetRecordOnStart(), Text = "Auto Listen On Start" };

            autoListenOnStart.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled") Preference.Set("record_on_start", autoListenOnStart.IsToggled);
            };

            var rawVoiceRecognitionText = new Setting
            { IsToggled = GetRawVoiceRecognitionText(), Text = "Raw Voice Recognition Text" };

            rawVoiceRecognitionText.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled") SetRawVoiceRecognitionText(rawVoiceRecognitionText.IsToggled);
            };

            var largerFont = new Setting
            { IsToggled = GetLargerFont(), Text = "Larger font" };

            largerFont.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled")
                {
                    SetLargerFont(largerFont.IsToggled);
                    MainPage.Label.FontSize = largerFont.IsToggled
                        ? Device.GetNamedSize(NamedSize.Large, typeof(ElmSharp.Label))
                        : Device.GetNamedSize(NamedSize.Micro, typeof(ElmSharp.Label));
                }
            };

            MainPage.Label.FontSize = largerFont.IsToggled
                ? Device.GetNamedSize(NamedSize.Large, typeof(ElmSharp.Label))
                : Device.GetNamedSize(NamedSize.Micro, typeof(ElmSharp.Label));

            var htmlResponse = new Setting
            { IsToggled = GetHtmlResponse(), Text = "HTML Responses" };

            htmlResponse.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled") SetHtmlResponse(htmlResponse.IsToggled);
            };

            Settings.Add(largerFont);
            Settings.Add(autoListenOnStart);
            Settings.Add(autoListenOnResume);
            Settings.Add(rawVoiceRecognitionText);
            Settings.Add(htmlResponse);
        }

    }
}