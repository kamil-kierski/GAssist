using Tizen.Applications;

namespace GAssist
{
    internal class Preferences
    {
        public void SetRecordOnStart(bool setting)
        {
            Preference.Set("record_on_start", setting);
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
    }
}