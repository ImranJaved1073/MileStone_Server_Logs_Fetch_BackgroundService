using MIP_SDK_Tray_Manager.ModelClasses;
using NAudio.CoreAudioApi;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoOS.Platform.Login;
using VideoOS.Platform.SDK.Platform;
using log4net;
using System.Configuration;

namespace MIP_SDK_Tray_Manager
{
    public partial class Logs : Form
    {
        // DLL Import to allocate a console
        private RichTextBox logTextBox;

        private static readonly ILog Log = LogManager.GetLogger("RollingFileAppender");
        private static readonly ILog Status = LogManager.GetLogger("RollingFileAppenderStatus");
        private static readonly Guid IntegrationId = new Guid(ConfigurationManager.AppSettings["IntegrationId"]);
        //private const string IntegrationName = "Log Read";
        //private const string Version = "1.0";
        //private const string ManufacturerName = "Sample Manufacturer";
        private static readonly string IntegrationName = ConfigurationManager.AppSettings["IntegrationName"];
        private static readonly string Version = ConfigurationManager.AppSettings["Version"];
        private static readonly string ManufacturerName = ConfigurationManager.AppSettings["ManufacturerName"];
        private static readonly string secretKey = ConfigurationManager.AppSettings["JwtSecretKey"];

        private static bool Connected = false;
        enum Authorizationmodes
        {
            DefaultWindows,
            Windows,
            Basic
        };

        //static string _url = "http://ec2-3-111-16-24.ap-south-1.compute.amazonaws.com/"; // Replace with your actual server address
        static string _url = ConfigurationManager.AppSettings["ApiBaseUrl"];
        static Authorizationmodes _auth = Authorizationmodes.Basic; // Set your preferred authentication mode (Basic, Windows, DefaultWindows)
        //static string _user = "apiuser"; // Replace with your actual username
        //static SecureString _securePwd = ConvertToSecureString("EyeDash@2024"); // Replace with your actual password
        static string _user = ConfigurationManager.AppSettings["Username"];
        static SecureString _securePwd = ConvertToSecureString(ConfigurationManager.AppSettings["Password"]);
        static bool _secureOnly = false; // change to true if you need to enforce secure communication

        private HttpRequestListener httpListener = new HttpRequestListener(ConfigurationManager.AppSettings["httpListener"], secretKey); // Change port as needed
        private static DateTime beginTime;
        private static DateTime endTime;

        public Logs()
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
        }


        private void AppendLog(string message)
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

