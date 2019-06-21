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
        private readonly Timer _reconnectTimer = new Timer(10000);
        private readonly Action _onConnectedCallback;

        public SapService(Action onConnectedCallback)
        {
            _onConnectedCallback = onConnectedCallback;
            _reconnectTimer.Elapsed += ReconnectTimer_Elapsed;
            _reconnectTimer.AutoReset = true;
        }

        private async Task Connect()
        {
            _agent = await Agent.GetAgent("/org/cybernetic87/gassist");
            _agent.PeerStatusChanged += PeerStatusChanged;
            var peers = await _agent.FindPeers();
            foreach (var peer in peers)
            {
                Log.Debug("PEERS", peer.ApplicationName);
                Log.Debug("PEERS", peer.Status.ToString());
                Log.Debug("PEERS", peer.Connection.Status.ToString());
            }

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
                    await StartAndConnect();
                    Log.Debug("CONNECTION", e.Message);
                }


                MainPage.SetActionButtonIsEnabled(true);
                MainPage.SetButtonImage("listen_blue.png");
                if (_reconnectTimer.Enabled) _reconnectTimer.Stop();
            }
            else
            {
                MainPage.ShowMessage("Can't connect to phone service...retrying in 10s");
                _reconnectTimer.Start();
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

        private static void PeerStatusChanged(object sender, PeerStatusEventArgs e)
        {
            //if (e.Peer == peer) ShowMessage($"Peer Available: {e.Available}, Status: {e.Peer.Status}");
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
            if (e.Reason == ConnectionStatus.Connected) _onConnectedCallback();

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
            await StartAndConnect();
        }

        private static void LaunchApp()
        {
            var appControl = new AppControl();
            appControl.Operation = AppControlOperations.Default;
            appControl.ApplicationId = "com.samsung.w-manager-service";
            appControl.ExtraData.Add("deeplink", "samsungapps://ProductDetail/com.cybernetic87.GAssist");
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

        public async Task StartAndConnect()
        {
            Task.Run(LaunchApp).Wait();
            await Connect();
        }
    }
}