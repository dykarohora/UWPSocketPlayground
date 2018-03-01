using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
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
    public sealed partial class Scenario3 : Page
    {
        private readonly MainPage _rootPage = MainPage.Current;

        public Scenario3()
        {
            this.InitializeComponent();
        }

        private async void SendHello_Click(object sender, RoutedEventArgs e)
        {
            if (!CoreApplication.Properties.ContainsKey("connected"))
            {
                _rootPage.NotifyUser("Please run previous steps before doing this one.", NotifyType.ErrorMessage);
                return;
            }

            if (!CoreApplication.Properties.TryGetValue("clientSocket", out var outValue))
            {
                _rootPage.NotifyUser("Please run previous steps before doing this one.", NotifyType.ErrorMessage);
                return;
            }

            // クライアントソケットの取得
            var socket = (StreamSocket)outValue;

            DataWriter writer;
            if (!CoreApplication.Properties.TryGetValue("clientDataWriter", out outValue))
            {
                writer = new DataWriter(socket.OutputStream);
                CoreApplication.Properties.Add("clientDataWriter", writer);
            }
            else
            {
                writer = (DataWriter)outValue;
            }

            var stringToSend = "Hello";
            writer.WriteUInt32(writer.MeasureString(stringToSend));
            writer.WriteString(stringToSend);

            try
            {
                await writer.StoreAsync();
                SendOutput.Text = "\"" + stringToSend + "\" sent successfully.";
            }
            catch (Exception exception)
            {
                                // If this is an unknown status it means that the error if fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                _rootPage.NotifyUser("Send failed with error: " + exception.Message, NotifyType.ErrorMessage);

            }
        }
    }
}