        public void MainLogic()
        {
            // Initialize VideoOS SDK Environment
            VideoOS.Platform.SDK.Environment.Initialize();
            VideoOS.Platform.SDK.Log.Environment.Initialize();
            VideoOS.Platform.SDK.UI.Environment.Initialize();

            // Secure password logic here
            _securePwd.MakeReadOnly();

            // Start HTTP Listener

            Log.Info("HTTP Listener initialized.");



            // Subscribe to the RequestReceived event
            httpListener.RequestReceived += async (requestBody) =>
            {
                try
                {
                    Log.Info($"Received request body: {requestBody}");
                    AppendLog($"Received request body: {requestBody}");
                    DateParser(requestBody);

                    int maxRetries = 5;
                    int retryDelay = 5000;
                    int attempt = 0;
                    bool isConnected = false;

                    while (attempt < maxRetries && !isConnected)
                    {
                        attempt++;
                        Log.Info($"Attempting to connect (Attempt {attempt}/{maxRetries})...");
                        AppendLog($"Attempting to connect (Attempt {attempt}/{maxRetries})...");

                        if (LoginUsingCredentials())
                        {
                            Log.Info("Connected to the server.");
                            AppendLog("Connected to the server.");
                            isConnected = true;

                            var logsJson = await ReadAndUploadLogs(); // This returns a JSON string
                            AppendLog("Logs processed successfully.");
                            Log.Info("Logs processed successfully.");

                            // Send logs back in the HTTP response
                            httpListener.SendResponse(logsJson); // Send response without passing context
                            return;
                        }
                        else
                        {
                            Log.Warn("Failed to connect. Retrying in 5 seconds...");
                            AppendLog("Failed to connect. Retrying in 5 seconds...");
                            await Task.Delay(retryDelay);
                        }
                    }

                    if (!isConnected)
                    {
                        Log.Error("Exceeded maximum retry attempts. Unable to connect.");
                        AppendLog("Exceeded maximum retry attempts. Unable to connect.");
                        httpListener.SendError("Unable to connect to the server."); // Send error without passing context
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"An error occurred while processing the request: {ex.Message}", ex);
                    AppendLog($"An error occurred while processing the request: {ex.Message}");
                    httpListener.SendError($"An error occurred: {ex.Message}"); // Send error without passing context
                }
            };

            // Start listening in a separate task
            Task.Run(() =>
            {
                Log.Info("Starting HTTP Listener...");
                AppendLog("Starting HTTP Listener...");
                httpListener.Start();
                Log.Info("HTTP Listener is running.");
                AppendLog("HTTP Listener is running.");
            });



            // Now, handle the status reading logic in parallel

            Task.Run(() =>
            {
                // Secure password logic here
                _securePwd.MakeReadOnly();

                // Wait until the first API request is received
                Status.Info("Waiting for incoming API request...");
                AppendLog("Waiting for incoming API request...");

                // Automatic login retry logic
                int maxRetries = 5; // Maximum number of retries
                int retryDelay = 5000; // Delay between retries in milliseconds (5 seconds)
                int attempt = 0;
                bool isConnected = false;

                while (attempt < maxRetries && !isConnected)
                {
                    attempt++;
                    Status.Info($"Attempting to connect (Attempt {attempt}/{maxRetries})...");
                    AppendLog($"Attempting to connect (Attempt {attempt}/{maxRetries})...");

                    if (LoginUsingCredentials())
                    {
                        Status.Info("Connected to the server.");
                        AppendLog("Connected to the server.");
                        isConnected = true;
                        StatusLogic statusLogic = new StatusLogic();
                        //statusLogic.Show();
                        statusLogic.Initialize();
                    }
                    else
                    {
                        Status.Warn("Failed to connect. Retrying in 5 seconds...");
                        AppendLog("Failed to connect. Retrying in 5 seconds...");
                        Task.Delay(retryDelay); // Wait before retrying
                    }
                }

                if (!isConnected)
                {
                    Status.Warn("Exceeded maximum retry attempts. Unable to connect.");
                    AppendLog("Exceeded maximum retry attempts. Unable to connect.");
                }
            });


            Log.Info("Application is running. Waiting for requests...");
            AppendLog("Application is running. Waiting for requests...");

        }



        private void DateParser(string requestBody)
        {
            Log.Info("Request received..");
            AppendLog("Request received in Program.cs:");

            try
            {
                // Parse the JSON directly
                var json = JObject.Parse(requestBody);

                // Access "beginTime" and "endTime" and parse them as DateTime
                beginTime = DateTime.Parse(json["beginTime"].ToString());
                endTime = DateTime.Parse(json["endTime"].ToString());

                Log.Info($"Parsed beginTime: {beginTime}");
                Log.Info($"Parsed endTime: {endTime}");
                AppendLog($"Parsed beginTime: {beginTime}");
                AppendLog($"Parsed endTime: {endTime}");
            }
            catch (JsonException ex)
            {
                Log.Error($"Failed to parse JSON: {ex.Message}");
                AppendLog($"Failed to parse JSON: {ex.Message}");
            }
            catch (FormatException ex)
            {
                Log.Error($"Date format error: {ex.Message}");
                AppendLog($"Date format error: {ex.Message}");
            }
        }




        /// <summary>
        /// Converts a regular string password to a SecureString
        /// </summary>
        public static SecureString ConvertToSecureString(string password)
        {
            SecureString secureString = new SecureString();
            foreach (char c in password)
            {
                secureString.AppendChar(c);
            }
            return secureString;
        }

