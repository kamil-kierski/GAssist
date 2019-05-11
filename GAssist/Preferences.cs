using System;
using System.Collections.Generic;
using System.Text;

namespace GAssist
{
    class Preferences
    {
        public void SetRecordOnStart(bool setting)
        {
            Tizen.Applications.Preference.Set("record_on_start", setting);
        }

        public bool GetRecordOnStart()
        {
            return Tizen.Applications.Preference.Get<bool>("record_on_start");
        }

    }
}
