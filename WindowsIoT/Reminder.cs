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
    class Reminder
    {
        private object sender;
        private MqttMsgPublishEventArgs e;
        private Connection connect;
        List<string> remind = new List<string>();
        int i = 0;

        public Reminder()
        {
            // register to message received 
            connect.getMqttConnection().MqttMsgPublishReceived += client_MqttMsgPublishReceivedRemind;

            string clientId = Guid.NewGuid().ToString();
            connect.getMqttConnection().Connect(clientId, "Reminder", "innovate");

            // subscribe to the topic "/home/temperature" with QoS 2 
            connect.getMqttConnection().Subscribe(new string[] { "/Reminder" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }



        public async void client_MqttMsgPublishReceivedRemind(object sender, MqttMsgPublishEventArgs e)
        {
            
            Debug.WriteLine("Received = " + Encoding.UTF8.GetString(e.Message) + " on topic " + e.Topic + " sender" + sender);
            remind.Add(Encoding.UTF8.GetString(e.Message));
            this.sender = sender;
            this.e = e;
            await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {

                while (i < remind.Count)
                {

                    if (remind.Count > 0)
                    {
                        RemindHeader1.Text = remind[i].ToString();
                    }


                    if (remind.Count > 1)
                    {

                        RemindHeader2.Text = remind[i - 1].ToString();



                    }
                    if (remind.Count > 2)
                    {
                        if (remind[i - 2] != null)
                        {
                            RemindHeader3.Text = remind[i - 2].ToString();
                            remind.RemoveAt(i - 2);
                        }

                    }
                    i++;

                }

            });


        }
    }
}