        /// <summary>
        /// Login routine using hardcoded credentials
        /// </summary>
        /// <returns></returns> True if successfully logged in
        static public bool LoginUsingCredentials()
        {
            Uri uri = new UriBuilder(_url).Uri;
            CredentialCache cc = new CredentialCache();

            switch (_auth)
            {
                case Authorizationmodes.DefaultWindows:
                    cc = Util.BuildCredentialCache(uri, "", "", "Negotiate");
                    break;
                case Authorizationmodes.Windows:
                    cc = Util.BuildCredentialCache(uri, _user, _securePwd, "Negotiate");
                    break;
                case Authorizationmodes.Basic:
                    cc = Util.BuildCredentialCache(uri, _user, _securePwd, "Basic");
                    break;
            }

            VideoOS.Platform.SDK.Environment.AddServer(_secureOnly, uri, cc);

            try
            {
                VideoOS.Platform.SDK.Environment.Login(uri, IntegrationId, IntegrationName, Version, ManufacturerName);
            }
            catch (ServerNotFoundMIPException snfe)
            {
                //Console.WriteLine("Server not found: " + snfe.Message);
                VideoOS.Platform.SDK.Environment.RemoveServer(uri);
                return false;
            }
            catch (InvalidCredentialsMIPException ice)
            {
                // Console.WriteLine("Invalid credentials for: " + ice.Message);
                VideoOS.Platform.SDK.Environment.RemoveServer(uri);
                return false;
            }
            catch (Exception)
            {
                // Console.WriteLine("Internal error connecting to: " + uri.DnsSafeHost);
                VideoOS.Platform.SDK.Environment.RemoveServer(uri);
                return false;
            }

            LoginSettings loginSettings = LoginSettingsCache.GetLoginSettings(uri.DnsSafeHost);
            if (loginSettings == null)
            {
                // Console.WriteLine($"Login not succeeded for user: {_user} on server: {uri.DnsSafeHost}.");
                VideoOS.Platform.SDK.Environment.RemoveServer(uri);
                return false;
            }

            // Console.WriteLine($"Login succeeded for user: {_user} on server: {loginSettings.Uri}.");
            return true;
        }

        private static void SetLoginResult(bool connected)
        {
            Connected = connected;
        }

        private async Task<string> ReadAndUploadLogs()
        {
            List<SystemLogEntry> SystemLogs = new List<SystemLogEntry>();
            List<AuditLogEntry> AuditLogs = new List<AuditLogEntry>();
            List<RuleLogEntry> RuleLogs = new List<RuleLogEntry>();

            bool isInitialized = VideoOS.Platform.Log.LogClient.Instance.Initialized;
            var groups = VideoOS.Platform.Log.LogClient.Instance.ReadGroups(VideoOS.Platform.EnvironmentManager.Instance.MasterSite.ServerId);

            //Console.WriteLine("Groups:");
            Log.Info("Groups:");
            AppendLog("Groups:");
            foreach (string group in groups)
            {
                //Console.WriteLine($"Group: {group}");
                Log.Info($"Group: {group}");
                AppendLog($"Group: {group}");
                try
                {
                    for (int page = 1; ; page++)
                    {
                        var logData = await UploadLogData(group, page);

                        if (logData == null || logData.Count == 0)
                        {
                            break;
                        }

                        // Map ArrayList to specific log types
                        foreach (ArrayList log in logData)
                        {
                            if (log.Count > 7) // Ensure there are enough elements
                            {
                                if (group == "System")
                                {
                                    var logEntry = new SystemLogEntry
                                    {
                                        Number = ToInt(log[0]),
                                        LogLevel = log[1].ToString(),
                                        LocalTime = ToDate(log[2]),
                                        MessageText = log[3].ToString(),
                                        Category = log[4].ToString(),
                                        SourceType = log[5].ToString(),
                                        SourceName = log[6].ToString(),
                                        EventType = log[7].ToString(),
                                        Group = group
                                    };
                                    SystemLogs.Add(logEntry);
                                }
                                else if (group == "Audit")
                                {
                                    var logEntry = new AuditLogEntry
                                    {
                                        Number = ToInt(log[0]),
                                        LocalTime = ToDate(log[1]),
                                        MessageText = log[2].ToString(),
                                        Permission = log[3].ToString(),
                                        Category = log[4].ToString(),
                                        SourceType = log[5].ToString(),
                                        SourceName = log[6].ToString(),
                                        User = log[7].ToString(),
                                        UserLocation = log[7].ToString(),
                                        Group = group
                                    };
                                    AuditLogs.Add(logEntry);
                                }
                                else if (group == "Rules")
                                {
                                    var logEntry = new RuleLogEntry
                                    {
                                        Number = ToInt(log[0]),
                                        LocalTime = ToDate(log[1]),
                                        MessageText = log[2].ToString(),
                                        Category = log[3].ToString(),
                                        SourceType = log[4].ToString(),
                                        SourceName = log[5].ToString(),
                                        EventType = log[6].ToString(),
                                        RuleName = log[7].ToString(),
                                        ServiceName = log[7].ToString(),
                                        Group = group
                                    };
                                    RuleLogs.Add(logEntry);
                                }
                            }
                        }

                        //// Pause for 5 seconds before fetching more data
                        //await Task.Delay(5000);
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"Error processing group {group}: {ex.Message}");
                    Log.Error($"Error processing group {group}: {ex.Message}");
                    AppendLog($"Error processing group {group}: {ex.Message}");
                }
            }

            //Console.WriteLine($"Total System logs: {SystemLogs.Count}, Audit logs: {AuditLogs.Count}, Rule logs: {RuleLogs.Count}");
            AppendLog($"Total System logs: {SystemLogs.Count}, Audit logs: {AuditLogs.Count}, Rule logs: {RuleLogs.Count}");
            Log.Info($"Total System logs: {SystemLogs.Count}, Audit logs: {AuditLogs.Count}, Rule logs: {RuleLogs.Count}");
            // Combine all logs into a single JSON array
            var combinedLogs = new List<object> { SystemLogs, AuditLogs, RuleLogs };
            return JsonConvert.SerializeObject(combinedLogs);
        }


