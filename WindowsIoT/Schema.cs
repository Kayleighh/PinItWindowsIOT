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
    class Schema
    {
        private Connection connect;
        List<string> agenda = new List<string>();
        int i = 0;

        public Schema()
        {

            connect.getMqttConnection().MqttMsgPublishReceived += client_MqttMsgPublishReceivedSchema;

            // subscribe to the topic "/home/temperature" with QoS 2 
            connect.getMqttConnection().Subscribe(new string[] { "/Agenda" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });


        }



        async void client_MqttMsgPublishReceivedSchema(object sender, MqttMsgPublishEventArgs e)
        {

            Debug.WriteLine("Received = " + Encoding.UTF8.GetString(e.Message) + " on topic " + e.Topic + " sender" + sender);
            agenda.Add(Encoding.UTF8.GetString(e.Message));

            await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {

                while (i < agenda.Count)
                {

                    if (agenda.Count > 0)
                    {
                        AgendaHeader1.Text = agenda[i].ToString();
                    }


                    if (agenda.Count > 1)
                    {

                        AgendaHeader2.Text = agenda[i - 1].ToString();



                    }
                    if (agenda.Count > 2)
                    {
                       
                        AgendaHeader3.Text = agenda[i - 2].ToString();
                        agenda.RemoveAt(i - 2);
                     

                    }
                    i++;

                }

            });


        }
    }
}

