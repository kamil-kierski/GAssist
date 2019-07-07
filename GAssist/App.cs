using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
            Task.Run(() =>
            {
                PermissionChecker.CheckAndRequestPermission(PermissionChecker.recorderPermission);
                PermissionChecker.CheckAndRequestPermission(PermissionChecker.mediaStoragePermission);
            });
            base.OnStart();
        }

        protected override void OnResume()
        {
            OnResumeEvent(EventArgs.Empty);
            base.OnResume();
        }

        protected override void OnBindingContextChanged()
        {
            Tizen.Log.Debug("CONTEXT", "binding context");
            base.OnBindingContextChanged();
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Tizen.Log.Debug("CONTEXT", "property");
            base.OnPropertyChanged(propertyName);
        }

        protected virtual void OnResumeEvent(EventArgs args)
        {
            ResumeEvent?.Invoke(this, args);
        }
    }
}