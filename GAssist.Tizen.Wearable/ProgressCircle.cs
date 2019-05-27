using ElmSharp;
using TForms = Xamarin.Forms.Platform.Tizen.Forms;

namespace GAssist.Tizen.Wearable
{
    internal class ProgressCircle
    {
        private Box _box;
        private ProgressBar _progress;

        internal void CreateProgressPopup()
        {
            _box = new Box(TForms.NativeParent);
            _box.Show();

            _progress = new ProgressBar(TForms.NativeParent)
            {
                Style = "process/popup/small"
            };
            _progress.Show();
            _progress.PlayPulse();
            _box.PackEnd(_progress);
        }
    }
}