        private async Task<ArrayList> UploadLogData(string group, int page)
        {
            ArrayList result;
            ArrayList names;


            //Console.WriteLine($"beginTime: {beginTime}, endTime: {endTime}, group: {group}");
            Log.Info($"beginTime: {beginTime}, endTime: {endTime}, group: {group}");
            AppendLog($"beginTime: {beginTime}, endTime: {endTime}, group: {group}");

            // Fetch log data
            VideoOS.Platform.Log.LogClient.Instance.ReadLog(VideoOS.Platform.EnvironmentManager.Instance.MasterSite.ServerId, page, out result, out names, group, beginTime, endTime);

            if (result == null || result.Count == 0)
            {
                //Console.WriteLine("Data finished");
                Log.Info("Data finished");
                AppendLog("Data finished");
                return null; // No more log data
            }





            // Add "Number" to the beginning of the names array
            names.Insert(0, "Number");
            // Display the fetched data in the console (optional)
            foreach (ArrayList entry in result)
            {
                for (int i = 0; i < names.Count; i++)
                {
                    string key = names[i].ToString();
                    object value = entry[i];
                    Log.Info($"{key}: {value}");
                    AppendLog($"{key}: {value}");
                }
                Log.Info("--------------------------------------");
                AppendLog("--------------------------------------");
            }

            // Call the asynchronous upload method
            // Console.WriteLine($"Logs Count Result: {result.Count}, Logs Count Names: {names.Count}");
            //  await MongoUploader.Upload(result, names, group);

            return result; // Return the fetched results for further processing if needed
        }

        public static int ToInt(object value)
        {
            if (value == null)
                return 0;  // Return a default value

            if (int.TryParse(value.ToString(), out int result))
                return result;

            return 0;  // Return a default value if parsing fails
        }

        public static DateTime ToDate(object value, string format = "MM/dd/yyyy HH:mm:ss")
        {
            if (value == null)
                return DateTime.MinValue;  // Return default DateTime value if null

            if (DateTime.TryParseExact(value.ToString(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                return result;

            return DateTime.MinValue;  // Return default DateTime value if parsing fails
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            this.Dispose();
            base.OnFormClosing(e);
            //stop the listener
            //httpListener.Stop();
        }
    }
}
