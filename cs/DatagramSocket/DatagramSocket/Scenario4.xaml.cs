using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
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
    public sealed partial class Scenario4 : Page
    {
        private readonly MainPage _rootPage = MainPage.Current;

        public Scenario4()
        {
            this.InitializeComponent();
        }

        private void CloseSockets_Click(object sender, RoutedEventArgs e)
        {
            // DataWriterの後始末
            if (CoreApplication.Properties.TryGetValue("clientDataWriter", out var outValue))
            {
                CoreApplication.Properties.Remove("clientDataWriter");
                var dataWriter = (DataWriter) outValue;

                dataWriter.DetachStream();
                dataWriter.Dispose();
            }

            // クライアントソケットの後始末
            if (CoreApplication.Properties.TryGetValue("clientSocket", out outValue))
            {
                CoreApplication.Properties.Remove("clientSocket");
                var socket = (Windows.Networking.Sockets.DatagramSocket) outValue;
                socket.Dispose();
            }

            // サーバソケットの後始末
            if (CoreApplication.Properties.TryGetValue("listener", out outValue))
            {
                CoreApplication.Properties.Remove("listener");
                var listener = (Windows.Networking.Sockets.DatagramSocket) outValue;

                listener.Dispose();
            }

            CoreApplication.Properties.Remove("remotePeer");
            CoreApplication.Properties.Remove("connected");
            CoreApplication.Properties.Remove("serverAddress");

            _rootPage.NotifyUser("Socket and Listener closed", NotifyType.StatusMessage);
        }
    }
}
