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

namespace DatagramSocket
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Scenario3 : Page
    {
        private MainPage _rootPage = MainPage.Current;
        public Scenario3()
        {
            this.InitializeComponent();
        }

        // ボタンを押したときのハンドラ
        private async void SendHello_Click(object sender, RoutedEventArgs e)
        {
            // サーバとコネクションがつながっていることを確認する
            if (!CoreApplication.Properties.ContainsKey("connected"))
            {
                _rootPage.NotifyUser("Please run previous steps before doing this one.", NotifyType.ErrorMessage);
                return;
            }

            // クライアントソケットを取得
            Windows.Networking.Sockets.DatagramSocket socket;
            if (!CoreApplication.Properties.TryGetValue("clientSocket", out var outValue))
            {
                _rootPage.NotifyUser("Please run previous steps before doing this one.", NotifyType.ErrorMessage);
                return;
            }

            socket = (Windows.Networking.Sockets.DatagramSocket) outValue;

            // DataWriterをソケットの出力ストリームから作る
            DataWriter writer;
            if (!CoreApplication.Properties.TryGetValue("clientDataWriter", out outValue))
            {
                writer = new DataWriter(socket.OutputStream);
                CoreApplication.Properties.Add("clientDataWriter", writer);
            }
            else
            {
                writer = (DataWriter) outValue;
            }

            const string stringToSend = "Hello";
            writer.WriteString(stringToSend);

            // 送信する
            try
            {
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
    }
}
