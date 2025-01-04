using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using VideoOS.Platform.SDK.UI.LoginDialog;
using System.Collections;
using System.Runtime.InteropServices; // Import this for DllImport
using System.Security;
using System.Net;
using VideoOS.Platform.Login;
using VideoOS.Platform.SDK.Platform;
using System.ServiceProcess;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Globalization;
using System.IO;
using log4net;
using System.Collections.ObjectModel;
using System.Timers;
using VideoOS.Platform.Data;
using VideoOS.Platform.Messaging;
using VideoOS.Platform;
using System.Configuration;
using MIP_SDK_Tray_Manager.ModelClasses;
using Microsoft.IdentityModel.Tokens;

namespace MIP_SDK_Tray_Manager
{
    public partial class MIPSDK_TrayManager : Form
    {
        private NotifyIcon trayIcon = null;
        private BackgroundWorker backgroundWorker = null;
        private bool serviceRunning = false;
        static Logs logs = new Logs();
        ContextMenuStrip contextMenu = new ContextMenuStrip();
        public MIPSDK_TrayManager()
        {
            InitializeComponent();
            InitializeTrayIcon();
            InitializeBackgroundService();
            Hide(); // Hide the main form on startup
        }


        private new void Hide()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
        }

        private new void Show()
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private void InitializeTrayIcon()
        {
            // Create the tray icon
            trayIcon = new NotifyIcon
            {
                Icon = new Icon("icon_running.ico"), // Replace with your .ico file
                Text = "Background Service App",
                Visible = true
            };

            // Create context menu

            contextMenu.Items.Add("Start Service", null, StartService);
            contextMenu.Items.Add("Stop Service", null, StopService);
            contextMenu.Items.Add("View Logs", null, ViewLogs);
            contextMenu.Items.Add("View Status", null, ViewStatus);
            contextMenu.Items.Add("Exit", null, ExitApplication);
            if (Application.OpenForms.OfType<StatusLogic>().Count() > 0)
            {
                contextMenu.Items[3].Enabled = false;
            }
            else
            {
                contextMenu.Items[3].Enabled = true;
            }
            trayIcon.ContextMenuStrip = contextMenu;
            contextMenu.Items[1].Enabled = false;
        }

        private void InitializeBackgroundService()
        {
            // Set up the background worker
            backgroundWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_Completed;
        }

        private void StartService(object sender, EventArgs e)
        {
            if (Environment.UserInteractive)
            {
                // Run the service in the background

                if (logs.InvokeRequired)
                {
                    logs.Invoke(new Action(() => logs.Show()));
                }
                else
                {
                    logs.Show();
                }
                logs.MainLogic();
                if (!serviceRunning)
                {
                    Hide(); // Hide the form when the service starts
                    serviceRunning = true;
                    trayIcon.Icon = new Icon("icon_running.ico");
                    trayIcon.Text = "Service Running...";
                    backgroundWorker.RunWorkerAsync();
                    contextMenu.Items[0].Enabled = false;
                    contextMenu.Items[1].Enabled = true;
                }

            }
            else
            {
                // Run the service as a Windows Service
                ServiceBase.Run(new TrayManagerService());
            }


        }

        private void StopService(object sender, EventArgs e)
        {
            if (serviceRunning)
            {
                serviceRunning = false;
                trayIcon.Icon = new Icon("icon_running.ico");
                trayIcon.Text = "Service Stopped.";
                backgroundWorker.CancelAsync();
                if (logs.InvokeRequired)
                {
                    logs.Invoke(new Action(() => logs.Close()));
                }
                else
                {
                    logs.Close();
                }
                contextMenu.Items[0].Enabled = true;
                contextMenu.Items[1].Enabled = false;
            }
            // Close and dispose all objects, then reopen the form
            this.Dispose();
            if (Application.OpenForms.OfType<StatusLogic>() != null)
            {
                Application.OpenForms.OfType<StatusLogic>().First().Dispose();
            }
            // Reinitialize and reopen the form
            Application.Restart(); // Restart the application
        }

        private void ViewLogs(object sender, EventArgs e)
        {
            // Open the log folder
            string logFolderPath = "C:\\Logs";
            Process.Start("explorer.exe", logFolderPath);
        }

        private void ViewStatus(object sender, EventArgs e)
        {
            // check if view status is already open then disable the button
            if (Application.OpenForms.OfType<StatusLogic>().Count() > 0)
            {
                contextMenu.Items[3].Enabled = false;
            }
            else
            {
                contextMenu.Items[3].Enabled = true;
            }
            StatusLogic statusLogic = new StatusLogic();
            statusLogic.FormClosed += (s, args) => contextMenu.Items[3].Enabled = true;
            statusLogic.Show();
            contextMenu.Items[3].Enabled = false;
            string statusFolder = "C:\\Status";
            Process.Start("explorer.exe", statusFolder);
        }

        private void ExitApplication(object sender, EventArgs e)
        {
            // Cleanup and exit
            trayIcon.Visible = false;
            if (serviceRunning)
            {
                backgroundWorker.CancelAsync();
            }
            this.Dispose();
            logs.Close();
            Application.Exit();
            //appliccation should also be removed from the task manager

        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Simulate service work
            while (!backgroundWorker.CancellationPending)
            {
                // Replace with Milestone SDK or other background logic
                System.Threading.Thread.Sleep(1000);

            }
        }

        private void BackgroundWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            trayIcon.Text = "Service Stopped.";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Prevent form from closing; minimize to tray instead
            e.Cancel = true;
            Hide();
        }
    }
}
