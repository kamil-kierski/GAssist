using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Tizen.Applications;
using Tizen.Wearable.CircularUI.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Application = Tizen.Applications.Application;

namespace GAssist
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage
    {
        private static MainPage _mainpage;
        public static Preferences Pref;
        public static readonly Label Label = new Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalOptions = LayoutOptions.FillAndExpand,
            Margin = new Thickness(50, 50, 50, 50),
            Text = "GAssist.Net\n\nPress listen button to start"
        };

        private static WebView WebView2;

        public static InformationPopup TextPopUp;

        public static ProgressPopup ProgressPopup;

        private static readonly string imageDir =
            "/opt/usr/apps/com.cybernetic87.GAssist.Tizen.Wearable/shared/res";

        private readonly SapService _sapService;
        private readonly App _app;


        public MainPage(App app)
        {
            _mainpage = this;
            _app = app;
            InitializeComponent();

            Pref = new Preferences(app, this);
            Pref.LoadSettings();
            SettingsPage.Appearing += SettingsPage_Appearing;
            

            PermissionChecker.CheckAndRequestPermission(PermissionChecker.recorderPermission);
            PermissionChecker.CheckAndRequestPermission(PermissionChecker.mediaStoragePermission);

            _mainpage.ScrollView.Content = Label;
            SetButtonImage("listen_disabled_allgreyedout.png");
            SetActionButtonIsEnabled(false);
            ImageButton.Pressed += ImageButton_PressedAsync;

            TextPopUp = new InformationPopup();

            TextPopUp.BackButtonPressed += (s, e) =>
            {
                TextPopUp.Dismiss();
            };

            ImageButton.Clicked += ActionButton_ButtonClicked;

            _sapService = new SapService(OnConnectedCallback);

            Task.Run(async () => await _sapService.Connect());
        }

        private void SettingsPage_Appearing(object sender, EventArgs e)
        {
            if (MyScroller.ItemsSource == null)
            {
                MyScroller.ItemsSource = Pref.Settings;
            }
        }

        private async void ImageButton_PressedAsync(object sender, EventArgs e)
        {
            await ImageButton.FadeTo(0.5, 300);
            await ImageButton.FadeTo(1, 300);
        }

        public void App_ResumeEvent(object sender, EventArgs e)
        {
            if (SapService.IsConnected && !AudioRecorder.IsRecording)
            {
                if (AudioPlayer.IsPlaying) ResponseHandler.Player.Stop();
                StartListening();
            }
            else if (!SapService.IsConnected)
            {
#pragma warning disable 4014
                _sapService.Connect();
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
            SetButtonImage("listen_blue.png");
            SetActionButtonIsEnabled(true);
            var appid = Application.Current.ApplicationInfo.ApplicationId;
            var arc = new ApplicationRunningContext(appid);
            if (Pref.GetRecordOnStart() && arc.State == ApplicationRunningContext.AppState.Foreground) StartListening();
        }

        internal static void SetLabelText(string text)
        {
            if (_mainpage.ScrollView.Content == Label)
            {
                Label.Text = text;
            }
            else
            {
                Label.Text = text;
                _mainpage.ScrollView.Content = Label;
                _mainpage.ScrollView.Orientation = ScrollOrientation.Vertical;
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
            htmlSource.Html = html; //HtmlResponseParser.ParseHtmlResponse(html);


            //_mainpage.WebView.Source = htmlSource;
            if (_mainpage.ScrollView.Content == WebView2)
            {
                WebView2.Source = htmlSource;
            }
            else
            {
                WebView2 = new WebView
                {
                    //Margin = new Thickness(0, 30),
                    BackgroundColor = Color.Black,
                    AnchorX = 0.5,
                    AnchorY = 0.5,
                    Scale = 0.5,
                    WidthRequest = 720,
                    HeightRequest = 720
                };
                WebView2.Source = htmlSource;
                _mainpage.ScrollView.Content = WebView2;
                _mainpage.ScrollView.Orientation = ScrollOrientation.Both;
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

        private void ActionButton_ButtonClicked(object sender, EventArgs e)
        {
            if (AudioPlayer.IsPlaying)
            {
                ResponseHandler.Player.Stop();
            }
            else if (!AudioRecorder.IsRecording && SapService.IsConnected)
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
            SetLabelText(string.Empty);
        }


        public static void ShowMessage(string message, string debugLog = null)
        {
            Toast.DisplayText(message, 2000);
            Debug.WriteLine("[DEBUG] " + message);
        }
    }
}