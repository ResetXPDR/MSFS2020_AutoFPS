﻿using H.NotifyIcon;
using Serilog;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
namespace MSFS2020_AutoFPS
{
    public partial class App : Application
    {
        private ServiceModel Model;
        private ServiceController Controller;
        protected int Interval = 1000;

        private TaskbarIcon notifyIcon;

        public static new App Current => Application.Current as App;
        public static string ConfigFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MSFS2020_AutoFPS\MSFS2020_AutoFPS.config";
        public static string AppDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MSFS2020_AutoFPS\bin";
        public static string MSFSDefaultsFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MSFS2020_AutoFPS\MSFSDefaults.config";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (Process.GetProcessesByName("MSFS2020_AutoFPS").Length > 1)
            {
                MessageBox.Show("MSFS2020_AutoFPS is already running!", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            if (Process.GetProcessesByName("DynamicLOD_ResetEdition").Length > 0)
            {
                MessageBox.Show("DynamicLOD_ResetEdition is already running!", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            if (Process.GetProcessesByName("DynamicLOD").Length > 0)
            {
                MessageBox.Show("A pre-ResetEdition version of DynamicLOD is already running!", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            if (Process.GetProcessesByName("SmoothFlight").Length > 0)
            {
                MessageBox.Show("SmoothFlight is already running!", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }
            Directory.SetCurrentDirectory(AppDir);

            if (!File.Exists(ConfigFile))
            {
                string ConfigFileDefault = Directory.GetCurrentDirectory() + @"\MSFS2020_AutoFPS.config";
                if (!File.Exists(ConfigFileDefault))
                {
                    MessageBox.Show("No Configuration File found! Closing ...", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                    return;
                }
                else
                {
                    File.Copy(ConfigFileDefault, ConfigFile);
                }
            }

            Model = new();
            InitLog();
            InitSystray();

            Controller = new(Model);
            Task.Run(Controller.Run);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += OnTick;
            timer.Start();

            MainWindow = new MainWindow(notifyIcon.DataContext as NotifyIconViewModel, Model);
            if (Model.OpenWindow && !Model.StartMinimized)
                MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (Model != null)
            {
                Model.CancellationRequested = true;
                Thread.Sleep(Interval); // Ensure Runtick finishes its last MSFS settings changes
                if (Model.DefaultSettingsRead && Model.IsSessionRunning)
                {
                    Logger.Log(LogLevel.Information, "App:OnExit", $"Resetting LODs to {Model.DefaultTLOD} / {Model.DefaultOLOD} and VR {Model.DefaultTLOD_VR} / {Model.DefaultOLOD_VR}");
                    Model.MemoryAccess.SetTLOD_PC(Model.DefaultTLOD);
                    Model.MemoryAccess.SetTLOD_VR(Model.DefaultTLOD_VR);
                    Model.MemoryAccess.SetOLOD_PC(Model.DefaultOLOD);
                    Model.MemoryAccess.SetOLOD_VR(Model.DefaultOLOD_VR);
                    Logger.Log(LogLevel.Information, "App:OnExit", $"Resetting cloud quality to {Model.CloudQualityText(Model.DefaultCloudQ)} / VR {Model.CloudQualityText(Model.DefaultCloudQ_VR)}");
                    Model.MemoryAccess.SetCloudQ(Model.DefaultCloudQ);
                    Model.MemoryAccess.SetCloudQ_VR(Model.DefaultCloudQ_VR);
                    if (Model.MemoryAccess.GetTLOD_PC() == Model.DefaultTLOD) // As long as one setting restoration stuck
                    {
                        Model.ConfigurationFile.RemoveSetting("defaultTLOD");
                        Model.ConfigurationFile.RemoveSetting("defaultTLOD_VR");
                        Model.ConfigurationFile.RemoveSetting("defaultOLOD");
                        Model.ConfigurationFile.RemoveSetting("defaultOLOD_VR");
                        Model.ConfigurationFile.RemoveSetting("defaultCloudQ");
                        Model.ConfigurationFile.RemoveSetting("defaultCloudQ_VR");
                    }
                }
            }
            notifyIcon?.Dispose();
            base.OnExit(e);

            Logger.Log(LogLevel.Information, "App:OnExit", "MSFS2020_AutoFPS exiting ...");
        }

        protected void OnTick(object sender, EventArgs e)
        {
            if (Model.ServiceExited)
            {
                Current.Shutdown();
            }
        }

        protected void InitLog()
        {
            string logFilePath = @"..\log\" + Model.GetSetting("logFilePath", "MSFS2020_AutoFPS.log");
            string logLevel = Model.GetSetting("logLevel", "Debug"); ;
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration().WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3,
                                                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message} {NewLine}{Exception}");
            if (logLevel == "Warning")
                loggerConfiguration.MinimumLevel.Warning();
            else if (logLevel == "Debug")
                loggerConfiguration.MinimumLevel.Debug();
            else if (logLevel == "Verbose")
                loggerConfiguration.MinimumLevel.Verbose();
            else
                loggerConfiguration.MinimumLevel.Information();
            Log.Logger = loggerConfiguration.CreateLogger();
            Log.Information($"-----------------------------------------------------------------------");
            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            assemblyVersion = assemblyVersion[0..assemblyVersion.LastIndexOf('.')];
            Logger.Log(LogLevel.Information, "App:InitLog", $"MSFS2020_AutoFPS v{assemblyVersion} started! Log Level: {logLevel} Log File: {logFilePath}");
        }

        protected void InitSystray()
        {
            Logger.Log(LogLevel.Information, "App:InitSystray", $"Creating SysTray Icon ...");
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            notifyIcon.Icon = GetIcon("icon.ico");
            notifyIcon.ForceCreate(false);
        }

        public static Icon GetIcon(string filename)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"MSFS2020_AutoFPS.{filename}");
            return new Icon(stream);
        }
    }
}
