using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ElmSharp;
using Tizen.Applications;
using Tizen.Wearable.CircularUI.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Application = Tizen.Applications.Application;
using Color = Xamarin.Forms.Color;
using Label = Xamarin.Forms.Label;
using Layout = ElmSharp.Layout;
using ProgressBar = ElmSharp.ProgressBar;
using TForms = Xamarin.Forms.Platform.Tizen.Forms;

namespace GAssist
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage
    {
        private static MainPage Mainpage;


        private static readonly Popup _popUp = new Popup(TForms.NativeParent)
        {
            Style = "circle"
        };

        private static readonly ProgressBar _progress = new ProgressBar(_popUp);
        private static readonly Layout _layout = new Layout(_popUp);
        private static readonly Box _box = new Box(_layout);

        private static readonly Label label = new Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(50, 50),
            Text = "GAssist.Net Beta"
        };

        private static readonly ElmSharp.Label _progressLabel = new ElmSharp.Label(TForms.NativeParent)
        {
            TextStyle =
                "DEFAULT ='font=Tizen:style=Light color=#F9F9F9FF font_size=32 align=center valign=top wrap=mixed'",
            LineWrapType = WrapType.Mixed,
            LineWrapWidth = 300
        };

        private static readonly string imageDir =
            "/opt/usr/apps/org.tizen.cybernetic87.GAssist.Tizen.Wearable_beta/shared/res";

        public static readonly Preferences pref = new Preferences();

        //private readonly RateLimiter rl = new RateLimiter();
        public static bool IsConnected;
        private readonly SapService _sapService;
        private readonly App app;


        public MainPage(App app)
        {
            Mainpage = this;
            this.app = app;
            InitializeComponent();
            LoadSettings();
            CreateProgressPopup();

            PermissionChecker.CheckAndRequestPermission(PermissionChecker.recorderPermission);
            PermissionChecker.CheckAndRequestPermission(PermissionChecker.mediaStoragePermission);

            ResponseHandler.player.PlaybackStopped += Player_PlaybackStopped;

            SetButtonImage("listen_disabled_allgreyedout.png");
            SetActionButtonIsEnabled(false);
            ImageButton.IsVisible = true;
            ImageButton.Pressed += ImageButton_PressedAsync;
            AbsoluteLayout.Children.Add(label);


            Observable.FromEventPattern(
                    ev => ImageButton.Clicked += ev,
                    ev => ImageButton.Clicked -= ev)
                //.Throttle(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ => ActionButton_ButtonClicked());

            _sapService = new SapService(OnConnectedCallback);

            Task.Run(async () => await _sapService.StartAndConnect());

            if (pref.GetRecordOnResume()) app.ResumeEvent += App_ResumeEvent;
        }

        public IList<Setting> Settings { get; set; } = new ObservableCollection<Setting>();

        private async void ImageButton_PressedAsync(object sender, EventArgs e)
        {
            await ImageButton.FadeTo(0.5, 300);
            await ImageButton.FadeTo(1, 300);
        }

        private void App_ResumeEvent(object sender, EventArgs e)
        {
            if (IsConnected && !AudioRecorder.IsRecording)
            {
                if (ResponseHandler.player.IsPlaying) ResponseHandler.player.Stop();
                StartListening();
            }
            else if (!IsConnected)
            {
                _sapService.StartAndConnect();
            }
        }

        private void Player_PlaybackStopped(object sender, EventArgs e)
        {
            SetButtonImage("listen_blue.png");
            SetActionButtonIsEnabled(true);
        }

        private void OnConnectedCallback()
        {
            IsConnected = true;
            SetButtonImage("listen_blue.png");
            SetActionButtonIsEnabled(true);
            var appid = Application.Current.ApplicationInfo.ApplicationId;
            var arc = new ApplicationRunningContext(appid);
            if (pref.GetRecordOnStart() && arc.State == ApplicationRunningContext.AppState.Foreground) StartListening();
        }

        internal static void SetLabelText(string text)
        {
            if (Mainpage.AbsoluteLayout.Children.OfType<Label>().Any())
            {
                Mainpage.AbsoluteLayout.Children.OfType<Label>().First().Text = text;
            }
            else
            {
                Mainpage.AbsoluteLayout.Children.Clear();
                Mainpage.ScrollView.Orientation = ScrollOrientation.Vertical;
                label.Text = text;
                Mainpage.AbsoluteLayout.Children.Add(label);
            }
        }

        internal static void SetHtmlView(string html)
        {
            var htmlSource = new HtmlWebViewSource();
            //var test = File.ReadAllText(Path.Combine(imageDir, "test.html"));


            //var html_mod = string.Format(
            //    "<iframe width=\"360\" height=\"360\" src=\"{0}\" style=\"-webkit-transform:scale(0.5);-moz-transform-scale(0.5);\"></iframe>",
            //    html);

            //htmlSource.Html = html_mod;
            htmlSource.Html = html;


            Mainpage.WebView.Source = htmlSource;
            if (Mainpage.AbsoluteLayout.Children.OfType<WebView>().Any())
            {
                Mainpage.AbsoluteLayout.Children.OfType<WebView>().First().Source = htmlSource;
            }
            else
            {
                Mainpage.AbsoluteLayout.Children.Clear();
                Mainpage.ScrollView.Orientation = ScrollOrientation.Both;
                var webView = new WebView
                {
                    ScaleX = 0.5,
                    ScaleY = 0.5,
                    //Margin = new Thickness(0, 30),
                    BackgroundColor = Color.Black,
                    AnchorX = 0,
                    AnchorY = 0,
                    Source = htmlSource,
                    HeightRequest = 720,
                    WidthRequest = 720
                };


                Mainpage.AbsoluteLayout.Children.Add(webView);
            }
        }

        internal static void SetButtonImage(string img)
        {
            Mainpage.ImageButton.Source = ImageSource.FromFile(Path.Combine(imageDir, img));
        }

        internal static void SetActionButtonIsEnabled(bool isEnable)
        {
            Mainpage.ImageButton.IsEnabled = isEnable;
        }

        private static void CreateProgressPopup()
        {
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


        private static void ShowProgressPopup()
        {
            _popUp.Show();
            _progress.PlayPulse();
        }


        internal static void UpdateProgressPopupText(string text)
        {
            _progressLabel.Text = text;
        }

        internal static void DismissProgressPopup()
        {
            _popUp.Hide();
            _progressLabel.Text = null;
            _progress.StopPulse();
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

        private void ActionButton_ButtonClicked()
        {
            if (ResponseHandler.player.IsPlaying)
            {
                ResponseHandler.player.Stop();
            }
            else if (!AudioRecorder.IsRecording && IsConnected)
            {
                StartListening();
                SetActionButtonIsEnabled(false);
                SetButtonImage("listen_disabled_allgreyedout.png");
            }
        }

        private void StartListening()
        {
            //NoResponseTimer.Start();
            AudioRecorder.StartRecording(pref.GetHtmlResponse());

            ShowProgressPopup();
            //SetActionButtonIsEnabled(false); button dissapears when listening
            //SetButtonImage("listen_green.png"); maybe animated in future
            SetLabelText("");
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

        private void LoadSettings()
        {
            var autoListenOnResume = new Setting
            {
                IsToggled = pref.GetRecordOnResume(),
                Text = "Auto Listen On Resume"
            };

            autoListenOnResume.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled")
                {
                    pref.SetRecordOnResume(autoListenOnResume.IsToggled);
                    if (autoListenOnResume.IsToggled) app.ResumeEvent += App_ResumeEvent;
                    else app.ResumeEvent -= App_ResumeEvent;
                }
            };

            var autoListenOnStart = new Setting
                {IsToggled = pref.GetRecordOnStart(), Text = "Auto Listen On Start"};

            autoListenOnStart.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled") Preference.Set("record_on_start", autoListenOnStart.IsToggled);
            };

            var rawVoiceRecognitionText = new Setting
                {IsToggled = pref.GetRawVoiceRecognitionText(), Text = "Raw Voice Recognition Text"};

            rawVoiceRecognitionText.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled") pref.SetRawVoiceRecognitionText(rawVoiceRecognitionText.IsToggled);
            };

            var largerFont = new Setting
                {IsToggled = pref.GetLargerFont(), Text = "Larger font"};

            largerFont.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled")
                {
                    pref.SetLargerFont(largerFont.IsToggled);
                    label.FontSize = largerFont.IsToggled
                        ? Device.GetNamedSize(NamedSize.Small, typeof(ElmSharp.Label))
                        : Device.GetNamedSize(NamedSize.Micro, typeof(ElmSharp.Label));
                }
            };

            label.FontSize = largerFont.IsToggled
                ? Device.GetNamedSize(NamedSize.Small, typeof(ElmSharp.Label))
                : Device.GetNamedSize(NamedSize.Micro, typeof(ElmSharp.Label));

            var htmlResponse = new Setting
                {IsToggled = pref.GetHtmlResponse(), Text = "HTML Responses"};

            htmlResponse.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled") pref.SetHtmlResponse(htmlResponse.IsToggled);
            };

            Settings.Add(largerFont);
            Settings.Add(autoListenOnStart);
            Settings.Add(autoListenOnResume);
            Settings.Add(rawVoiceRecognitionText);
            Settings.Add(htmlResponse);

            MyScroller.ItemsSource = Settings;
        }

        public static void ShowMessage(string message, string debugLog = null)
        {
            Toast.DisplayText(message, 1000);
            Debug.WriteLine("[DEBUG] " + message);
        }
    }
}