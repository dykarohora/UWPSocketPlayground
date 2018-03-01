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

namespace StreamSocketSample
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
            if (CoreApplication.Properties.ContainsKey("listener"))
            {
                _rootPage.NotifyUser(
                    "This step has already been executed. Please move to the next one.",
                    NotifyType.ErrorMessage);
                return;
            }

            if (String.IsNullOrEmpty(ServiceNameForListener.Text))
            {
                _rootPage.NotifyUser("Please provide a service name.", NotifyType.ErrorMessage);
                return;
            }

            CoreApplication.Properties.Remove("serverAddress");
            CoreApplication.Properties.Remove("adapter");

            LocalHostItem selectedLocalHost = null;
            if ((BindToAddress.IsChecked == true) || (BindToAdapter.IsChecked == true))
            {
                selectedLocalHost = (LocalHostItem)AdapterList.SelectedItem;
                if (selectedLocalHost == null)
                {
                    rootPage.NotifyUser("Please select an address / adapter.", NotifyType.ErrorMessage);
                    return;
                }

                // The user selected an address. For demo purposes, we ensure that connect will be using the same
                // address.
                CoreApplication.Properties.Add("serverAddress", selectedLocalHost.LocalHost.CanonicalName);
            }

            // サーバソケットの準備
            var listener = new StreamSocketListener();
            listener.ConnectionReceived += OnConnection;

            listener.Control.KeepAlive = false;
            CoreApplication.Properties.Add("listener", listener);

            try
            {
                if (BindToAny.IsChecked == true)
                {
                    await listener.BindServiceNameAsync(ServiceNameForListener.Text);
                    _rootPage.NotifyUser("Listening", NotifyType.StatusMessage);
                }
                else if (BindToAddress.IsChecked == true)
                {
                    await listener.BindEndpointAsync(selectedLocalHost.LocalHost, ServiceNameForListener.Text);
                    _rootPage.NotifyUser("Listening on address", NotifyType.StatusMessage);
                }
                else if (BindToAdapter.IsChecked == true)
                {
                    var selectedAdapter = selectedLocalHost.LocalHost.IPInformation.NetworkAdapter;
                    CoreApplication.Properties.Add("adapter", selectedAdapter);
                    await listener.BindServiceNameAsync(
                        ServiceNameForListener.Text,
                        SocketProtectionLevel.PlainSocket,
                        selectedAdapter);

                    _rootPage.NotifyUser(
                        "Listening on adapter " + selectedAdapter.NetworkAdapterId,
                        NotifyType.StatusMessage);
                }
            }
            catch (Exception exception)
            {
                CoreApplication.Properties.Remove("listener");
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                _rootPage.NotifyUser(
                    "Start listening failed with error: " + exception.Message,
                    NotifyType.ErrorMessage);
            }
        }

        // コネクションが確立したときのハンドラ
        private async void OnConnection(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var reader = new DataReader(args.Socket.InputStream);
            try
            {
                while (true)
                {
                    // 最初の4バイトを読み込む
                    var sizeFieldCount = await reader.LoadAsync(sizeof(uint));
                    if (sizeFieldCount != sizeof(uint))
                    {
                        return;
                    }

                    var stringLength = reader.ReadUInt32();
                    var actualStringLength = await reader.LoadAsync(stringLength);
                    if (stringLength != actualStringLength)
                    {
                        return;
                    }

                    NotifyUserFromAsyncThread(
                        string.Format("Received data: \"{0}\"", reader.ReadString(actualStringLength)),
                        NotifyType.StatusMessage);
                }
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                NotifyUserFromAsyncThread(
                    "Read stream failed with error: " + exception.Message,
                    NotifyType.ErrorMessage);
            }

        }

        private void NotifyUserFromAsyncThread(string strMessage, NotifyType type)
        {
            var ignore = Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => _rootPage.NotifyUser(strMessage, type));
        }

        private void PopulateAdapterList()
        {
            _localHostItems.Clear();
            AdapterList.ItemsSource = _localHostItems;
            AdapterList.DisplayMemberPath = "DisplayString";

            foreach (HostName localHostInfo in NetworkInformation.GetHostNames())
            {
                if (localHostInfo.IPInformation != null)
                {
                    LocalHostItem adapterItem = new LocalHostItem(localHostInfo);
                    _localHostItems.Add(adapterItem);
                }
            }
        }

    }

    /// <summary>
    /// Helper class describing a NetworkAdapter and its associated IP address
    /// </summary>
    class LocalHostItem
    {
        public string DisplayString {
            get;
            private set;
        }

        public HostName LocalHost {
            get;
            private set;
        }

        public LocalHostItem(HostName localHostName)
        {
            if (localHostName == null)
            {
                throw new ArgumentNullException("localHostName");
            }

            if (localHostName.IPInformation == null)
            {
                throw new ArgumentException("Adapter information not found");
            }

            this.LocalHost = localHostName;
            this.DisplayString = "Address: " + localHostName.DisplayName +
                " Adapter: " + localHostName.IPInformation.NetworkAdapter.NetworkAdapterId;
        }
    }

}
