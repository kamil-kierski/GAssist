
using Samsung.Sap;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Tizen.Applications;
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
        const int bufferSize = 2048;


        // = false;
        private bool isPlaying;

        public MainPage()
        {
            Connect();
            InitializeComponent();

            PermissionChecker.CheckAndRequestPermission(PermissionChecker.recorderPermission);

            //listView.ItemTapped += ListView_ItemTapped;s
            actionButton.Clicked += ActionButton_ButtonClicked;
            actionButton.IsEnable = false;
            label.Text = "GAssist.Net Demo";

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
                Tizen.Log.Debug("APPLAUNCH", "APP NOT FOUND ?");
            }
            //Feedback feedback = new Feedback();
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
                }
                else
                {
                    ShowMessage("Any peer not found");
                }
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, ex.ToString());
            }
            audioRecorder = new AudioRecorder(connection, agent, bufferSize);
            audioPlayer = new AudioPlayer();

            actionButton.IsEnable = true;
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
                audioPlayer.ClearBuffer();
                audioPlayer.isStopped = false;
                label.Text = Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.UTF8.GetString(e.Data)));
                //ShowMessage(Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.UTF8.GetString(e.Data))));
            }
            else
            {
                if (e.Data.Length != 0 || e.Data != null)
                {
                    audioPlayer.buffer.Put(e.Data);
                }

                if (audioRecorder.isRecording && !audioPlayer.isStopped)
                {
                    audioRecorder.StopRecording();
                    isPlaying = true;
                    actionButton.Text = "Listen";
                    audioPlayer.StartPlaying();
                    actionButton.IsEnable = true;
                    actionButton.BackgroundColor = Color.Red;
                    actionButton.Text = "Stop";
                }

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

        private void ActionButton_ButtonClicked(object sender, EventArgs e)
        {
            if (connection != null && agent != null && agent.Channels.Count > 0)
            {
                if (isPlaying)
                {
                    audioPlayer.Stop();
                    isPlaying = false;
                }
                else
                {
                    audioRecorder.StartRecording();
                    actionButton.IsEnable = false;
                }

            }
        }

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
            ShowMessage(e.Reason.ToString());

            if (e.Reason == ConnectionStatus.ConnectionClosed ||
                e.Reason == ConnectionStatus.ConnectionLost)
            {
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

                connection.DataReceived -= Connection_DataReceived;
                connection.StatusChanged -= Connection_StatusChanged;
                connection.Close();
                connection = null;
                peer = null;
                agent = null;
            }
        }

        private void ShowMessage(string message, string debugLog = null)
        {
            Toast.DisplayText(message, 1500);
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