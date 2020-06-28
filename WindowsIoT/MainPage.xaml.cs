using System;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Windows.UI.Core;
using System.Diagnostics;
using System.Text;
using Windows.System.Threading;
using System.Threading;


namespace WindowsIoT
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static MqttClient client;
        private ThreadPoolTimer ConnectUpdateTimer;
        private ThreadPoolTimer PresenceUpdateTimer;
        private ThreadPoolTimer ReminderUpdateTimer;
        private ThreadPoolTimer AgendaUpdateTimer;
        private ThreadPoolTimer ClearRenewReminder;
        private ThreadPoolTimer ClearRenewAgenda;
        private ThreadPoolTimer ClearRenewPresence;
        private DispatcherTimer timer = new DispatcherTimer();
        private rss r = new rss();                                  //New Rss instance at startup
        List<Reminders> RemindersList = new List<Reminders>();      //List for Reminder instances (MQTT messages)
        List<Agenda> AgendaList = new List<Agenda>();               //List for Agenda instances (MQTT messages)   
        List<Presence> PresenceList = new List<Presence>();         //List for Presence instances (MQTT messages)

        //counters for the Presence section with default values
        int p1 = 0;
        int p2 = 1;
        int p3 = 2;
        //counters for the Reminder section with default values
        int r1 = 0;
        int r2 = 1;
        int r3 = 2;
        //counters for the Agenda section with default values
        int a1 = 0;
        int a2 = 1;
        int a3 = 2;
        int a4 = 3;


        public MainPage()
        {
            this.InitializeComponent();

            // create client instance 
            client = new MqttClient("194.171.181.139");

            client.ProtocolVersion = MqttProtocolVersion.Version_3_1_1;
            // register to message received 
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceivedRemind;

            ConnectMqtt();
            //At startup ask for available messages        

            RequestDataBaseAgenda();
            RequestDataBasePresence();
            RequestDataBaseReminders();

            //load rss for the first time at startup
            LoadRss();

            //check mqtt connection, if false reconnect
            ConnectUpdateTimer = ThreadPoolTimer.CreatePeriodicTimer((t) =>
            {
                if (client.IsConnected == false)
                {
                    ConnectMqtt();
                }
            }, TimeSpan.FromSeconds(5));

            //Shuffle presence
            PresenceUpdateTimer = ThreadPoolTimer.CreatePeriodicTimer((t) =>
            {        
                UpdatePresence();
            }, TimeSpan.FromSeconds(6));

            //Shuffle Reminder
            ReminderUpdateTimer = ThreadPoolTimer.CreatePeriodicTimer((t) =>
            {
                UpdateReminders();
            }, TimeSpan.FromSeconds(10));

            //shuffle Agenda
            AgendaUpdateTimer = ThreadPoolTimer.CreatePeriodicTimer((t) =>
            {
                UpdateAgenda();
            }, TimeSpan.FromSeconds(15));

            //clear and renew Reminders 
            ClearRenewReminder = ThreadPoolTimer.CreatePeriodicTimer((t) =>
            {
                RequestDataBaseReminders();
            }, TimeSpan.FromMinutes(5));

            //clear and renew Agenda 
            ClearRenewAgenda = ThreadPoolTimer.CreatePeriodicTimer((t) =>
            {
                RequestDataBaseAgenda();
            }, TimeSpan.FromMinutes(5));

            //clear and renew presence 
            ClearRenewPresence = ThreadPoolTimer.CreatePeriodicTimer((t) =>
            {
                RequestDataBasePresence();
            }, TimeSpan.FromMinutes(5));

        }

        //connect and subscribe MQTT method
        async void ConnectMqtt()
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                string clientId = Guid.NewGuid().ToString();
                try
                {
                    client.Connect(clientId, "TvApp", "innovate");
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }

                // subscribe to the topic "/PinIt/Inf/#" (with QoS 2) 
                client.Subscribe(new string[] { "/PinIt/Inf/#" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            });
        }

        async void client_MqttMsgPublishReceivedRemind(object sender, MqttMsgPublishEventArgs e)
        {
            if (e.Topic.Contains("/ReminderMessagesTMP") || e.Topic.Contains("/ReminderMessages/TVapp"))
            {
                string message_R = Encoding.UTF8.GetString(e.Message);

                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (message_R.Contains('@'))
                    {
                        string[] wordsR = message_R.ToString().Split('@');

                        Reminders a = new Reminders();
                        a.Name = wordsR[0];
                        a.Title = wordsR[1];
                        a.Message = wordsR[2];
                        RemindersList.Insert(0, a);
                    }
                    if (RemindersList.Count >= 4)
                    {
                        r1 = 0;
                        r2 = 1;
                        r3 = 2;
                        ReminderName1.Text = RemindersList[0].Name.ToString();
                        RemindHeader1.Text = RemindersList[0].Title.ToString();
                        RemindText1.Text = RemindersList[0].Message.ToString();

                        ReminderName2.Text = RemindersList[1].Name.ToString();
                        RemindHeader2.Text = RemindersList[1].Title.ToString();
                        RemindText2.Text = RemindersList[1].Message.ToString();

                        ReminderName3.Text = RemindersList[2].Name.ToString();
                        RemindHeader3.Text = RemindersList[2].Title.ToString();
                        RemindText3.Text = RemindersList[2].Message.ToString();
                    }
                });
            }
            if (e.Topic.Contains("/AgendaMessagesTMP") || e.Topic.Contains("/AgendaMessages/TVapp"))
            {
                string message_A = Encoding.UTF8.GetString(e.Message);

                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (message_A.Contains('@'))
                    {
                        string[] wordsA = message_A.ToString().Split('@');

                        Agenda b = new Agenda();
                        b.AgendaDate = wordsA[0];
                        b.Title = wordsA[1];
                        b.Message = wordsA[2];
                        AgendaList.Insert(0, b);
                    }
                    if (AgendaList.Count >= 5)
                    {
                        a1 = 0;
                        a2 = 1;
                        a3 = 2;
                        a4 = 3;

                        AgendaDate1.Text = AgendaList[0].AgendaDate.ToString();
                        AgendaHeader1.Text = AgendaList[0].Title.ToString();
                        AgendaText1.Text = AgendaList[0].Message.ToString();

                        AgendaDate2.Text = AgendaList[1].AgendaDate.ToString();
                        AgendaHeader2.Text = AgendaList[1].Title.ToString();
                        AgendaText2.Text = AgendaList[1].Message.ToString();

                        AgendaDate3.Text = AgendaList[2].AgendaDate.ToString();
                        AgendaHeader3.Text = AgendaList[2].Title.ToString();
                        AgendaText3.Text = AgendaList[2].Message.ToString();

                        AgendaHeader4.Text = AgendaList[3].AgendaDate.ToString();
                        AgendaHeader4.Text = AgendaList[3].Title.ToString();
                        AgendaText4.Text = AgendaList[3].Message.ToString();
                    }
                });
            }
            if (e.Topic.Contains("/PresenceState/TVapp") || e.Topic.Contains("/PresenceStateTMP"))
            {
                string message_P = (Encoding.UTF8.GetString(e.Message));

                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    string name = message_P;
                    if (message_P.Contains('1'))
                    {
                        name = message_P.ToString().Split('@').First();
                        Presence c = new Presence();
                        c.Name = name;
                        PresenceList.Insert(0, c);
                    }
                    else if (message_P.Contains('0'))
                    {
                        int index = -1;
                        name = message_P.ToString().Split('@').First();

                        foreach (Presence presence in PresenceList)
                        {
                            if (presence.Name == name)
                            {
                                index = PresenceList.IndexOf(presence);
                            }
                        }
                        if (index >= 0)
                        {
                            PresenceList.RemoveAt(index);
                            p1 = 0;
                            p2 = 1;
                            p3 = 2;
                            UpdatePresence();
                        }
                    }
                    else if (!message_P.Contains('0') && !message_P.Contains('1'))
                    {
                        Presence c = new Presence();
                        c.Name = name;
                        PresenceList.Insert(0, c);
                    }
                    if (PresenceList.Count >= 4)
                    {
                        p1 = 0;
                        p2 = 1;
                        p3 = 2;
                        PresenceName1.Text = PresenceList[0].Name.ToString();
                        PresenceName2.Text = PresenceList[1].Name.ToString();
                        PresenceName3.Text = PresenceList[2].Name.ToString();
                    }
                });
            }
        }

        private void RequestDataBaseReminders()
        {
            RemindersList.Clear();
            client.Publish("/PinIt/Inf/ReminderRequest/TVapp", // topic
                       Encoding.UTF8.GetBytes("")); // message body          
        }

        private void RequestDataBaseAgenda()
        {
            AgendaList.Clear();
            client.Publish("/PinIt/Inf/AgendaRequest/TVapp", // topic
                       Encoding.UTF8.GetBytes("")); // message body
        }

        private void RequestDataBasePresence()
        {
            PresenceList.Clear();
            client.Publish("/PinIt/Inf/PresenceRequest/TVapp", // topic
                       Encoding.UTF8.GetBytes("")); // message body
        }
        //rss method to create and load
        public void LoadRss()
        {
            r.Go(ref Display, "http://feeds.arstechnica.com/arstechnica/index");
        }

        //When the scrollviewer of the RSS is loaded and ready for interaction
        private async void RssFeed_Loaded(object sender, RoutedEventArgs e)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                timer.Tick += (ss, ee) =>
            {

                if (timer.Interval.Ticks == 300)
                {
                    //each time set the offset to scrollviewer.HorizontalOffset + 7
                    RssFeed.ChangeView(RssFeed.HorizontalOffset + 7, null, null, false);

                    //If the row of the feed ended, the feed is reloaded + a little push
                    //Otherwise it will stop 
                    if (RssFeed.HorizontalOffset == RssFeed.ScrollableWidth)
                    {
                        RssFeed.ChangeView(RssFeed.HorizontalOffset + 3.5, null, null, false);
                        LoadRss();
                        RssFeed.ChangeView(RssFeed.HorizontalOffset + 3.5, null, null, false);
                    }
                }
            };
                timer.Interval = new TimeSpan(300);
                timer.Start();
            });
        }

        //shuffle Presence over the board
        public async void UpdatePresence()
        {

            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (PresenceList.Count == 0)
                {
                    PresenceName1.Text = "No teachers available at this moment";
                    Presence1.Visibility = Visibility.Visible;
                    PresencePhoto1.Visibility = Visibility.Visible;
                    PresenceName2.Text = String.Empty;
                    Presence2.Visibility = Visibility.Collapsed;
                    PresencePhoto2.Visibility = Visibility.Collapsed;
                    PresenceName3.Text = String.Empty;
                    Presence3.Visibility = Visibility.Collapsed;
                    PresencePhoto3.Visibility = Visibility.Collapsed;
                    PresenceActive.Text = String.Empty;
                    PresenceRemaining.Visibility = Visibility.Collapsed;
                }
                else if (PresenceList.Count == 1)
                {
                    Presence1.Visibility = Visibility.Visible;
                    PresencePhoto1.Visibility = Visibility.Visible;
                    Presence2.Visibility = Visibility.Collapsed;
                    PresencePhoto2.Visibility = Visibility.Collapsed;
                    Presence3.Visibility = Visibility.Collapsed;
                    PresencePhoto3.Visibility = Visibility.Collapsed;
                    PresenceName1.Text = PresenceList[0].Name.ToString();
                    PresenceName2.Text = String.Empty;
                    PresenceName3.Text = String.Empty;
                    PresenceActive.Text = String.Empty;
                    PresenceRemaining.Visibility = Visibility.Collapsed;
                }
                else if (PresenceList.Count == 2)
                {
                    Presence1.Visibility = Visibility.Visible;
                    PresencePhoto1.Visibility = Visibility.Visible;
                    Presence2.Visibility = Visibility.Visible;
                    PresencePhoto2.Visibility = Visibility.Visible;
                    Presence3.Visibility = Visibility.Collapsed;
                    PresencePhoto3.Visibility = Visibility.Collapsed;
                    PresenceName1.Text = PresenceList[0].Name.ToString();
                    PresenceName2.Text = PresenceList[1].Name.ToString();
                    PresenceName3.Text = String.Empty;
                    PresenceActive.Text = String.Empty;
                    PresenceRemaining.Visibility = Visibility.Collapsed;

                }
                else if (PresenceList.Count == 3)
                {
                    Presence1.Visibility = Visibility.Visible;
                    PresencePhoto1.Visibility = Visibility.Visible;
                    Presence2.Visibility = Visibility.Visible;
                    PresencePhoto2.Visibility = Visibility.Visible;
                    Presence3.Visibility = Visibility.Visible;
                    PresencePhoto3.Visibility = Visibility.Visible;
                    PresenceName1.Text = PresenceList[0].Name.ToString();
                    PresenceName2.Text = PresenceList[1].Name.ToString();
                    PresenceName3.Text = PresenceList[2].Name.ToString();
                    PresenceActive.Text = String.Empty;
                    PresenceRemaining.Visibility = Visibility.Collapsed;
                }
                else if (PresenceList.Count >= 4)
                {
                    Presence1.Visibility = Visibility.Visible;
                    PresencePhoto1.Visibility = Visibility.Visible;
                    Presence2.Visibility = Visibility.Visible;
                    PresencePhoto2.Visibility = Visibility.Visible;
                    Presence3.Visibility = Visibility.Visible;
                    PresencePhoto3.Visibility = Visibility.Visible;
                    PresenceRemaining.Visibility = Visibility.Visible;

                    int limitP = PresenceList.Count;

                    p1 = (p1 + 1) % limitP;
                    p2 = (p2 + 1) % limitP;
                    p3 = (p3 + 1) % limitP;

                    PresenceName1.Text = PresenceList[p1].Name.ToString();
                    PresenceName2.Text = PresenceList[p2].Name.ToString();
                    PresenceName3.Text = PresenceList[p3].Name.ToString();

                    PresenceActive.Text = "+" + (limitP - 3).ToString();
                }
            });
        }

        //Shuffle reminders ovder the board
        public async void UpdateReminders()
        {

            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (RemindersList.Count == 0)
                {
                    Remind1Eclipse.Visibility = Visibility.Collapsed;
                    Remind2Eclipse.Visibility = Visibility.Collapsed;
                    Remind3Eclipse.Visibility = Visibility.Collapsed;

                    ReminderName1.Text = String.Empty;
                    RemindHeader1.Text = String.Empty;
                    RemindText1.Text = String.Empty;

                    ReminderName2.Text = String.Empty;
                    RemindHeader2.Text = String.Empty;
                    RemindText2.Text = String.Empty;

                    ReminderName3.Text = String.Empty;
                    RemindHeader3.Text = String.Empty;
                    RemindText3.Text = String.Empty;
                }
                else if (RemindersList.Count == 1)
                {
                    Remind1Eclipse.Visibility = Visibility.Visible;
                    Remind2Eclipse.Visibility = Visibility.Collapsed;
                    Remind3Eclipse.Visibility = Visibility.Collapsed;
                    ReminderName1.Text = RemindersList[0].Name.ToString();
                    RemindHeader1.Text = RemindersList[0].Title.ToString();
                    RemindText1.Text = RemindersList[0].Message.ToString();

                    ReminderName2.Text = String.Empty;
                    RemindHeader2.Text = String.Empty;
                    RemindText2.Text = String.Empty;

                    ReminderName3.Text = String.Empty;
                    RemindHeader3.Text = String.Empty;
                    RemindText3.Text = String.Empty;
                }
                else if (RemindersList.Count == 2)
                {
                    Remind1Eclipse.Visibility = Visibility.Visible;
                    Remind2Eclipse.Visibility = Visibility.Visible;
                    Remind3Eclipse.Visibility = Visibility.Collapsed;
                    ReminderName1.Text = RemindersList[0].Name.ToString();
                    RemindHeader1.Text = RemindersList[0].Title.ToString();
                    RemindText1.Text = RemindersList[0].Message.ToString();

                    ReminderName2.Text = RemindersList[1].Name.ToString();
                    RemindHeader2.Text = RemindersList[1].Title.ToString();
                    RemindText2.Text = RemindersList[1].Message.ToString();

                    ReminderName3.Text = String.Empty;
                    RemindHeader3.Text = String.Empty;
                    RemindText3.Text = String.Empty;
                }
                else if (RemindersList.Count == 3)
                {
                    Remind1Eclipse.Visibility = Visibility.Visible;
                    Remind2Eclipse.Visibility = Visibility.Visible;
                    Remind3Eclipse.Visibility = Visibility.Visible;
                    ReminderName1.Text = RemindersList[0].Name.ToString();
                    RemindHeader1.Text = RemindersList[0].Title.ToString();
                    RemindText1.Text = RemindersList[0].Message.ToString();

                    ReminderName2.Text = RemindersList[1].Name.ToString();
                    RemindHeader2.Text = RemindersList[1].Title.ToString();
                    RemindText2.Text = RemindersList[1].Message.ToString();

                    ReminderName3.Text = RemindersList[2].Name.ToString();
                    RemindHeader3.Text = RemindersList[2].Title.ToString();
                    RemindText3.Text = RemindersList[2].Message.ToString();
                }
                else if (RemindersList.Count >= 4)
                {
                    Remind1Eclipse.Visibility = Visibility.Visible;
                    Remind2Eclipse.Visibility = Visibility.Visible;
                    Remind3Eclipse.Visibility = Visibility.Visible;
                    int limitR = RemindersList.Count;

                    r1 = (r1 + 1) % limitR;
                    r2 = (r2 + 1) % limitR;
                    r3 = (r3 + 1) % limitR;
                    ReminderName1.Text = RemindersList[r1].Name.ToString();
                    RemindHeader1.Text = RemindersList[r1].Title.ToString();
                    RemindText1.Text = RemindersList[r1].Message.ToString();

                    ReminderName2.Text = RemindersList[r2].Name.ToString();
                    RemindHeader2.Text = RemindersList[r2].Title.ToString();
                    RemindText2.Text = RemindersList[r2].Message.ToString();

                    ReminderName3.Text = RemindersList[r3].Name.ToString();
                    RemindHeader3.Text = RemindersList[r3].Title.ToString();
                    RemindText3.Text = RemindersList[r3].Message.ToString();
                }
            });
        }

        //empty Reminder boxes
        public void EmptyRiminderBoxes()
        {
            Remind1Eclipse.Visibility = Visibility.Collapsed;
            Remind2Eclipse.Visibility = Visibility.Collapsed;
            Remind3Eclipse.Visibility = Visibility.Collapsed;

            ReminderName1.Text = String.Empty;
            RemindHeader1.Text = String.Empty;
            RemindText1.Text = String.Empty;
            ReminderName2.Text = String.Empty;
            RemindHeader2.Text = String.Empty;
            RemindText2.Text = String.Empty;
            ReminderName3.Text = String.Empty;
            RemindHeader3.Text = String.Empty;
            RemindText3.Text = String.Empty;
        }

        //Shuffle Agenda items over the board
        public async void UpdateAgenda()
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (AgendaList.Count == 0)
                {
                    Agenda1Eclipse.Visibility = Visibility.Collapsed;
                    Agenda2Eclipse.Visibility = Visibility.Collapsed;
                    Agenda3Eclipse.Visibility = Visibility.Collapsed;
                    Agenda4Eclipse.Visibility = Visibility.Collapsed;

                    AgendaDate1.Text = String.Empty;
                    AgendaHeader1.Text = String.Empty;
                    AgendaText1.Text = String.Empty;

                    AgendaDate2.Text = String.Empty;
                    AgendaHeader2.Text = String.Empty;
                    AgendaText2.Text = String.Empty;

                    AgendaDate3.Text = String.Empty;
                    AgendaHeader3.Text = String.Empty;
                    AgendaText3.Text = String.Empty;
                }
                else if (AgendaList.Count == 1)
                {
                    Agenda1Eclipse.Visibility = Visibility.Visible;
                    Agenda2Eclipse.Visibility = Visibility.Collapsed;
                    Agenda3Eclipse.Visibility = Visibility.Collapsed;
                    Agenda4Eclipse.Visibility = Visibility.Collapsed;
                    AgendaDate1.Text = AgendaList[0].AgendaDate.ToString();
                    AgendaHeader1.Text = AgendaList[0].Title.ToString();
                    AgendaText1.Text = AgendaList[0].Message.ToString();

                    AgendaDate2.Text = String.Empty;
                    AgendaHeader2.Text = String.Empty;
                    AgendaText2.Text = String.Empty;

                    AgendaDate3.Text = String.Empty;
                    AgendaHeader3.Text = String.Empty;
                    AgendaText3.Text = String.Empty;

                    AgendaDate4.Text = String.Empty;
                    AgendaHeader4.Text = String.Empty;
                    AgendaText4.Text = String.Empty;

                }
                else if (AgendaList.Count == 2)
                {
                    Agenda1Eclipse.Visibility = Visibility.Visible;
                    Agenda2Eclipse.Visibility = Visibility.Visible;
                    Agenda3Eclipse.Visibility = Visibility.Collapsed;
                    Agenda4Eclipse.Visibility = Visibility.Collapsed;

                    AgendaDate1.Text = AgendaList[0].AgendaDate.ToString();
                    AgendaHeader1.Text = AgendaList[0].Title.ToString();
                    AgendaText1.Text = AgendaList[0].Message.ToString();

                    AgendaDate2.Text = AgendaList[1].AgendaDate.ToString();
                    AgendaHeader2.Text = AgendaList[1].Title.ToString();
                    AgendaText2.Text = AgendaList[1].Message.ToString();

                    AgendaDate3.Text = String.Empty;
                    AgendaHeader3.Text = String.Empty;
                    AgendaText3.Text = String.Empty;

                    AgendaDate4.Text = String.Empty;
                    AgendaHeader4.Text = String.Empty;
                    AgendaText4.Text = String.Empty;
                }
                else if (AgendaList.Count == 3)
                {
                    Agenda1Eclipse.Visibility = Visibility.Visible;
                    Agenda2Eclipse.Visibility = Visibility.Visible;
                    Agenda3Eclipse.Visibility = Visibility.Visible;
                    Agenda4Eclipse.Visibility = Visibility.Collapsed;
                    AgendaDate1.Text = AgendaList[0].AgendaDate.ToString();
                    AgendaHeader1.Text = AgendaList[0].Title.ToString();
                    AgendaText1.Text = AgendaList[0].Message.ToString();

                    AgendaDate2.Text = AgendaList[1].AgendaDate.ToString();
                    AgendaHeader2.Text = AgendaList[1].Title.ToString();
                    AgendaText2.Text = AgendaList[1].Message.ToString();

                    AgendaDate3.Text = AgendaList[2].AgendaDate.ToString();
                    AgendaHeader3.Text = AgendaList[2].Title.ToString();
                    AgendaText3.Text = AgendaList[2].Message.ToString();

                    AgendaDate4.Text = String.Empty;
                    AgendaHeader4.Text = String.Empty;
                    AgendaText4.Text = String.Empty;
                }
                else if (AgendaList.Count == 4)
                {
                    Agenda1Eclipse.Visibility = Visibility.Visible;
                    Agenda2Eclipse.Visibility = Visibility.Visible;
                    Agenda3Eclipse.Visibility = Visibility.Visible;
                    Agenda4Eclipse.Visibility = Visibility.Visible;
                    AgendaDate1.Text = AgendaList[0].AgendaDate.ToString();
                    AgendaHeader1.Text = AgendaList[0].Title.ToString();
                    AgendaText1.Text = AgendaList[0].Message.ToString();

                    AgendaDate2.Text = AgendaList[1].AgendaDate.ToString();
                    AgendaHeader2.Text = AgendaList[1].Title.ToString();
                    AgendaText2.Text = AgendaList[1].Message.ToString();

                    AgendaDate3.Text = AgendaList[2].AgendaDate.ToString();
                    AgendaHeader3.Text = AgendaList[2].Title.ToString();
                    AgendaText3.Text = AgendaList[2].Message.ToString();

                    AgendaDate4.Text = AgendaList[3].AgendaDate.ToString();
                    AgendaHeader4.Text = AgendaList[3].Title.ToString();
                    AgendaText4.Text = AgendaList[3].Message.ToString();
                }

                else if (AgendaList.Count >= 5)
                {
                    Agenda1Eclipse.Visibility = Visibility.Visible;
                    Agenda2Eclipse.Visibility = Visibility.Visible;
                    Agenda3Eclipse.Visibility = Visibility.Visible;
                    Agenda4Eclipse.Visibility = Visibility.Visible;
                    int limitA = AgendaList.Count;

                    a1 = (a1 + 1) % limitA;
                    a2 = (a2 + 1) % limitA;
                    a3 = (a3 + 1) % limitA;
                    a4 = (a4 + 1) % limitA;

                    AgendaDate1.Text = AgendaList[a1].AgendaDate.ToString();
                    AgendaHeader1.Text = AgendaList[a1].Title.ToString();
                    AgendaText1.Text = AgendaList[a1].Message.ToString();

                    AgendaDate2.Text = AgendaList[a2].AgendaDate.ToString();
                    AgendaHeader2.Text = AgendaList[a2].Title.ToString();
                    AgendaText2.Text = AgendaList[a2].Message.ToString();

                    AgendaDate3.Text = AgendaList[a3].AgendaDate.ToString();
                    AgendaHeader3.Text = AgendaList[a3].Title.ToString();
                    AgendaText3.Text = AgendaList[a3].Message.ToString();

                    AgendaDate4.Text = AgendaList[a4].AgendaDate.ToString();
                    AgendaHeader4.Text = AgendaList[a4].Title.ToString();
                    AgendaText4.Text = AgendaList[a4].Message.ToString();
                }
            });
        }

        //empty Agenda boxes
        public void EmptyAgendaBoxes()
        {
            Agenda1Eclipse.Visibility = Visibility.Collapsed;
            Agenda2Eclipse.Visibility = Visibility.Collapsed;
            Agenda3Eclipse.Visibility = Visibility.Collapsed;
            Agenda4Eclipse.Visibility = Visibility.Collapsed;
            AgendaDate1.Text = String.Empty;
            AgendaHeader1.Text = String.Empty;
            AgendaText1.Text = String.Empty;

            AgendaDate2.Text = String.Empty;
            AgendaHeader2.Text = String.Empty;
            AgendaText2.Text = String.Empty;

            AgendaDate3.Text = String.Empty;
            AgendaHeader3.Text = String.Empty;
            AgendaText3.Text = String.Empty;

            AgendaDate4.Text = String.Empty;
            AgendaHeader4.Text = String.Empty;
            AgendaText4.Text = String.Empty;
        }
    }
}
