using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Core;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace DatagramSocket
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Scenario1 : Page
    {
        private readonly MainPage _rootPage = MainPage.Current;
        private readonly List<LocalHostItem> _localHostItems = new List<LocalHostItem>();

        public Scenario1()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            BindToAny.IsChecked = true;
            PopulateAdapterList();
        }

        private void BindToAny_Checked(object sender, RoutedEventArgs e)
        {
            AdapterList.IsEnabled = false;
        }

        private void BindToAny_Unchecked(object sender, RoutedEventArgs e)
        {
            AdapterList.IsEnabled = true;
        }

        private async void StartListener_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ServiceNameForListener.Text))
            {
                _rootPage.NotifyUser("Please provide a service name.", NotifyType.ErrorMessage);
                return;
            }

            if (CoreApplication.Properties.ContainsKey("listener"))
            {
                _rootPage.NotifyUser(
                    "This step has already been executed. Please move to the next one.",
                    NotifyType.ErrorMessage);
                return;
            }

            CoreApplication.Properties.Remove("serverAddress");

            // サーバソケットの作成
            var listener = new Windows.Networking.Sockets.DatagramSocket();
            // メッセージを受信したときのハンドラ
            listener.MessageReceived += MessageReceivedAsync;

            if (!string.IsNullOrWhiteSpace(InboundBufferSize.Text))
            {
                if (!uint.TryParse(InboundBufferSize.Text, out var inboundBufferSize))
                {
                    _rootPage.NotifyUser(
                        "Please provide a positive numeric Inbound buffer size.",
                        NotifyType.ErrorMessage);
                    return;
                }

                try
                {
                    // 受信時のデータバッファサイズをセット
                    listener.Control.InboundBufferSizeInBytes = inboundBufferSize;
                }
                catch (ArgumentException)
                {
                    _rootPage.NotifyUser("Please provide a valid Inbound buffer size.", NotifyType.ErrorMessage);
                    return;
                }
            }

            // Address or Adapter binding
            LocalHostItem selectedLocalHost = null;
            if ((BindToAddress.IsChecked == true) || (BindToAdapter.IsChecked == true))
            {
                selectedLocalHost = (LocalHostItem) AdapterList.SelectedItem;
                if (selectedLocalHost == null)
                {
                    _rootPage.NotifyUser("Please select an address / adapter.", NotifyType.ErrorMessage);
                    return;
                }

                CoreApplication.Properties.Add("serverAddress", selectedLocalHost.LocalHost.CanonicalName);
            }

            // 作成したソケットをアプリに保存する
            CoreApplication.Properties.Add("listener", listener);

            // Listenの開始
            try
            {
                if (BindToAny.IsChecked == true)
                {
                    // コントロールに入力されたポートでListenする
                    await listener.BindServiceNameAsync(ServiceNameForListener.Text);
                    _rootPage.NotifyUser("Listening", NotifyType.StatusMessage);
                }else if (BindToAddress.IsChecked == true)
                {
                    if (selectedLocalHost == null) return;
                    await listener.BindEndpointAsync(selectedLocalHost.LocalHost, ServiceNameForListener.Text);
                    _rootPage.NotifyUser(
                        "Listening on addrress " + selectedLocalHost.LocalHost.CanonicalName,
                        NotifyType.StatusMessage);
                }else if (BindToAdapter.IsChecked == true)
                {
                    if (selectedLocalHost == null) return;
                    var selectedAdapter = selectedLocalHost.LocalHost.IPInformation.NetworkAdapter;

                    await listener.BindServiceNameAsync(ServiceNameForListener.Text, selectedAdapter);

                    _rootPage.NotifyUser(
                        "Listening on adapter " + selectedAdapter.NetworkAdapterId,
                        NotifyType.StatusMessage);
                }
            }
            catch (Exception exception)
            {
                CoreApplication.Properties.Remove("listenner");
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                _rootPage.NotifyUser(
                    "Start listening failed with error: " + exception.Message,
                    NotifyType.ErrorMessage);
            }

        }

        // Listenerがメッセージを受信したときのハンドラ
        private async void MessageReceivedAsync(Windows.Networking.Sockets.DatagramSocket socket, DatagramSocketMessageReceivedEventArgs args)
        {
            if (CoreApplication.Properties.TryGetValue("remotePeer", out var outObj))
            {
                EchoMessage((RemotePeer) outObj, args);
                return;
            }

            try
            {
                // SenderのアドレスとポートからOutputStreamを取得
                var outputStream = await socket.GetOutputStreamAsync(
                    args.RemoteAddress,
                    args.RemotePort);

                RemotePeer peer;
                lock (this)
                {
                    if (CoreApplication.Properties.TryGetValue("remotePeer", out outObj))
                    {
                        peer = (RemotePeer) outObj;
                    }
                    else
                    {
                        peer = new RemotePeer(outputStream, args.RemoteAddress, args.RemotePort);
                        CoreApplication.Properties.Add("remotePeer", peer);
                    }
                }

                EchoMessage(peer, args);
            }
            catch (Exception exception)
            {
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                NotifyUserFromAsyncThread("Connect failed with error: " + exception.Message, NotifyType.ErrorMessage);
            }
        }

        // 受信したメッセージをエコーバックする
        private async void EchoMessage(RemotePeer peer, DatagramSocketMessageReceivedEventArgs args)
        {
            if (!peer.IsMatching(args.RemoteAddress, args.RemotePort))
            {
                NotifyUserFromAsyncThread(
                    string.Format(
                        "Got datagram from {0}:{1}, but already 'connected' to {2}",
                        args.RemoteAddress,
                        args.RemotePort,
                        peer),
                    NotifyType.ErrorMessage);
            }

            try
            {
                await peer.OutputStream.WriteAsync(args.GetDataReader().DetachBuffer());
            }
            catch (Exception exception)
            {
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }
                NotifyUserFromAsyncThread("Send failed with error: " + exception.Message, NotifyType.ErrorMessage);
            }
        }

        private void NotifyUserFromAsyncThread(string s, NotifyType errorMessage)
        {
            var ignore = Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => _rootPage.NotifyUser(s, errorMessage));
        }

        private void PopulateAdapterList()
        {
            _localHostItems.Clear();
            AdapterList.ItemsSource = _localHostItems;
            AdapterList.DisplayMemberPath = "DisplayString";

            foreach (var localHostInfo in NetworkInformation.GetHostNames())
            {
                if (localHostInfo.IPInformation == null) continue;
                var adapterItem = new LocalHostItem(localHostInfo);
                _localHostItems.Add(adapterItem);
            }
        }
    }

    internal class RemotePeer
    {
        private readonly HostName _hostName;
        private readonly string _port;

        public RemotePeer(IOutputStream outputStream, HostName hostName, string port)
        {
            OutputStream = outputStream;
            _hostName = hostName;
            _port = port;
        }

        public bool IsMatching(HostName hostName, string port)
        {
            return (_hostName == hostName && _port == port);
        }

        public IOutputStream OutputStream { get; }

        public override string ToString()
        {
            return _hostName + _port;
        }
    }

    internal class LocalHostItem
    {
        public LocalHostItem(HostName localHost)
        {
            if (localHost == null)
            {
                throw new ArgumentException("LocalHost name is not allowed null.");
            }

            if (localHost.IPInformation == null)
            {
                throw new ArgumentException("Adapter information not found");
            }
            LocalHost = localHost;
            DisplayString = "Address: " + localHost.DisplayName + " Adapter: " +
                            localHost.IPInformation.NetworkAdapter.NetworkAdapterId;
        }

        public string DisplayString { get; private set; }
        public HostName LocalHost { get; private set; }
    }
}
