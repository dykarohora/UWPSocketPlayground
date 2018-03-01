using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StreamSocketSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Scenario2 : Page
    {
        private readonly MainPage _rootPage = MainPage.Current;
        private NetworkAdapter adapter = null;

        public Scenario2()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Make sure we're using the correct server address if an adapter was selected in scenario 1.
            if (CoreApplication.Properties.TryGetValue("serverAddress", out var serverAddress))
            {
                if (serverAddress is string)
                {
                    HostNameForConnect.Text = serverAddress as string;
                }
            }

            adapter = null;
            if (CoreApplication.Properties.TryGetValue("adapter", out var networkAdapter))
            {
                adapter = (NetworkAdapter)networkAdapter;
            }
        }

        private async void ConnectSocket_Click(object sender, RoutedEventArgs e)
        {
            if (CoreApplication.Properties.ContainsKey("clientSocket"))
            {
                _rootPage.NotifyUser(
                    "This step has already been executed. Please move to the next one.",
                    NotifyType.ErrorMessage);
                return;
            }

            if (string.IsNullOrEmpty(ServiceNameForConnect.Text))
            {
                _rootPage.NotifyUser("Please provide a service name.", NotifyType.ErrorMessage);
                return;
            }

            HostName hostName;
            try
            {
                hostName = new HostName(HostNameForConnect.Text);
            }
            catch (ArgumentException)
            {
                _rootPage.NotifyUser("Error: Invalid host name.", NotifyType.ErrorMessage);
                return;
            }

            // クライアントソケットの準備
            var socket = new StreamSocket();
            socket.Control.KeepAlive = false;

            CoreApplication.Properties.Add("clientSocket", socket);

            try
            {
                if (adapter == null)
                {
                    _rootPage.NotifyUser("Connecting to: " + HostNameForConnect.Text, NotifyType.StatusMessage);
                    // サーバソケットへ接続
                    await socket.ConnectAsync(hostName, ServiceNameForConnect.Text);
                    _rootPage.NotifyUser("Connected", NotifyType.StatusMessage);
                }
                else
                {
                    _rootPage.NotifyUser(
                        "Connecting to: " + HostNameForConnect.Text +
                        " using network adapter " + adapter.NetworkAdapterId,
                        NotifyType.StatusMessage);

                    await socket.ConnectAsync(
                        hostName,
                        ServiceNameForConnect.Text,
                        SocketProtectionLevel.PlainSocket,
                        adapter);

                    _rootPage.NotifyUser(
                        "Connected using network adapter " + adapter.NetworkAdapterId,
                        NotifyType.StatusMessage);
                }
                CoreApplication.Properties.Add("connected", null);
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                _rootPage.NotifyUser("Connect failed with error: " + exception.Message, NotifyType.ErrorMessage);
            }
        }
    }
}
