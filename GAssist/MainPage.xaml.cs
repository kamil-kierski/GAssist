using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Tizen.Applications;
using Tizen.Wearable.CircularUI.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Application = Tizen.Applications.Application;
using Color = Xamarin.Forms.Color;
using Label = Xamarin.Forms.Label;


namespace GAssist
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage
    {
        private static MainPage _mainpage;

        private static readonly Label Label = new Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(50, 50),
            Text = "GAssist.Net Beta"
        };

        public static ProgressPopup ProgressPopup;

        private static readonly string imageDir =
            "/opt/usr/apps/org.tizen.cybernetic87.GAssist.Tizen.Wearable_beta/shared/res";

        public static readonly Preferences Pref = new Preferences();

        public static volatile bool IsConnected;
        private readonly SapService _sapService;
        private readonly App _app;


        public MainPage(App app)
        {
            _mainpage = this;
            _app = app;
            InitializeComponent();
            LoadSettings();

            PermissionChecker.CheckAndRequestPermission(PermissionChecker.recorderPermission);
            PermissionChecker.CheckAndRequestPermission(PermissionChecker.mediaStoragePermission);

            AbsoluteLayout.Children.Add(Label);
            SetButtonImage("listen_disabled_allgreyedout.png");
            SetActionButtonIsEnabled(false);
            ImageButton.IsVisible = true;
            ImageButton.Pressed += ImageButton_PressedAsync;
            


            Observable.FromEventPattern(
                    ev => ImageButton.Clicked += ev,
                    ev => ImageButton.Clicked -= ev)
                //.Throttle(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ => ActionButton_ButtonClicked());

            _sapService = new SapService(OnConnectedCallback);

            Task.Run(async () => await _sapService.StartAndConnect());
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
                if (ResponseHandler.Player.IsPlaying) ResponseHandler.Player.Stop();
                StartListening();
            }
            else if (!IsConnected)
            {
#pragma warning disable 4014
                _sapService.StartAndConnect();
#pragma warning restore 4014
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
            ResponseHandler.Player.PlaybackStopped -= Player_PlaybackStopped;
            ResponseHandler.Player.PlaybackStopped += Player_PlaybackStopped;
            var appid = Application.Current.ApplicationInfo.ApplicationId;
            var arc = new ApplicationRunningContext(appid);
            if (Pref.GetRecordOnStart() && arc.State == ApplicationRunningContext.AppState.Foreground) StartListening();
        }

        internal static void SetLabelText(string text)
        {
            if (_mainpage.AbsoluteLayout.Children.OfType<Label>().Any())
            {
                _mainpage.AbsoluteLayout.Children.OfType<Label>().First().Text = text;
            }
            else
            {
                _mainpage.AbsoluteLayout.Children.Clear();
                _mainpage.ScrollView.Orientation = ScrollOrientation.Vertical;
                Label.Text = text;
                _mainpage.AbsoluteLayout.Children.Add(Label);
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


            _mainpage.WebView.Source = htmlSource;
            if (_mainpage.AbsoluteLayout.Children.OfType<WebView>().Any())
            {
                _mainpage.AbsoluteLayout.Children.OfType<WebView>().First().Source = htmlSource;
            }
            else
            {
                _mainpage.AbsoluteLayout.Children.Clear();
                _mainpage.ScrollView.Orientation = ScrollOrientation.Both;
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


                _mainpage.AbsoluteLayout.Children.Add(webView);
            }
        }

        internal static void SetButtonImage(string img)
        {
            _mainpage.ImageButton.Source = ImageSource.FromFile(Path.Combine(imageDir, img));
        }

        internal static void SetActionButtonIsEnabled(bool isEnable)
        {
            _mainpage.ImageButton.IsEnabled = isEnable;
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
            if (ResponseHandler.Player.IsPlaying)
            {
                ResponseHandler.Player.Stop();
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
            AudioRecorder.StartRecording(Pref.GetHtmlResponse());

            if (ProgressPopup == null)
            {
                ProgressPopup = new ProgressPopup();
            }
            ProgressPopup.Show();
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
                IsToggled = Pref.GetRecordOnResume(),
                Text = "Auto Listen On Resume"
            };

            if (autoListenOnResume.IsToggled) _app.ResumeEvent += App_ResumeEvent;

            autoListenOnResume.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled")
                {
                    Pref.SetRecordOnResume(autoListenOnResume.IsToggled);
                    if (autoListenOnResume.IsToggled) _app.ResumeEvent += App_ResumeEvent;
                    else _app.ResumeEvent -= App_ResumeEvent;
                }
            };

            var autoListenOnStart = new Setting
                {IsToggled = Pref.GetRecordOnStart(), Text = "Auto Listen On Start"};

            autoListenOnStart.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled") Preference.Set("record_on_start", autoListenOnStart.IsToggled);
            };

            var rawVoiceRecognitionText = new Setting
                {IsToggled = Pref.GetRawVoiceRecognitionText(), Text = "Raw Voice Recognition Text"};

            rawVoiceRecognitionText.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled") Pref.SetRawVoiceRecognitionText(rawVoiceRecognitionText.IsToggled);
            };

            var largerFont = new Setting
                {IsToggled = Pref.GetLargerFont(), Text = "Larger font"};

            largerFont.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled")
                {
                    Pref.SetLargerFont(largerFont.IsToggled);
                    Label.FontSize = largerFont.IsToggled
                        ? Device.GetNamedSize(NamedSize.Small, typeof(ElmSharp.Label))
                        : Device.GetNamedSize(NamedSize.Micro, typeof(ElmSharp.Label));
                }
            };

            Label.FontSize = largerFont.IsToggled
                ? Device.GetNamedSize(NamedSize.Small, typeof(ElmSharp.Label))
                : Device.GetNamedSize(NamedSize.Micro, typeof(ElmSharp.Label));

            var htmlResponse = new Setting
                {IsToggled = Pref.GetHtmlResponse(), Text = "HTML Responses"};

            htmlResponse.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "IsToggled") Pref.SetHtmlResponse(htmlResponse.IsToggled);
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