using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Installer
{
    public class InstallerWorker
    {
        private Queue<string> messageList = null;

        public bool IsRunning { get; private set; } = false;
        public bool HasError { get; private set; } = false;
        private int WasmErrorCount = 0;

        public bool CfgDesktopLink { get; set; } = false;
        public bool Migrate2020Config { get; set; } = false;
        public bool appInstalled { get; set; } = false;
        public bool app2020Installed { get; set; } = false;
        public AutoStart CfgAutoStart { get; set; } = AutoStart.NONE;

        public InstallerWorker(Queue<string> messageList)
        {
            this.messageList = messageList;
            if (Directory.Exists(Parameters.appDir)) appInstalled = true;
            if (Directory.Exists(Parameters.appDir2020)) app2020Installed = true;
        }

        public void Run()
        {
            IsRunning = true;

            InstallDotNet();
            if (!HasError && File.Exists(Parameters.msConfigStore) || File.Exists(Parameters.msConfigSteam))
                InstallWasm();
            if (!HasError && File.Exists(Parameters.msConfigStore2024) || File.Exists(Parameters.msConfigSteam2024))
                InstallWasm(true);
            if (!HasError)
                InstallApp();
            if (!HasError)
                SetupAutoStart();
            
            messageList.Enqueue("\nDone.");
            if (!HasError)
                messageList.Enqueue($"MSFS_AutoFPS was installed to {Parameters.appDir}");
            IsRunning = false;
        }

        protected void InstallDotNet()
        {
            messageList.Enqueue("\nChecking .NET 7 Desktop Runtime ...");

            if (InstallerFunctions.CheckDotNet())
                messageList.Enqueue("Runtime already installed!");
            else
            {
                messageList.Enqueue("Runtime not installed or outdated!");
                messageList.Enqueue("Downloading .NET Desktop Runtime ...");
                if (!InstallerFunctions.DownloadFile(Parameters.netUrl, Parameters.netUrlFile))
                {
                    HasError = true;
                    messageList.Enqueue("Could not download .NET Runtime!");
                    return;
                }
                messageList.Enqueue("Installing .NET Desktop Runtime ...");
                InstallerFunctions.RunCommand($"{Parameters.netUrlFile} /install /quiet /norestart");
                File.Delete(Parameters.netUrlFile);
            }
        }

        protected void InstallWasm(bool isMSFS2024 = false)
        {
            messageList.Enqueue("\nChecking MobiFlight WASM/Event Module for " + (isMSFS2024 ? "MSFS2024" : "MSFS2020") + "...");

            if (!InstallerFunctions.CheckInstalledMSFS(isMSFS2024, out string packagePath))
            {
                MessageBox.Show("Could not determine Community folder location for " + (isMSFS2024 ? "MSFS2024" : "MSFS2020") + ". App may not work correctly with " + (isMSFS2024 ? "MSFS2024" : "MSFS2020") + "!", "Unable to install Wasm!", MessageBoxButton.OK, MessageBoxImage.Warning);
                messageList.Enqueue("Could not determine Community folder location for " + (isMSFS2024 ? "MSFS2024" : "MSFS2020") + "!");
                if (++WasmErrorCount == 2)
                {
                    messageList.Enqueue("\nUnable to install Wasm for either MSFS2020 or MSFS2024. \nCheck correct installation of at least one of these MSFS versions!");
                    HasError = true;
                }
                return;
            }


            if (InstallerFunctions.CheckPackageVersion(packagePath, Parameters.wasmMobiName, Parameters.wasmMobiVersion))
            {
                messageList.Enqueue("Module already installed!");
            }
            else
            {
                if (!InstallerFunctions.GetProcessRunning(isMSFS2024 ? "FlightSimulator2024" : "FlightSimulator"))
                {
                    messageList.Enqueue("Module not installed or outdated!");
                    if (Directory.Exists(packagePath + @"\" + Parameters.wasmMobiName))
                    {
                        messageList.Enqueue("Deleting old Version ...");
                        Directory.Delete(packagePath + @"\" + Parameters.wasmMobiName, true);
                    }
                    messageList.Enqueue("Downloading MobiFlight Module ...");
                    if (!InstallerFunctions.DownloadFile(Parameters.wasmUrl, Parameters.wasmUrlFile))
                    {
                        HasError = true;
                        messageList.Enqueue("Could not download MobiFlight Module!");
                        return;
                    }
                    messageList.Enqueue("Extracting new Version ...");
                    if (!InstallerFunctions.ExtractZip(packagePath, Parameters.wasmUrlFile))
                    {
                        HasError = true;
                        messageList.Enqueue("Error while extracting MobiFlight Module!");
                        return;
                    }
                    File.Delete(Parameters.wasmUrlFile);
                }
                else
                {
                    HasError = true;
                    messageList.Enqueue("Can not install/update Module while MSFS is running.");
                    MessageBox.Show("Please stop MSFS and try again.", "MSFS is running!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        protected void InstallApp()
        {
            messageList.Enqueue("\nChecking Application State ...");

            // Old - install new, copy old 2020 config, uninstall old
            // Old & new - update new, give option to copy old 2020 config, uninstall old
            // New - update new
            // Nothing - install new


            if (!appInstalled)
            {
                messageList.Enqueue("Installing MSFS_AutoFPS ...");
                messageList.Enqueue("Extracting Application ...");
                if (!InstallerFunctions.ExtractZip())
                {
                    HasError = true;
                    messageList.Enqueue("Error while extracting Application!");
                    return;
                }
            }
            else 
            {
                messageList.Enqueue("Deleting old MSFS_AutoFPS Version ...");
                if (Directory.Exists(Parameters.binDir))
                    Directory.Delete(Parameters.binDir, true);
                Directory.CreateDirectory(Parameters.binDir);
                messageList.Enqueue("Extracting new MSFS_AutoFPS Version ...");
                if (!InstallerFunctions.ExtractZip())
                {
                    HasError = true;
                    messageList.Enqueue("Error while extracting Application!");
                    return;
                }
            }
            if (app2020Installed)
            {
                if (Migrate2020Config && File.Exists(Parameters.confFile2020)) File.Copy(Parameters.confFile2020, Parameters.confFile, true);
                messageList.Enqueue("Deleting old MSFS2020_AutoFPS Version ...");
                if (Directory.Exists(Parameters.appDir2020))
                    Directory.Delete(Parameters.appDir2020, true);
                InstallerFunctions.AutoStartExe2020(true);
                InstallerFunctions.AutoStartFsuipc(false, true);
                InstallerFunctions.RemoveDesktopLink(Parameters.appName2020);
            }

            if (!File.Exists(Parameters.confFile))
            {
                messageList.Enqueue("Creating Config File ...");
                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Installer.MSFS2020_AutoFPS.config"))
                {
                    using (var file = new FileStream(Parameters.confFile, FileMode.Create, FileAccess.Write))
                    {
                        resource.CopyTo(file);
                    }
                }
            }

            if (CfgDesktopLink)
            {
                messageList.Enqueue("Placing Shortcut ...");
                InstallerFunctions.PlaceDesktopLink();
            }
        }

        protected void SetupAutoStart()
        {
            if (CfgAutoStart == AutoStart.NONE)
                return;

            if (CfgAutoStart == AutoStart.FSUIPC)
            {
                messageList.Enqueue("Check/Remove MSFS Auto-Start ...");
                InstallerFunctions.AutoStartExe(true);
                messageList.Enqueue("Setup FSUIPC Auto-Start ...");
                if (InstallerFunctions.AutoStartFsuipc(true))
                    messageList.Enqueue("Auto-Start added successfully!");
                else
                {
                    messageList.Enqueue("Failed to add Auto-Start!");
                    HasError = true;
                }
            }

            if (CfgAutoStart == AutoStart.EXE)
            {
                messageList.Enqueue("Check/Remove FSUIPC Auto-Start ...");
                InstallerFunctions.AutoStartFsuipc(true, true);
                messageList.Enqueue("Setup EXE.xml Auto-Start ...");
                if (InstallerFunctions.AutoStartExe())
                    messageList.Enqueue("Auto-Start added successfully!");
                else
                {
                    messageList.Enqueue("Failed to add Auto-Start!");
                    HasError = true;
                }
            }

            if (CfgAutoStart == AutoStart.REMOVE)
            {
                messageList.Enqueue("Check/Remove FSUIPC Auto-Start ...");
                InstallerFunctions.AutoStartFsuipc(true, true);
                messageList.Enqueue("Check/Remove MSFS Auto-Start ...");
                InstallerFunctions.AutoStartExe(true);
            }
        }
    }
}
