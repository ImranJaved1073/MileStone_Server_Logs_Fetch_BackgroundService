using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using static MIP_SDK_Tray_Manager.MIPSDK_TrayManager;
using VideoOS.Platform.Data;
using VideoOS.Platform.Messaging;
using VideoOS.Platform;
using System.Security;
using System.Configuration;
using System.Text.Json;
using MIP_SDK_Tray_Manager.ModelClasses;

namespace MIP_SDK_Tray_Manager
{
    public partial class StatusLogic : Form
    {
        // Import the AllocConsole function from kernel32.dll
        //[DllImport("kernel32.dll", SetLastError = true)]
        //public static extern bool AllocConsole();

        private static bool Connected = false;
        enum Authorizationmodes
        {
            DefaultWindows,
            Windows,
            Basic
        };


        private static RichTextBox logTextBox;
        private static readonly ILog Status = LogManager.GetLogger("RollingFileAppenderStatus");
        private static List<object> _registrations = new List<object>();
        private static MessageCommunication _messageCommunication;
        private static System.Timers.Timer _serverConnectedTimer = new System.Timers.Timer();
        private static bool _serverConnected = false;

        public StatusLogic()
        {

            logTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                WordWrap = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            this.Controls.Add(logTextBox);
            this.Text = "Log Viewer";
            this.Size = new System.Drawing.Size(800, 600);
            InitializeComponent();

            //Task.Run(() =>
            //{
            //    VideoOS.Platform.SDK.Environment.Initialize();
            //    VideoOS.Platform.SDK.Log.Environment.Initialize();
            //    VideoOS.Platform.SDK.UI.Environment.Initialize();

            //    // Secure password logic here
            //    _securePwd.MakeReadOnly();

            //    // Wait until the first API request is received
            //    Status.Info("Waiting for incoming API request...");
            //    AppendLog("Waiting for incoming API request...");

            //    // Automatic login retry logic
            //    int maxRetries = 5; // Maximum number of retries
            //    int retryDelay = 5000; // Delay between retries in milliseconds (5 seconds)
            //    int attempt = 0;
            //    bool isConnected = false;

            //    while (attempt < maxRetries && !isConnected)
            //    {
            //        attempt++;
            //        Status.Info($"Attempting to connect (Attempt {attempt}/{maxRetries})...");
            //        AppendLog($"Attempting to connect (Attempt {attempt}/{maxRetries})...");

            //        if (LoginUsingCredentials())
            //        {
            //            Status.Info("Connected to the server.");
            //            AppendLog("Connected to the server.");
            //            isConnected = true;
            //            Initialize();
            //        }
            //        else
            //        {
            //            Status.Warn("Failed to connect. Retrying in 5 seconds...");
            //            AppendLog("Failed to connect. Retrying in 5 seconds...");
            //            System.Threading.Thread.Sleep(retryDelay); // Wait before retrying
            //        }
            //    }

            //    if (!isConnected)
            //    {
            //        Status.Warn("Exceeded maximum retry attempts. Unable to connect.");
            //        AppendLog("Exceeded maximum retry attempts. Unable to connect.");
            //    }
            //});

        }

