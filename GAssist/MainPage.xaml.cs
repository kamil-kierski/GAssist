
using Samsung.Sap;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Tizen.Applications;
using Tizen.System;
using Tizen.Wearable.CircularUI.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Forms;

namespace GAssist
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : CirclePage
    {

        private Agent agent;
        private Connection connection;
        private Peer peer;
        private Feedback feedback;
        private AudioRecorder audioRecorder;
        private AudioPlayer audioPlayer;
        const int bufferSize = 1024;
        private System.Timers.Timer reconnectTimer;

        private bool isPlaying;

        public MainPage()
        {
            reconnectTimer = new System.Timers.Timer(3000);
            reconnectTimer.Elapsed += ReconnectTimer_Elapsed;
            reconnectTimer.AutoReset = true;

            Connect();
            InitializeComponent();

            PermissionChecker.CheckAndRequestPermission(PermissionChecker.recorderPermission);
            feedback = new Feedback();

            //listView.ItemTapped += ListView_ItemTapped;s
            actionButton.Clicked += ActionButton_ButtonClicked;
            actionButton.IsEnable = false;
            actionButton.BackgroundColor = Color.Default;
            label.Text = "GAssist.Net Demo";

            LaunchApp();

            //Tizen.System.Display.StateChanged += OnDisplayOn;

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

        private void LaunchApp()
        {
            AppControl appControl = new AppControl();
            appControl.Operation = AppControlOperations.Default;
            appControl.ApplicationId = "com.samsung.w-manager-service";
            appControl.ExtraData.Add("deeplink", "cybernetic87://gassist");
            appControl.ExtraData.Add("type", "phone");

            try
            {
                AppControl.SendLaunchRequest(appControl);
            }
            catch (Exception e)
            {
                ShowMessage("APPLAUNCH", "APP NOT FOUND ?");
            }
        }

        private async void Connect()
        {
            try
            {
                agent = await Agent.GetAgent("gassist");
                agent.PeerStatusChanged += PeerStatusChanged;
                var peers = await agent.FindPeers();
                if (peers.Count() > 0)
                {
                    peer = peers.First();
                    connection = peer.Connection;
                    connection.DataReceived -= Connection_DataReceived;
                    connection.DataReceived += Connection_DataReceived;
                    connection.StatusChanged -= Connection_StatusChanged;
                    connection.StatusChanged += Connection_StatusChanged;
                    await connection.Open();
                    if (reconnectTimer.Enabled == true) reconnectTimer.Stop();
                    actionButton.IsEnable = true;
                }
                else
                {
                    ShowMessage("Any peer not found, trying to launch app");
                    LaunchApp();
                    Connect();
                }
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, ex.ToString());
            }
            audioRecorder = new AudioRecorder(connection, agent, bufferSize);
            audioPlayer = new AudioPlayer(OnStopCallback);    
        }

        private void PeerStatusChanged(object sender, PeerStatusEventArgs e)
        {
            if (e.Peer == peer)
            {
                ShowMessage($"Peer Available: {e.Available}, Status: {e.Peer.Status}");
               
            }
        }

        private void Connection_DataReceived(object sender, Samsung.Sap.DataReceivedEventArgs e)
        {
            if (IsBase64(Encoding.UTF8.GetString(e.Data)))
            {
                //audioPlayer.ClearBuffer();
                label.Text = Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.UTF8.GetString(e.Data)));
                //ShowMessage(Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.UTF8.GetString(e.Data))));
            }
            else
            {
                if (e.Data.Length != 0 || e.Data != null)
                {
                    audioPlayer.buffer.Put(e.Data);
                }

                if (audioRecorder.isRecording && !isPlaying)
                {
                    audioRecorder.StopRecording();
                    isPlaying = true;
                    audioPlayer.StartPlaying();

                    actionButton.IsEnable = true;
                    actionButton.BackgroundColor = Color.Red;
                    actionButton.Text = "Stop";
                }

                //if (e.Data.Length == 1)
                //{
                //    audioPlayer.isEnd = true;
                //}

                //stopwatch.Stop();

                //if (!isPlaying && !audioPlayer.isStopped)
                //{//OnStopCallback

                //}

 
            }
        }

        //private void Disconnect()
        //{
        //    if (connection != null)
        //    {
        //        actionButton.IsEnable = true;
        //        connection.Close();
        //    }
        //}

        private void OnStopCallback()
        {
            actionButton.Text = "Listen";
            isPlaying = false;
        }

        private void ActionButton_ButtonClicked(object sender, EventArgs e)
        {
            if (connection != null && agent != null && agent.Channels.Count > 0)
            {
                if (isPlaying)
                {
                    audioPlayer.Stop();
                }
                else
                {
                    feedback.Play(FeedbackType.Sound, "Tap");

                    audioRecorder.StartRecording();
                    actionButton.IsEnable = false;
                }

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

        //private void ListView_ItemTapped(object sender, ItemTappedEventArgs e)
        //{
        //    try
        //    {
        //        switch (e.Item as string)
        //        {
        //            case "Connect":
        //                Connect();
        //                break;

        //            case "Record":
        //                break;

        //            case "Play":
        //                break;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ShowMessage(ex.Message, ex.ToString());
        //    }
        //}

        private void Connection_StatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            
            if (e.Reason == ConnectionStatus.ConnectionClosed ||
                e.Reason == ConnectionStatus.ConnectionLost)
            {

                ShowMessage("Lost connection, will try to reconnect in 3 seconds");
                if (audioRecorder.isRecording)
                {
                    audioRecorder.StopRecording();
                    audioRecorder.isRecording = false;
                }

                if (isPlaying)
                {
                    audioPlayer.Stop();
                    isPlaying = false;
                }
                actionButton.IsEnable = false;
                connection.DataReceived -= Connection_DataReceived;
                connection.StatusChanged -= Connection_StatusChanged;
                connection.Close();
                connection = null;
                peer = null;
                agent = null;

                reconnectTimer.Start();
            }
        }

        private void ReconnectTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ShowMessage("Reconnecting...");
            LaunchApp();
            Connect();
        }

        private void ShowMessage(string message, string debugLog = null)
        {
            Toast.DisplayText(message, 500);
            if (debugLog != null)
            {
                debugLog = message;
            }
            Debug.WriteLine("[DEBUG] " + message);
        }

        public bool IsBase64(string base64String)
        {
            // Credit: oybek https://stackoverflow.com/users/794764/oybek
            if (string.IsNullOrEmpty(base64String) || base64String.Length % 4 != 0
               || base64String.Contains(" ") || base64String.Contains("\t") || base64String.Contains("\r") || base64String.Contains("\n"))
            {
                return false;
            }

            try
            {
                Convert.FromBase64String(base64String);
                return true;
            }
            catch (Exception)
            {
                // Handle the exception
            }
            return false;
        }

        private void OnStopCallback(String callback)
        {
            actionButton.BackgroundColor = Color.Default;
        }
    }
}