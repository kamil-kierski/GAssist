using Samsung.Sap;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Tizen;
using Tizen.Applications;
using Tizen.Network.Bluetooth;
using Tizen.Wearable.CircularUI.Forms;

namespace GAssist
{
    public class SapService
    {
        private static Agent _agent;
        private static Connection _connection;
        private Peer _peer;
        private readonly Timer _reconnectTimer = new Timer(5000);
        private readonly Action _onConnectedCallback;
        private ResponseHandler responseHandler;
        public static InformationPopup TextPopUp;

        public static bool IsConnected { get; private set; }

        public SapService(Action onConnectedCallback)
        {
            _onConnectedCallback = onConnectedCallback;
            _reconnectTimer.Elapsed += ReconnectTimer_Elapsed;
            _reconnectTimer.AutoReset = true;

            TextPopUp = new InformationPopup();

            TextPopUp.BackButtonPressed += (s, e) =>
            {
                TextPopUp.Dismiss();
            };
        }

        private void PromptBluetooth()
        {
            if (!BluetoothAdapter.IsBluetoothEnabled)
            {
                AppControl myAppControl = new AppControl
                {
                    Operation = AppControlOperations.SettingBluetoothEnable
                };
                AppControl.SendLaunchRequest(myAppControl);
                return;
            }
        }

        public async Task Connect()
        {
            _agent = await Agent.GetAgent("/org/cybernetic87/gassist").ConfigureAwait(false);
            _agent.PeerStatusChanged += PeerStatusChanged;
            var peers = await _agent.FindPeers().ConfigureAwait(false);

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
                    await _connection.Open().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    TextPopUp.Text = "Phone app is not configured. Configure the phone app.";
                    TextPopUp.Show();
                    await Task.Delay(2000).ConfigureAwait(false);
                    await Connect().ConfigureAwait(false);
                }
                if (_reconnectTimer.Enabled) _reconnectTimer.Stop();
            }
            else
            {
                if (BluetoothAdapter.IsBluetoothEnabled)
                {
                    if (BluetoothAdapter
                        .GetBondedDevices()
                        .First(x => x.Class.MajorDeviceClassType == BluetoothMajorDeviceClassType.Phone)
                        .IsConnected)
                    {
                        TextPopUp.Text = "Companion app is not installed, check your phone.";
                        TextPopUp.Show();
                        LaunchApp();
                    }
                    else
                    {
                        TextPopUp.Text = "Unable to connect to phone. Please check if Bluetooth is turned on both devices.";
                        TextPopUp.Show();
                    }
                }
                else
                {
                    PromptBluetooth();
                }
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
            //TextPopUp.Text = "Configure companion app on phone";
            await Connect().ConfigureAwait(false);
        }

        internal static void SendData(byte[] dataBytes)
        {
            if (_connection != null && _agent?.Channels.Count > 0)
                _connection.Send(_agent.Channels.First().Value, dataBytes);
        }

        private async void Connection_DataReceived(object sender, DataReceivedEventArgs e)
        {
            await responseHandler.HandleResponse(e.Data).ConfigureAwait(false);
        }

        private void Connection_StatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            if (e.Reason == ConnectionStatus.Connected)
            {
                IsConnected = true;
                TextPopUp.Dismiss();
                _onConnectedCallback();
                if (responseHandler == null) responseHandler = new ResponseHandler();
            }

            if (e.Reason == ConnectionStatus.ConnectionClosed || e.Reason == ConnectionStatus.ConnectionLost || e.Reason == ConnectionStatus.Unknown)
            {
                IsConnected = false;
                _connection.Dispose();
                TextPopUp.Text = "Connection lost. Trying to reconnect in 5s.";
                TextPopUp.Show();
                MainPage.SetActionButtonIsEnabled(false);
                MainPage.SetButtonImage("listen_disabled_allgreyedout.png");

                if (AudioRecorder.IsRecording)
                {
                    AudioRecorder.StopRecording();
                }

                //if (AudioPlayer.IsPlaying)
                //{
                //    AudioPlayer.Stop();
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
            TextPopUp.Text = "Reconnecting...";
            await Connect().ConfigureAwait(false);
        }

        private static void LaunchApp()
        {
            var appControl = new AppControl
            {
                Operation = AppControlOperations.Default,
                ApplicationId = "com.samsung.w-manager-service"
            };
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