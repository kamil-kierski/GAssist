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

        public event EventHandler ResumeEvent;

        protected override void OnStart()
        {
            PermissionChecker.CheckAndRequestPermission(PermissionChecker.recorderPermission);
            PermissionChecker.CheckAndRequestPermission(PermissionChecker.mediaStoragePermission);
            base.OnStart();
        }

        protected override void OnResume()
        {
            OnResumeEvent(EventArgs.Empty);
            base.OnResume();
        }

        protected virtual void OnResumeEvent(EventArgs args)
        {
            ResumeEvent?.Invoke(this, args);
        }
    }
}