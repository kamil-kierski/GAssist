using System.Collections.Generic;
using ElmSharp;
using TForms = Xamarin.Forms.Platform.Tizen.Forms;

namespace GAssist
{
    public class ProgressPopup
    {

        private readonly Popup _popUp = new Popup(TForms.NativeParent)
        {
            Style = "circle"
        };

        private readonly ProgressBar _progress;
        private readonly Layout _layout;
        private readonly Box _box;
        private readonly HashSet<string> words = new HashSet<string>();

        public ProgressPopup()
        {
            _progress = new ProgressBar(_popUp);
            _layout = new Layout(_popUp);
            _box = new Box(_layout);

            _layout.SetTheme("layout", "application", "default");
            _popUp.SetContent(_layout);

            _progress.Style = "process";
            _progress.Show();
            _progressLabel.Show();
            _box.Show();
            _layout.SetPartContent("elm.swallow.bg", _progress, false);
            _layout.SetPartContent("elm.swallow.content", _box, false);
            _box.PackEnd(_progressLabel);
        }

        private static readonly Label _progressLabel = new Label(TForms.NativeParent)
        {
            TextStyle =
                "DEFAULT ='font=Tizen:style=Light color=#F9F9F9FF font_size=32 align=center valign=center wrap=mixed'",
            LineWrapType = WrapType.Mixed,
            LineWrapWidth = 300
        };

        public void Show()
        {
            _popUp.Show();
            _progress.PlayPulse();
        }


        public void UpdateText(string text)
        {
            //string[] incomingWords = text.Split();
            //foreach (var word in incomingWords)
            //{
            //    if (words.Add(word.ToLower()))
            //    {
            //        _progressLabel.Text += word + " ";
            //    }
            //}
            _progressLabel.Text = text;
        }

        public void Dismiss()
        {
            _popUp.Hide();
            UpdateText(string.Empty);
            _progress.StopPulse();
            //words.Clear();
        }
    }
}
