using System;
using Xamarin.Forms;

namespace GAssist
{
    public class App : Application
    {
        public App()
        {
            MainPage = new MainPage(this);
        }

        public Action OnResumeCallback { get; set; }

        protected override void OnResume()
        {
            OnResumeCallback?.Invoke();
            base.OnResume();
        }
    }
}