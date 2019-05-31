using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElmSharp;
using Scar.Common;
using Tizen.Multimedia;
using Tizen.Wearable.CircularUI.Forms;
using Xamarin.Forms.Xaml;
using TForms = Xamarin.Forms.Platform.Tizen.Forms;

namespace GAssist
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : IndexPage
    {
        private static MainPage Mainpage;
        private AudioPlayer player;
        private static ProgressBar _progress;
        private static Box _box;
        private static Popup _popUp;
        private static Layout _layout;
        private static Label _progressLabel;
        private static Button _bottomButton;
        private readonly SapService _sapService;
        private readonly Preferences pref;
        private readonly RateLimiter rl = new RateLimiter();
        private bool _isConnected;
        private App app;

        public MainPage(App app)
        {

            //AudioManager.VolumeController.Level[AudioVolumeType.Media] = 15;

            InitializeComponent();

            Mainpage = this;

            PermissionChecker.CheckAndRequestPermission(PermissionChecker.recorderPermission);
            PermissionChecker.CheckAndRequestPermission(PermissionChecker.mediaStoragePermission);
            player = ResponseHandler.player;

            SetActionButtonIsEnabled(false);

            //ActionButtonItem.Clicked += ActionButton_ButtonClicked;

            var test = Observable.FromEventPattern(
                    ev => ActionButtonItem.Clicked += ev,
                    ev => ActionButtonItem.Clicked -= ev)
                //.Throttle(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ => ActionButton_ButtonClicked());

            //test.Subscribe(_ => rl.Throttle(TimeSpan.FromMilliseconds(1500), ActionButton_ButtonClicked, false, true));


            Label.Text = "GAssist.Net Demo";

            pref = new Preferences();

            var AutoListenOnResume = new Setting
            {
                IsToggled = pref.GetRecordOnResume(),
                Text = "Auto Listen On Resume"
            };

            AutoListenOnResume.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled")
                {
                    pref.SetRecordOnResume(AutoListenOnResume.IsToggled);
                    if (AutoListenOnResume.IsToggled) app.ResumeEvent += App_ResumeEvent;
                    else app.ResumeEvent -= App_ResumeEvent;
                }
            };

            var AutoListenOnStart = new Setting
                {IsToggled = pref.GetRecordOnStart(), Text = "Auto Listen On Start"};

            AutoListenOnStart.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled") pref.SetRecordOnStart(AutoListenOnStart.IsToggled);
            };

            Settings.Add(AutoListenOnStart);
            Settings.Add(AutoListenOnResume);

            MyScroller.ItemsSource = Settings;

            _sapService = new SapService(OnConnectedCallback);
            _sapService.StartAndConnect();

            if (pref.GetRecordOnResume()) app.ResumeEvent += App_ResumeEvent;

            player.PlaybackStopped += Player_PlaybackStopped;
            //Tizen.System.Display.StateChanged += OnDisplayOn;
        }

        private void App_ResumeEvent(object sender, EventArgs e)
        {
            StartListening();
        }

        private void Player_PlaybackStopped(object sender, EventArgs e)
        {
            ActionButtonItem.Text = "Listen";
            ResponseHandler.player.IsPlaying = false;
        }

        public IList<Setting> Settings { get; set; } = new ObservableCollection<Setting>();

        private void OnConnectedCallback()
        {
            _isConnected = true;
            if (pref.GetRecordOnStart())
            {
                
                StartListening();
            }
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

        internal static void CreateProgressPopup()
        {
            _popUp = new Popup(TForms.NativeParent);
            _popUp.Style = "circle";

            _layout = new Layout(_popUp);
            _layout.SetTheme("layout", "application", "default");
            _popUp.SetContent(_layout);

            //_box = new Box(_layout);
            //_box.Show();

            _progress = new ProgressBar(_popUp)
            {
                Style = "process"
            };
            _progress.Show();
            _progress.PlayPulse();

            //_box.PackEnd(_progress);
            _layout.SetPartContent("elm.swallow.bg", _progress, true);

            _bottomButton = new Button(_popUp)
            {
                WeightX = 1.0,
                WeightY = 1.0,
                Style = "bottom",
                Text = "Listening"
            };
            _bottomButton.IsEnabled = false;
            _popUp.SetPartContent("button1", _bottomButton);
            _popUp.Show();
        }


        internal static void UpdateProgressPopupText(string text)
        {
            _layout.SetPartText("elm.text", null);
            if (!string.IsNullOrEmpty(text))
            {
                if (_progressLabel == null)
                {
                    _box = new Box(_layout);
                    _box.Show();
                    _progressLabel = new Label(TForms.NativeParent)
                    {
                        TextStyle =
                            "DEFAULT ='font=Tizen:style=Light color=#F9F9F9FF font_size=32 align=center valign=top wrap=mixed'",
                        LineWrapType = WrapType.Mixed,
                        LineWrapWidth = 300
                    };
                }

                _progressLabel.Text = text;
                _progressLabel.Show();
                if (_box != null) _box.PackEnd(_progressLabel);
                _layout.SetPartContent("elm.swallow.content", _box, true);
            }
        }

        internal static void DismissProgressPopup()
        {
            _popUp.Hide();
            _popUp = null;
            _progressLabel = null;
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


        //private void OnStopCallback(EventArgs e)
        //{

        //}

        private async void ActionButton_ButtonClicked()
        {
            await Task.Run(StartListening);
        }

        private void StartListening()
        {
            rl.Throttle(TimeSpan.FromMilliseconds(500), () =>
            {
                if (player.Player.State == PlayerState.Playing) player.Stop();
                else if (!AudioRecorder.IsRecording && _isConnected && player.Player.State != PlayerState.Playing)
                {
                    AudioRecorder.StartRecording();
                    ActionButtonItem.IsEnable = false;
                    ActionButtonItem.Text = "Listening";
                    SetLabelText("");
                }
            },false, true);


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