using Google.Assistant.Embedded.V1Alpha2;
using Samsung.Sap;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tizen.Applications;
using Tizen.Multimedia;
using Tizen.System;
using Tizen.Wearable.CircularUI.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GAssist
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : CirclePage
    {
        private Agent agent;
        private Connection connection;
        private Peer peer;
        private AudioRecorder audioRecorder;
        private AudioPlayer audioPlayer;
        private const int bufferSize = 1600;

        private static string filePath = StorageManager.Storages.First().GetAbsolutePath(DirectoryType.Others)
                                + @"temp.mp3";

        private System.Timers.Timer reconnectTimer;
        private FileStream fs;

        private bool isPlaying;

        public MainPage()
        {
            //AudioManager.VolumeController.Level[AudioVolumeType.Media] = 15;

            InitializeComponent();

            reconnectTimer = new System.Timers.Timer(3000);
            reconnectTimer.Elapsed += ReconnectTimer_Elapsed;
            reconnectTimer.AutoReset = true;

            PermissionChecker.CheckAndRequestPermission(PermissionChecker.recorderPermission);
            PermissionChecker.CheckAndRequestPermission(PermissionChecker.mediaStoragePermission);

            actionButton.Clicked += ActionButton_ButtonClicked;
            actionButton.IsEnable = false;
            actionButton.BackgroundColor = Color.Default;
            label.Text = "GAssist.Net Demo";

            StartAndConnect();
            //Tizen.System.Display.StateChanged += OnDisplayOn;
        }

        protected override bool OnBackButtonPressed()
        {
            connection.DataReceived -= Connection_DataReceived;
            connection.StatusChanged -= Connection_StatusChanged;
            connection.Close();
            connection = null;
            peer = null;
            agent = null;
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
            catch (Exception)
            {
                ShowMessage("APPLAUNCH", "APP NOT FOUND ?");
            }
        }

        private async void Connect()
        {
            try
            {
                agent = await Agent.GetAgent("/org/cybernetic87/gassist");
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
                    if (reconnectTimer.Enabled == true)
                    {
                        reconnectTimer.Stop();
                    }

                    actionButton.IsEnable = true;
                }
                else
                {
                    ShowMessage("Any peer not found, trying to launch app");
                    StartAndConnect();
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
            AssistResponse ar = AssistResponse.Parser.ParseFrom(e.Data);

            if (ar.SpeechResults != null)
            {
                if (ar.SpeechResults.Any() && ar.SpeechResults.First().Stability > 0.01)
                {
                    label.Text = ar.SpeechResults.First().Transcript;

                    if (ar.SpeechResults.First().Stability == 1)
                    {
                        audioRecorder.StopRecording();
                        updateLabel(ar.SpeechResults.First().Transcript);

                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                        fs = File.Create(filePath);
                    }
                }
            }

            if (ar.DialogStateOut != null && ar.DialogStateOut.SupplementalDisplayText != "")
            {
                updateLabel(ar.DialogStateOut.SupplementalDisplayText);
            }

            if (ar.DialogStateOut != null && ar.DialogStateOut.VolumePercentage != 0)
            {
                int newVolumeLevel = Convert.ToInt32(15 * ar.DialogStateOut.VolumePercentage / 100);
                AudioManager.VolumeController.Level[AudioVolumeType.Media] = newVolumeLevel;
                actionButton.IsEnable = true;
            }

            if (ar.ScreenOut != null)
            {
                updateLabel(ar.ScreenOut.Data.ToStringUtf8());
            }

            if (ar.AudioOut != null && ar.AudioOut.AudioData.Length != 0)
            {
                try
                {
                    fs.Write(ar.AudioOut.AudioData.ToByteArray(), 0, ar.AudioOut.AudioData.Length);
                    if (fs.Length != 0)
                    {
                        fs.Flush();
                    }

                    if (!isPlaying && fs.Length >= 1600)
                    {
                        isPlaying = true;
                        audioPlayer.Play(fs.Name);
                        actionButton.IsEnable = true;
                        actionButton.BackgroundColor = Color.Red;
                        actionButton.Text = "Stop";
                    }
                }
                catch (System.ObjectDisposedException)
                {
                    Tizen.Log.Debug("AUDIO RESPONSE", "Tried to write to closed FileStream, Knownbug");
                    return;
                }
            }
        }

        private void updateLabel(string text)
        {
            label.Text = text;
        }

        //if (IsBase64(Encoding.UTF8.GetString(e.Data)))
        //{
        //    //audioPlayer.ClearBuffer();
        //    label.Text = Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.UTF8.GetString(e.Data)));
        //    //ShowMessage(Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.UTF8.GetString(e.Data))));
        //    if (Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.UTF8.GetString(e.Data))).StartsWith("Response:"))
        //    {
        //        if (audioRecorder.isRecording) audioRecorder.StopRecording();
        //        label.Text = Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.UTF8.GetString(e.Data)));

        //        if (File.Exists(filePath))
        //        {
        //            File.Delete(filePath);
        //        }
        //        fs = File.Create(filePath);
        //    }
        //}
        //else
        //{
        //    if (e.Data.Length != 0 || e.Data != null)
        //    {
        //        fs.Write(e.Data, 0, e.Data.Length);
        //        fs.Flush();
        //    }

        //    if (!isPlaying)
        //    {
        //        isPlaying = true;
        //        audioPlayer.Play(fs.Name);
        //        actionButton.IsEnable = true;
        //        actionButton.BackgroundColor = Color.Red;
        //        actionButton.Text = "Stop";
        //    }
        //}

        private void OnStopCallback()
        {
            Task.Factory.StartNew(() =>
            {
                fs.Close();
                fs.Dispose();
                actionButton.Text = "Listen";
                isPlaying = false;
            });
        }

        private async void StartAndConnect()
        {
            await Task.Run(() => LaunchApp());
            await Task.Run(() => Connect());
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

        private void Connection_StatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            if (e.Reason == ConnectionStatus.ConnectionClosed ||
                e.Reason == ConnectionStatus.ConnectionLost || e.Reason == ConnectionStatus.Unknown)
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
            StartAndConnect();
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
    }
}