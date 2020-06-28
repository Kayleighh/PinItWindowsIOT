using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Windows.UI.Core;
using System.Diagnostics;
using System.Text;
using System.Collections;
using System.Threading;

namespace WindowsIoT
{
    class Connection
    {
        private MqttClient client;
        public Connection()
        {
            // create client instance 
            client = new MqttClient("94.214.252.213");

            client.ProtocolVersion = MqttProtocolVersion.Version_3_1_1;

            string clientId = Guid.NewGuid().ToString();
            client.Connect(clientId, "TvApp", "innovate");
        }
        public MqttClient getMqttConnection()
        {
            return client;
        }
    }
}
