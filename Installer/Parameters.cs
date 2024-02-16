using System;
using System.Text.RegularExpressions;

namespace Installer
{
    public static class Parameters
    {
        public static readonly string fileName = "AppPackage.zip";

        public static readonly string appName = "MSFS2020_AutoFPS";
        public static readonly string appBinary = $"{appName}.exe";
        public static readonly string appDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MSFS2020_AutoFPS";
        public static readonly string binDir = appDir + @"\bin";
        public static readonly string binPath = binDir + @"\MSFS2020_AutoFPS.exe";
        public static readonly string confFile = appDir + @"\MSFS2020_AutoFPS.config";

        public static readonly Regex netDesktop = new Regex(@"Microsoft.WindowsDesktop.App ((\d+)\.(\d+)\.(\d+)).+", RegexOptions.Compiled);

        public static readonly int netMajor = 7;
        public static readonly int netMinor = 0;
        public static readonly int netPatch = 14;
        public static readonly string netVersion = $"{netMajor}.{netMinor}.{netPatch}";
        public static readonly string netUrl = "https://download.visualstudio.microsoft.com/download/pr/8f5b0079-2bb4-49cd-874e-0f58703eff6e/7010b5f213a2c436a307eb385dbb16ff/windowsdesktop-runtime-7.0.14-win-x64.exe";
        public static readonly string netUrlFile = "windowsdesktop-runtime-7.0.14-win-x64.exe";

        public static readonly string ipcRegPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\FSUIPC7";
        public static readonly string ipcRegInstallDirValue = "InstallDir";
        public static readonly string ipcRegValue = "DisplayVersion";
        public static readonly string ipcVersion = "7.4.0";

        public static readonly Regex wasmRegex = new Regex("^\\s*\"package_version\":\\s*\"([0-9\\.]+)\"\\s*,\\s*$", RegexOptions.Compiled);
        public static readonly string wasmMobiName = "mobiflight-event-module";
        public static readonly string wasmMobiVersion = "0.7.1";
        public static readonly string wasmUrl = "https://github.com/MobiFlight/MobiFlight-WASM-Module/releases/download/0.7.1/mobiflight-event-module-0.7.1.zip";
        public static readonly string wasmUrlFile = "mobiflight-event-module-0.7.1.zip";

        public static readonly string msConfigStore = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\UserCfg.opt";
        public static readonly string msConfigSteam = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft Flight Simulator\UserCfg.opt";
        public static readonly string msStringPackage = "InstalledPackagesPath ";
        public static readonly string msExeStore = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\EXE.xml";
        public static readonly string msExeSteam = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft Flight Simulator\EXE.xml";
    }
}
