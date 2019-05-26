using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Tizen.Wearable.CircularUI.Forms;
using Xamarin.Forms.Xaml;

namespace GAssist
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : IndexPage
    {
        private static MainPage Mainpage;
        private readonly SapService _sapService;
        private readonly Preferences pref;

        private static InformationPopup _progressPopUp;
        ElmSharp.ProgressBar _progress;
        ElmSharp.Box _box;

        public MainPage(App app)
        {
            //AudioManager.VolumeController.Level[AudioVolumeType.Media] = 15;

            InitializeComponent();

            Mainpage = this;

            PermissionChecker.CheckAndRequestPermission(PermissionChecker.recorderPermission);
            PermissionChecker.CheckAndRequestPermission(PermissionChecker.mediaStoragePermission);

            ActionButtonItem.IsEnable = false;
            ActionButtonItem.Clicked += ActionButton_ButtonClicked;
            Label.Text = "GAssist.Net Demo";


            pref = new Preferences();
            Check.IsToggled = pref.GetRecordOnStart();
            Check.Toggled += Check_Toggled;

            if (Check.IsToggled)
            {
                app.OnResumeCallback = StartListening;
            }

            _sapService = new SapService();
            _sapService.StartAndConnect();

            AudioPlayer.OnStopCallback = OnStopCallback;
            //Tizen.System.Display.StateChanged += OnDisplayOn;
        }

        private void Check_Toggled(object sender, Xamarin.Forms.ToggledEventArgs e)
        {
            pref.SetRecordOnStart(e.Value);
        }

        internal static void SetLabelText(string text)
        {
            Mainpage.Label.Text = text;
        }

        internal static void SetActionButtonIsEnabled(bool isEnable)
        {
            Mainpage.ActionButtonItem.IsEnable = isEnable;
        }

        internal static void SetActionButtonText(string text)
        {
            Mainpage.ActionButtonItem.Text = text;
        }

        //internal void createProgressPopup()
        //{
        //    _box = new ElmSharp.Box(this.ev);
        //    _box.Show();

        //    _progress = new ElmSharp.ProgressBar(Xamarin.Forms.Platform.Tizen.Forms.NativeParent)
        //    {
        //        Style = "process/popup/small",
        //    };
        //    _progress.Show();
        //    _progress.PlayPulse();
        //    _box.PackEnd(_progress);
        //}
        

        internal static void UpdateProgressPopupText(string text)
        {
            _progressPopUp.Text = text;
        }

        internal static void dismissProgressPopup()
        {
            _progressPopUp?.Dismiss();
            _progressPopUp = null;
        }

        protected override bool OnBackButtonPressed()
        {
            _sapService.Disconnect();
            return base.OnBackButtonPressed();
        }


        //public void OnDisplayOn(object sender, DisplayStateChangedEventArgs args)
        //{
        //    Tizen.Log.Debug("DISPLAYSTATE", args.State.ToString());
        //    if(args.State == DisplayState.Normal)
        //    {
        //        Connect();
        //        OnScreenOnListening();

        //        if (label.Text == "Google")
        //        {
        //            AppControl appControl = new AppControl();
        //            appControl.Operation = AppControlOperations.Default;
        //            appControl.ApplicationId = Tizen.Applications.Application.Current.ApplicationInfo.ApplicationId;
        //            AppControl.SendLaunchRequest(appControl);

        //        }

        //    }

        //}


        private void OnStopCallback()
        {
            Task.Factory.StartNew(() =>
            {
                ActionButtonItem.Text = "Listen";
                AudioPlayer.IsPlaying = false;
            });
        }

        private void ActionButton_ButtonClicked(object sender, EventArgs e)
        {
            StartListening();
        }

        private void StartListening()
        {
            if (AudioPlayer.IsPlaying)
            {
                AudioPlayer.Stop();
            }
            else if (!AudioRecorder.IsRecording)
            {
                AudioRecorder.StartRecording();
                ActionButtonItem.IsEnable = false;
            }
        }

        //private void OnScreenOnListening()
        //{
        //    if (connection != null && agent != null && agent.Channels.Count > 0)
        //    {
        //        if (isPlaying)
        //        {
        //            audioPlayer.Stop();
        //            isPlaying = false;
        //        }
        //        else
        //        {
        //            feedback.Play(FeedbackType.Sound, "Tap");

        //            audioRecorder.StartRecording();
        //            actionButton.IsEnable = false;
        //        }
        //    }
        //}


        public static void ShowMessage(string message, string debugLog = null)
        {
            Toast.DisplayText(message, 500);
            if (debugLog != null) debugLog = message;
            Debug.WriteLine("[DEBUG] " + message);
        }
    }
}