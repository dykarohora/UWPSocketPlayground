using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Appointments;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace DatagramSocket
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Scenario5 : Page
    {
        private MainPage _rootPage = MainPage.Current;

        private Windows.Networking.Sockets.DatagramSocket listenerSocket = null;

        public Scenario5()
        {
            this.InitializeComponent();
        }

        private void CloseListenerSocket()
        {
            if (listenerSocket == null) return;
            listenerSocket.Dispose();
            listenerSocket = null;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MulticastRadioButton.IsChecked = true;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            CloseListenerSocket();
        }

        private void SetupMulticastScenarioUI()
        {
            RemoteAddressLabel.Text = "Multicast Group:";
            StartListener.Content = "Start listener and join multicast group";
            RemoteAddress.Text = "224.3.0.5";
            RemoteAddress.IsEnabled = false;
            SendMessageButton.IsEnabled = false;
            CloseListenerButton.IsEnabled = false;
            SendOutput.Text = "";
        }

        private void SetupBroadcastScenarioUI()
        {
            RemoteAddressLabel.Text = "Broadcast Address:";
            StartListener.Content = "Start listener";
            RemoteAddress.Text = "255.255.255.255";
            RemoteAddress.IsEnabled = false;
            SendMessageButton.IsEnabled = false;
            CloseListenerButton.IsEnabled = false;
            SendOutput.Text = "";
        }


        private void MulticastRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            CloseListenerSocket();
            SetupMulticastScenarioUI();
        }

        private void MulticastRadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            CloseListenerSocket();
            SetupBroadcastScenarioUI();
        }

        private void StartListener_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ServiceName.Text))
            {
                _rootPage.NotifyUser("Please provide a service name.", NotifyType.ErrorMessage);
                return;
            }

            if (listenerSocket != null)
            {
                _rootPage.NotifyUser("A listener socket is already set up.", NotifyType.ErrorMessage);
                return;
            }

            var isMulticastSocket = MulticastRadioButton.IsChecked == true;
            listenerSocket = new Windows.Networking.Sockets.DatagramSocket();
            listenerSocket.MessageReceived += MessageReceived;

            if (isMulticastSocket)
            {
                listenerSocket.Control.MulticastOnly = true;
            }

            try
            {

            }
            catch (Exception exception)
            {
                listenerSocket.Dispose();
                listenerSocket = null;

                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                _rootPage.NotifyUser("Start listening failed with error: " + exception.Message,
                    NotifyType.ErrorMessage);
            }
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            SendOutput.Text = "";

            try
            {
                var remoteHostname = new HostName(RemoteAddress.Text);
                var outputStream = await listenerSocket.GetOutputStreamAsync(remoteHostname, ServiceName.Text);
                const string stringToSend = "Hello";
                var writer = new DataWriter(outputStream);
                writer.WriteString(stringToSend);
                await writer.StoreAsync();

                SendOutput.Text = "\"" + stringToSend + "\" sent successfully.";

            }
            catch (Exception exception)
            {
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                _rootPage.NotifyUser("Send failed with error: " + exception.Message, NotifyType.ErrorMessage);
            }
        }

        private void CloseListener_Click(object sender, RoutedEventArgs e)
        {
            CloseListenerSocket();
            SendMessageButton.IsEnabled = false;
            CloseListenerButton.IsEnabled = false;
            SendOutput.Text = "";

            _rootPage.NotifyUser("Listener closed", NotifyType.StatusMessage);
        }

        private void MessageReceived(Windows.Networking.Sockets.DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            try
            {
                var stringLength = args.GetDataReader().UnconsumedBufferLength;
                var receivedMessage = args.GetDataReader().ReadString(stringLength);

                NotifyUserFromAsyncThread(
                    "Received data from remote peer (Remote Address: " +
                    args.RemoteAddress.CanonicalName +
                    ", Remote Port: " +
                    args.RemotePort + "): \"" +
                    args.RemotePort + "): \"" +
                     receivedMessage + "\"",
                    NotifyType.StatusMessage);
            }
            catch (Exception e)
            {
                var socketError = SocketError.GetStatus(e.HResult);
                if (SocketError.GetStatus(e.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }
                _rootPage.NotifyUser("Error happend when receiving a datagram: " + e.Message,
                    NotifyType.ErrorMessage);
            }


        }

        private void NotifyUserFromAsyncThread(string strMessage, NotifyType type)
        {
            var ignore = Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => _rootPage.NotifyUser(strMessage, type));
        }

    }
}