        private static void AppendLog(string message)
        {
            // Ensure the call is on the UI thread
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() => AppendLog(message)));
            }
            else
            {
                logTextBox.AppendText($"{DateTime.Now}: {message}{Environment.NewLine}");
                logTextBox.ScrollToCaret();
            }
        }

        public void Initialize()
        {
            // Allocate a console window for the application
            //AllocConsole();

            // Initialize the VideoOS Platform SDK
            VideoOS.Platform.SDK.Environment.Initialize();
            VideoOS.Platform.SDK.UI.Environment.Initialize(); // UI initialization is still needed for underlying functionality

            Status.Info("Connecting to server...");
            AppendLog("Connecting to server...");

            // Start communication and subscribe to messages
            StartCommunication();

            // Set up a timer to check server connectivity
            _serverConnectedTimer.Elapsed += _timer_Elapsed;
            _serverConnectedTimer.AutoReset = true;
            _serverConnectedTimer.Interval = 5000;
            _serverConnectedTimer.Start();
        }

        public static void Run()
        {
            // Clean up before exiting
            StopCommunication();
        }

        private static void StartCommunication()
        {
            try
            {
                AppendLog("Starting communication setup...");
                Status.Info("Starting communication setup...");

                // Start the MessageCommunicationManager
                AppendLog("Initializing MessageCommunicationManager...");
                Status.Info("Initializing MessageCommunicationManager...");
                MessageCommunicationManager.Start(EnvironmentManager.Instance.MasterSite.ServerId);
                _messageCommunication = MessageCommunicationManager.Get(EnvironmentManager.Instance.MasterSite.ServerId);

                if (_messageCommunication == null)
                {
                    AppendLog("Error: MessageCommunication instance is null.");
                    Status.Error("MessageCommunication instance is null.");
                    return;
                }

                // Register real-time event handlers
                _registrations.Add(_messageCommunication.RegisterCommunicationFilter(MessageHandler,
                    new CommunicationIdFilter(MessageId.Server.NewEventIndication)));

                _registrations.Add(_messageCommunication.RegisterCommunicationFilter(MessageHandler,
                    new CommunicationIdFilter(MessageId.System.SystemConfigurationChangedIndication)));

                _registrations.Add(_messageCommunication.RegisterCommunicationFilter(MessageHandler,
                    new CommunicationIdFilter(MessageId.System.SystemConfigurationChangedDetailsIndication)));

                // Register handler for the ProvideCurrentStateResponse
                _registrations.Add(_messageCommunication.RegisterCommunicationFilter(ProvideCurrentStateResponseHandler,
                    new CommunicationIdFilter(MessageCommunication.ProvideCurrentStateResponse)));

                // Log state before connectingk
                _messageCommunication.ConnectionStateChangedEvent += _messageCommunication_ConnectionStateChangedEvent;



                // Attempt to send the ProvideCurrentStateRequest
                AppendLog("Attempting to transmit ProvideCurrentStateRequest...");
                Status.Info("Attempting to transmit ProvideCurrentStateRequest...");
                try
                {
                    _messageCommunication.TransmitMessage(
                        new VideoOS.Platform.Messaging.Message(MessageCommunication.ProvideCurrentStateRequest), null, null, null);
                    AppendLog("ProvideCurrentStateRequest transmitted successfully.");
                    Status.Info("ProvideCurrentStateRequest transmitted successfully.");
                }
                catch (Exception ex)
                {
                    AppendLog("Error while transmitting ProvideCurrentStateRequest: " + ex.Message);
                    AppendLog("Stack Trace: " + ex.StackTrace);
                    Status.Error("Error while transmitting ProvideCurrentStateRequest: " + ex.Message);
                    Status.Error("Stack Trace: " + ex.StackTrace);
                }

            }
            catch (Exception ex)
            {
                AppendLog("Unexpected error in StartCommunication(): " + ex.Message);
                AppendLog("Stack Trace: " + ex.StackTrace);
                Status.Error("Unexpected error in StartCommunication(): " + ex.Message);
                Status.Error("Stack Trace: " + ex.StackTrace);
            }
        }

        private static void StopCommunication()
        {
            foreach (var registration in _registrations)
            {
                _messageCommunication.UnRegisterCommunicationFilter(registration);
            }
            _serverConnectedTimer.Stop();
            VideoOS.Platform.SDK.Environment.RemoveAllServers();
        }

        private static void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var connected = VideoOS.Platform.SDK.Environment.IsServerConnected(EnvironmentManager.Instance.CurrentSite.ServerId.Uri);
            if (connected != _serverConnected)
            {
                _serverConnected = connected;
                AppendLog($"Server Connection State: {(_serverConnected ? "Connected" : "Not Responding")}");
                Status.Info($"Server Connection State: {(_serverConnected ? "Connected" : "Not Responding")}");
            }
            //StopCommunication();
        }

        private static void _messageCommunication_ConnectionStateChangedEvent(object sender, EventArgs e)
        {
            AppendLog($"Connection state changed: {(_messageCommunication.IsConnected ? "Connected" : "Disconnected")}");
            Status.Info($"Connection state changed: {(_messageCommunication.IsConnected ? "Connected" : "Disconnected")}");
            if (_messageCommunication.IsConnected)
            {
                try
                {
                    _messageCommunication.TransmitMessage(
                        new VideoOS.Platform.Messaging.Message(MessageCommunication.ProvideCurrentStateRequest), null, null, null);
                }
                catch (MIPException)
                {
                    AppendLog("Warning: Unable to connect to EventServer's MessageCommunication service. Retrying...");
                    Status.Warn("Unable to connect to EventServer's MessageCommunication service. Retrying...");
                }
            }
        }

        private static async Task<object> ProvideCurrentStateResponseHandler(VideoOS.Platform.Messaging.Message message, FQID dest, FQID source)
        {
            AppendLog("ProvideCurrentStateResponseHandler invoked.");
            AppendLog($"Message ID: {message.MessageId}");
            AppendLog($"Destination FQID: {dest}");
            AppendLog($"Source FQID: {source}");

            Status.Info("ProvideCurrentStateResponseHandler invoked.");
            Status.Info($"Message ID: {message.MessageId}");
            Status.Info($"Destination FQID: {dest}");
            Status.Info($"Source FQID: {source}");

            var result = message.Data as Collection<ItemState>;
            var dataInfo = "";

            List<EventStatus> eventStatuses = new List<EventStatus>();

            if (result != null)
            {
                AppendLog($"Number of ItemStates received: {result.Count}");
                Status.Info($"Number of ItemStates received: {result.Count}");

                foreach (ItemState itemState in result)
                {
                    AppendLog($"Processing ItemState with FQID: {itemState.FQID.ToString()}");
                    Status.Info($"Processing ItemState with FQID: {itemState.FQID.ToString()}");

                    string name = string.Empty;
                    try
                    {
                        var configItem = VideoOS.Platform.ConfigurationItems.Factory.GetConfigurationItem(itemState.FQID);
                        if (configItem != null)
                        {
                            name = configItem.Name;
                            AppendLog($"Config item name retrieved: {name}");
                            Status.Info($"Config item name retrieved: {name}");
                        }
                        else
                        {
                            AppendLog("Config item is null.");
                            Status.Info("Config item is null.");
                        }

                        string kindName = VideoOS.Platform.ConfigurationItems.Factory.GetItemTypeFromKind(itemState.FQID.Kind);
                        AppendLog($"Kind name retrieved: {kindName}");
                        Status.Info($"Kind name retrieved: {kindName}");

                        dataInfo = $" - Name={name}, Kind={kindName}, State={itemState.State}\n";
                        AppendLog(DateTime.Now.ToLongTimeString() + ": " + dataInfo);
                        Status.Info(DateTime.Now.ToLongTimeString() + ": " + dataInfo);


                        AppendLog("ItemState: " + itemState);
                        Status.Info("ItemState: " + itemState);


                        var fqidRepresentation = new FQIDRepresentation
                        {
                            ServerId = new ServerInfo
                            {
                                Uri = itemState.FQID.ServerId?.Uri.ToString(),
                                IsExportType = itemState.FQID.ServerId?.IsExportType ?? false
                            },
                            ParentId = itemState.FQID?.ParentId.ToString(),
                            ObjectId = itemState.FQID?.ObjectId.ToString(),
                            ObjectIdString = itemState?.FQID.ObjectIdString,
                            Kind = itemState.FQID?.Kind.ToString(),
                        };

                        // Storing the dataInfo in a list
                        eventStatuses.Add(new EventStatus
                        {
                            Timestamp = DateTime.Now.ToLongTimeString(),
                            Name = name,
                            Kind = kindName,
                            State = itemState?.State,
                            FQID = fqidRepresentation
                        });

                    }
                    catch (Exception ex)
                    {
                        AppendLog($"Error processing ItemState with FQID {itemState.FQID}: {ex.Message}");
                        Status.Error($"Error processing ItemState with FQID {itemState.FQID}: {ex.Message}");
                    }
                }
            }
            else
            {
                AppendLog("Message data is null or not of type Collection<ItemState>.");
                Status.Info("Message data is null or not of type Collection<ItemState>.");
            }

            // Send eventStatuses to the API
            //string apiUrl = "http://3.6.137.123:9201/api/status"; // Replace with your target IP and endpoint
            //string apiUrl = "https://0ef1-2400-adc5-188-5f00-1477-99cb-3ac6-64f4.ngrok-free.app/api/status";
            string apiUrl = ConfigurationManager.AppSettings["apiUrl"];
            using (HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(1) })
            {
                try
                {
                    var jsonPayload = JsonSerializer.Serialize(eventStatuses);
                    //Status.Info(jsonPayload.ToString());
                    //Status.Info(JsonSerializer.Serialize(eventStatuses));
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        AppendLog("Event statuses successfully sent to the API.");
                        Status.Info("Event statuses successfully sent to the API.");
                    }
                    else
                    {
                        AppendLog($"Failed to send event statuses. Status Code: {response.StatusCode}");
                        Status.Error($"Failed to send event statuses. Status Code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"Error while sending event statuses to API: {ex.Message}");
                    Status.Error($"Error while sending event statuses to API: {ex.Message}");
                }
            }

            return null;
        }



        private static object MessageHandler(VideoOS.Platform.Messaging.Message message, FQID dest, FQID source)
        {
            AppendLog($"MessageHandler triggered. MessageId: {message.MessageId}");
            Status.Info($"MessageHandler triggered. MessageId: {message.MessageId}");

            if (message.MessageId == MessageId.Server.NewEventIndication)
            {
                EventData eventData = message.Data as EventData;
                AppendLog($"New Event Received: {eventData.EventHeader.Message}");
                Status.Info($"New Event Received: {eventData.EventHeader.Message}");
            }
            else if (message.MessageId == MessageId.System.SystemConfigurationChangedIndication)
            {
                AppendLog("System Configuration Changed.");
                Status.Info("System Configuration Changed.");
            }
            else if (message.MessageId == VideoOS.Platform.Messaging.MessageId.System.SystemConfigurationChangedDetailsIndication)
            {
                AppendLog("Detailed System Configuration Change detected.");
                Status.Info("Detailed System Configuration Change detected.");
                var data = message.Data as SystemConfigurationChangedDetailsIndicationData;
                if (data != null)
                {
                    foreach (var detail in data.DetailsList)
                    {
                        AppendLog($"ChangeType: {detail.ChangeType}, Id: {detail.FQID.ObjectId}");
                        Status.Info($"ChangeType: {detail.ChangeType}, Id: {detail.FQID.ObjectId}");
                    }

                    // Send the statuses of all devices again to the API
                    _messageCommunication.TransmitMessage(
                        new VideoOS.Platform.Messaging.Message(MessageCommunication.ProvideCurrentStateRequest), null, null, null);

                    AppendLog("ProvideCurrentStateRequest sent after SystemConfigurationChangedDetailsIndication.");
                    Status.Info("ProvideCurrentStateRequest sent after SystemConfigurationChangedDetailsIndication.");
                }
                else
                {
                    AppendLog("Details data is null.");
                    Status.Info("Details data is null.");
                }
            }
            else
            {
                AppendLog($"Unhandled MessageId: {message.MessageId}");
                Status.Warn($"Unhandled MessageId: {message.MessageId}");
            }

            return null;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _serverConnectedTimer.Stop();
            logTextBox.Dispose();
            this.Controls.Remove(logTextBox);
            this.Dispose();
            base.OnFormClosing(e);
        }
    }
}
