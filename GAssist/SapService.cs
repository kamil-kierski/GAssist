using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Samsung.Sap;
using Tizen;
using Tizen.Applications;

namespace GAssist
{
    internal class SapService
    {
        private static Agent _agent;
        private static Connection _connection;
        private static Peer _peer;
        private static readonly Timer _reconnectTimer = new Timer(3000);
        private readonly Action _onConnectedCallback;

        public SapService(Action onConnectedCallback)
        {
            _onConnectedCallback = onConnectedCallback;
            _reconnectTimer.Elapsed += ReconnectTimer_Elapsed;
            _reconnectTimer.AutoReset = true;
        }

        private async void Connect()
        {
            try
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
                    await _connection.Open();
                    if (_reconnectTimer.Enabled) _reconnectTimer.Stop();
                }
                else
                {
                    //Log.Debug("SAPSERVICE", "Any peer not found, trying to launch app");
                    StartAndConnect();
                }
            }
            catch (Exception ex)
            {
                // MainPage.ShowMessage(ex.Message, ex.ToString());
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
            if (e.Reason == ConnectionStatus.ConnectionClosed ||
                e.Reason == ConnectionStatus.ConnectionLost || e.Reason == ConnectionStatus.Unknown)
            {
                MainPage.ShowMessage("Lost connection, will try to reconnect in 3 seconds");
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

                MainPage.SetActionButtonIsEnabled(false);
                _connection.DataReceived -= Connection_DataReceived;
                _connection.StatusChanged -= Connection_StatusChanged;
                _connection.Close();
                _connection = null;
                _peer = null;
                _agent = null;

                _reconnectTimer.Start();
            }

            if (e.Reason == ConnectionStatus.Connected) _onConnectedCallback();
        }

        private void ReconnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            MainPage.ShowMessage("Reconnecting...");
            StartAndConnect();
        }

        private static void LaunchApp()
        {
            var appControl = new AppControl();
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
                Log.Debug("APPLAUNCH", "APP NOT FOUND ?");
            }
        }

        public void StartAndConnect()
        {
            Task.Run(LaunchApp).Wait();
            Task.Run(Connect).Wait();

            MainPage.SetActionButtonIsEnabled(true);
        }
    }
}