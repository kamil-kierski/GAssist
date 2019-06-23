using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Samsung.Sap;
using Tizen;
using Tizen.Applications;

namespace GAssist
{
    public class SapService
    {
        private static Agent _agent;
        private static Connection _connection;
        private Peer _peer;
        private readonly Timer _reconnectTimer = new Timer(3000);
        private readonly Action _onConnectedCallback;

        public SapService(Action onConnectedCallback)
        {
            _onConnectedCallback = onConnectedCallback;
            _reconnectTimer.Elapsed += ReconnectTimer_Elapsed;
            _reconnectTimer.AutoReset = true;
        }

        public async Task Connect()
        {
            _agent = await Agent.GetAgent("/org/cybernetic87/gassist");
            _agent.PeerStatusChanged += PeerStatusChanged;
            var peers = await _agent.FindPeers();

            if (peers.Any())
            {
                _peer = peers.First();
                _connection = _peer.Connection;
                _connection.DataReceived -= Connection_DataReceived;
                _connection.DataReceived += Connection_DataReceived;
                _connection.StatusChanged -= Connection_StatusChanged;
                _connection.StatusChanged += Connection_StatusChanged;
                try
                {
                    await _connection.Open();
                }
                catch (Exception e)
                {
                    await Connect();
                    Log.Debug("CONNECTION", e.Message);
                }


                MainPage.SetActionButtonIsEnabled(true);
                MainPage.SetButtonImage("listen_blue.png");
                if (_reconnectTimer.Enabled) _reconnectTimer.Stop();
            }
            else
            {
                MainPage.TextPopUp.Text = "Companion app is not installed, check your phone.";
                MainPage.TextPopUp.Show();
                //MainPage.ShowMessage("Companion app is not installed, check your phone.");
                LaunchApp();
            }
        }

        public void Disconnect()
        {
            _connection.DataReceived -= Connection_DataReceived;
            _connection.StatusChanged -= Connection_StatusChanged;
            _connection.Close();
            _connection = null;
            _peer = null;
            _agent = null;
        }

        private async void PeerStatusChanged(object sender, PeerStatusEventArgs e)
        {
            MainPage.TextPopUp.Text = "Configure companion app on phone";
            await Connect();
        }

        internal static void SendData(byte[] dataBytes)
        {
            if (_connection != null && _agent != null && _agent.Channels.Count > 0)
                _connection.Send(_agent.Channels.First().Value, dataBytes);
        }

        private static void Connection_DataReceived(object sender, DataReceivedEventArgs e)
        {
            ResponseHandler.HandleResponse(e.Data);
        }

        private void Connection_StatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            if (e.Reason == ConnectionStatus.Connected)
            {
                MainPage.TextPopUp.Dismiss();
                _onConnectedCallback();
            }

            if (e.Reason == ConnectionStatus.ConnectionClosed ||
                e.Reason == ConnectionStatus.ConnectionLost || e.Reason == ConnectionStatus.Unknown)
            {
                _connection.Dispose();
                MainPage.ShowMessage("Lost connection, will try to reconnect in 10 seconds");
                MainPage.IsConnected = false;
                MainPage.SetActionButtonIsEnabled(false);
                MainPage.SetButtonImage("listen_disabled_allgreyedout.png");

                if (AudioRecorder.IsRecording)
                {
                    AudioRecorder.StopRecording();
                    AudioRecorder.IsRecording = false;
                }

                //if (AudioPlayer.IsPlaying)
                //{
                //    AudioPlayer.Stop();
                //    AudioPlayer.IsPlaying = false;
                //}
                _connection.DataReceived -= Connection_DataReceived;
                _connection.StatusChanged -= Connection_StatusChanged;
                _connection.Close();
                _connection = null;
                _peer = null;
                _agent = null;

                _reconnectTimer.Start();
            }
        }

        private async void ReconnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            MainPage.ShowMessage("Reconnecting...");
            await Connect();
        }

        private static void LaunchApp()
        {
            var appControl = new AppControl();
            appControl.Operation = AppControlOperations.Default;
            appControl.ApplicationId = "com.samsung.w-manager-service";
            appControl.ExtraData.Add("deeplink", "https://play.google.com/store/apps/details?id=com.cybernetic87.GAssist");
            appControl.ExtraData.Add("type", "phone");

            try
            {
                AppControl.SendLaunchRequest(appControl);
            }
            catch (Exception)
            {
                Log.Debug("APPLAUNCH", "APP NOT FOUND ?");
            }
        }
    }
}