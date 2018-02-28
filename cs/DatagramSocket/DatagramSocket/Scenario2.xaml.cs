using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Sockets;
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
    public sealed partial class Scenario2 : Page
    {
        private MainPage _rootPage = MainPage.Current;
        public Scenario2()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Scenario1で設定された宛先を使用する
            if (!CoreApplication.Properties.TryGetValue("serverAddress", out var serverAddress)){
                return;
            }
            if (serverAddress is string s)
            {
                HostNameForConnect.Text = s;
            }
        }


        private async void ConnectSocket_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ServiceNameForConnect.Text))
            {
                _rootPage.NotifyUser("Please provide a service name.", NotifyType.ErrorMessage);
                return;
            }

            HostName hostName;
            try
            {
                hostName =new HostName(HostNameForConnect.Text);
            }
            catch (ArgumentException )
            {
                _rootPage.NotifyUser("Error: Invalid host name.", NotifyType.ErrorMessage);
                return;
            }

            if (CoreApplication.Properties.ContainsKey("clientSocket"))
            {
                _rootPage.NotifyUser("This step has already been executed. Please move to the next one.",
                    NotifyType.ErrorMessage);
                return;
            }

            // クライアントソケットの作成
            var socket = new Windows.Networking.Sockets.DatagramSocket();
            if (DontFragment.IsOn)
            {
                // IPフラグメンテーションを許可しない
                socket.Control.DontFragment = true;
            }

            socket.MessageReceived += MessageReceived;
            CoreApplication.Properties.Add("clientSocket", socket);
            _rootPage.NotifyUser("Connecting to: " + HostNameForConnect.Text, NotifyType.StatusMessage);

            try
            {
                // サーバソケットに接続しにいく
                await socket.ConnectAsync(hostName, ServiceNameForConnect.Text);
                _rootPage.NotifyUser("Connected", NotifyType.StatusMessage);
                CoreApplication.Properties.Add("connected", null);
            }
            catch (Exception exception)
            {
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                _rootPage.NotifyUser("Connect failed with error: " + exception.Message, NotifyType.ErrorMessage);
            }
        }

        // メッセージを受信したらUIに見せる
        private void MessageReceived(Windows.Networking.Sockets.DatagramSocket socket, DatagramSocketMessageReceivedEventArgs args)
        {
            try
            {
                var stringLength = args.GetDataReader().UnconsumedBufferLength;
                var receivedMessage = args.GetDataReader().ReadString(stringLength);

                NotifyUserFromAsyncThread(
                    "Received data from remote peer: \"" +
                    receivedMessage + "\"",
                    NotifyType.StatusMessage);
            }
            catch (Exception exception)
            {
                var socketError = SocketError.GetStatus(exception.HResult);
                if (socketError == SocketErrorStatus.ConnectionResetByPeer)
                {
                    NotifyUserFromAsyncThread(
                        "Peer does not listen on the specific port. Please make sure that you run step 1 first " +
                        "or you have a server properly working on a remote server.",
                        NotifyType.ErrorMessage);
                }else if (socketError != SocketErrorStatus.Unknown)
                {
                    NotifyUserFromAsyncThread(
                        "Error happened when receiving a datagram: " + socketError.ToString(),
                        NotifyType.ErrorMessage);
                }
                else
                {
                    throw;
                }
            }
        }

        private void NotifyUserFromAsyncThread(string strMessage, NotifyType type)
        {
            var ignore = Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => _rootPage.NotifyUser(strMessage, type));
        }
    }
}
