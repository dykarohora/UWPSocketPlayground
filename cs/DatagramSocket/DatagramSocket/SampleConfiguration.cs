using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace DatagramSocket
{
    public partial class MainPage : Page
    {
        public const string FEATURE_NAME = "DatagramSocket";

        private List<Scenario> _scenarios = new List<Scenario>
        {
            new Scenario() { Title = "Start Datagram Listener", ClassType = typeof(DatagramSocket.Scenario1) },
            new Scenario() { Title = "Connect to Listener", ClassType = typeof(DatagramSocket.Scenario2) },
            new Scenario() { Title = "Send Data", ClassType = typeof(DatagramSocket.Scenario3) },
            new Scenario() { Title = "Close Socket", ClassType = typeof(DatagramSocket.Scenario4) },
        };
    }

    public class Scenario
    {
        public string Title { get; set; }
        public Type ClassType { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }
}
