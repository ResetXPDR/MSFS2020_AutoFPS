using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;

namespace Installer
{
    public enum AutoStart
    {
        REMOVE = -1,
        NONE = 0,
        FSUIPC,
        EXE
    }

    public static class InstallerFunctions
    {
        public static bool GetProcessRunning(string name)
        {
            Process proc = Process.GetProcessesByName(name).FirstOrDefault();
            return proc != null && proc.ProcessName == name;
        }

        #region Install Actions
        public static bool AutoStartFsuipc(bool removeEntry = false)
        {
            bool result = false;
            string programParam = "READY";
            if (CheckFSUIPC("7.4.0"))
                programParam = "CONNECTED";

            try
            {
                string regPath = (string)Registry.GetValue(Parameters.ipcRegPath, Parameters.ipcRegInstallDirValue, null);
                if (!string.IsNullOrEmpty(regPath))
                    regPath += "\\" + "FSUIPC7.ini";
                else
                    return false;

                if (File.Exists(regPath))
                {
                    string fileContent = File.ReadAllText(regPath, Encoding.Default);
                    if (!fileContent.Contains("[Programs]") && !removeEntry)
                    {
                        fileContent += $"\r\n[Programs]\r\nRunIf1={programParam},CLOSE,{Parameters.binPath}";
                        File.WriteAllText(regPath, fileContent, Encoding.Default);
                        result = true;
                    }
                    else
                    {
                        RegexOptions regOptions = RegexOptions.Compiled | RegexOptions.Multiline;
                        var runMatches = Regex.Matches(fileContent, @"[;]{0,1}Run(\d+).*", regOptions);
                        int lastRun = 0;
                        if (runMatches.Count > 0 && runMatches[runMatches.Count - 1].Groups.Count == 2)
                            lastRun = Convert.ToInt32(runMatches[runMatches.Count - 1].Groups[1].Value);

                        var runIfMatches = Regex.Matches(fileContent, @"[;]{0,1}RunIf(\d+).*", regOptions);
                        int lastRunIf = 0;
                        if (runIfMatches.Count > 0 && runIfMatches[runIfMatches.Count - 1].Groups.Count == 2)
                            lastRunIf = Convert.ToInt32(runIfMatches[runIfMatches.Count - 1].Groups[1].Value);

                        if (Regex.IsMatch(fileContent, @"^[;]{0,1}Run(\d+).*" + Parameters.appName + "\\.exe", regOptions))
                        {
                            if (!removeEntry)
                                fileContent = Regex.Replace(fileContent, @"^[;]{0,1}Run(\d+).*" + Parameters.appName + "\\.exe", $"RunIf{lastRunIf + 1}={programParam},CLOSE,{Parameters.binPath}", regOptions);
                            else
                                fileContent = Regex.Replace(fileContent, @"^[;]{0,1}Run(\d+).*" + Parameters.appName + "\\.exe", $"", regOptions);
                            File.WriteAllText(regPath, fileContent, Encoding.Default);
                            result = true;
                        }
                        else if (Regex.IsMatch(fileContent, @"^[;]{0,1}RunIf(\d+).*" + Parameters.appName + "\\.exe", regOptions))
                        {
                            if (!removeEntry)
                                fileContent = Regex.Replace(fileContent, @"^[;]{0,1}RunIf(\d+).*" + Parameters.appName + "\\.exe", $"RunIf$1={programParam},CLOSE,{Parameters.binPath}", regOptions);
                            else
                                fileContent = Regex.Replace(fileContent, @"^[;]{0,1}RunIf(\d+).*" + Parameters.appName + "\\.exe", $"", regOptions);
                            File.WriteAllText(regPath, fileContent, Encoding.Default);
                            result = true;
                        }
                        else
                        {
                            int index = -1;
                            if (runIfMatches.Count > 0 && runMatches.Count > 0)
                            {
                                index = runIfMatches[runIfMatches.Count - 1].Index + runIfMatches[runIfMatches.Count - 1].Length;
                                if (runMatches[runMatches.Count - 1].Index > runIfMatches[runIfMatches.Count - 1].Index)
                                    index = runMatches[runMatches.Count - 1].Index + runMatches[runMatches.Count - 1].Length;
                            }
                            else if (runIfMatches.Count > 0)
                                index = runIfMatches[runIfMatches.Count - 1].Index + runIfMatches[runIfMatches.Count - 1].Length;
                            else if (runMatches.Count > 0)
                                index = runMatches[runMatches.Count - 1].Index + runMatches[runMatches.Count - 1].Length;

                            if (index > 0 && !removeEntry)
                            {
                                fileContent = fileContent.Insert(index + 1, $"RunIf{lastRunIf + 1}={programParam},CLOSE,{Parameters.binPath}\r\n");
                                File.WriteAllText(regPath, fileContent, Encoding.Default);
                                result = true;
                            }
                            else if (!removeEntry)
                            {
                                fileContent = Regex.Replace(fileContent, @"^\[Programs\]\r\n", $"[Programs]\r\nRunIf{lastRunIf + 1}={programParam},CLOSE,{Parameters.binPath}\r\n", regOptions);
                                File.WriteAllText(regPath, fileContent, Encoding.Default);
                                result = true;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during AutoStartFsuipc", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return result;
        }

        public static bool AutoStartExe(bool removeEntry = false)
        {
            bool result = false;

            try
            {
                string path = Parameters.msExeSteam;
                if (!File.Exists(path))
                    path = Parameters.msExeStore;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(File.ReadAllText(path));

                bool found = false;
                XmlNode simbase = xmlDoc.ChildNodes[1];
                List<XmlNode> removeList = new List<XmlNode>();
                foreach (XmlNode outerNode in simbase.ChildNodes)
                {
                    if (outerNode.Name == "Launch.Addon" && outerNode.InnerText.Contains(Parameters.appBinary))
                    {
                        found = true;

                        if (!removeEntry)
                        {
                            foreach (XmlNode innerNode in outerNode.ChildNodes)
                            {
                                if (innerNode.Name == "Disabled")
                                    innerNode.InnerText = "False";
                                else if (innerNode.Name == "Path")
                                    innerNode.InnerText = Parameters.binPath;
                                else if (innerNode.Name == "CommandLine")
                                    innerNode.InnerText = "";
                                else if (innerNode.Name == "ManualLoad")
                                    innerNode.InnerText = "False";
                            }
                        }
                        else
                            removeList.Add(outerNode);
                    }
                }
                foreach (XmlNode node in removeList)
                    xmlDoc.ChildNodes[1].RemoveChild(node);

                if (!found && !removeEntry)
                {
                    XmlNode outerNode = xmlDoc.CreateElement("Launch.Addon");

                    XmlNode innerNode = xmlDoc.CreateElement("Disabled");
                    innerNode.InnerText = "False";
                    outerNode.AppendChild(innerNode);

                    innerNode = xmlDoc.CreateElement("ManualLoad");
                    innerNode.InnerText = "False";
                    outerNode.AppendChild(innerNode);

                    innerNode = xmlDoc.CreateElement("Name");
                    innerNode.InnerText = Parameters.appName;
                    outerNode.AppendChild(innerNode);

                    innerNode = xmlDoc.CreateElement("Path");
                    innerNode.InnerText = Parameters.binPath;
                    outerNode.AppendChild(innerNode);

                    xmlDoc.ChildNodes[1].AppendChild(outerNode);
                }

                xmlDoc.Save(path);
                result = true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during AutoStartExe", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return result;
        }

        public static bool PlaceDesktopLink()
        {
            bool result = false;
            try
            {
                IShellLink link = (IShellLink)new ShellLink();

                link.SetDescription("Start " + Parameters.appName);
                link.SetPath(Parameters.binPath);

                IPersistFile file = (IPersistFile)link;
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                file.Save(Path.Combine(desktopPath, $"{Parameters.appName}.lnk"), false);
                result = true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during PlaceDesktopLink", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return result;
        }

        public static bool DeleteOldFiles()
        {
            try
            {
                if (!Directory.Exists(Parameters.binDir))
                    return true;

                Directory.Delete(Parameters.binDir, true);
                Directory.CreateDirectory(Parameters.binDir);

                return (new DirectoryInfo(Parameters.binDir)).GetFiles().Length == 0;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during RemoveOldFiles", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static bool ExtractZip(string extractDir = null, string zipFile = null)
        {
            try
            {
                if (zipFile == null)
                {
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Installer.{Parameters.fileName}"))
                    {
                        ZipArchive archive = new ZipArchive(stream);
                        archive.ExtractToDirectory(Parameters.binDir);
                        stream.Close();
                    }

                    RunCommand($"powershell -WindowStyle Hidden -Command \"dir -Path {Parameters.binDir} -Recurse | Unblock-File\"");
                }
                else
                {
                    using (Stream stream = new FileStream(zipFile, FileMode.Open))
                    {
                        ZipArchive archive = new ZipArchive(stream);
                        archive.ExtractToDirectory(extractDir);
                        stream.Close();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during ExtractZip", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static bool InstallWasm()
        {
            bool result = false;
            try
            {


            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during InstallWasm", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return result;
        }

        public static bool DownloadFile(string url, string file)
        {
            bool result = false;
            try
            {
                var webClient = new WebClient();
                webClient.DownloadFile(url, file);
                result = File.Exists(file);

            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during DownloadFile", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return result;
        }
        #endregion

        #region Check Requirements
        public static bool CheckFSUIPC(string version = null)
        {
            bool result = false;
            string ipcVersion = Parameters.ipcVersion;
            if (!string.IsNullOrEmpty(version))
                ipcVersion = version;

            try
            {
                string regVersion = (string)Registry.GetValue(Parameters.ipcRegPath, Parameters.ipcRegValue, null);
                if (!string.IsNullOrWhiteSpace(regVersion))
                {
                    regVersion = regVersion.Substring(1);
                    int index = regVersion.IndexOf("(beta)");
                    if (index > 0)
                        regVersion = regVersion.Substring(0, index).TrimEnd();
                    result = CheckVersion(regVersion, ipcVersion, true, false);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during CheckFSUIPC", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return result;
        }
        public static bool CheckVersion(string versionInstalled, string versionRequired, bool majorEqual, bool ignoreBuild)
        {
            bool majorMatch = false;
            bool minorMatch = false;
            bool patchMatch = false;

            string[] strInst = versionInstalled.Split('.');
            string[] strReq = versionRequired.Split('.');
            int vInst;
            int vReq;
            bool prevWasEqual = false;

            for (int i = 0; i < strInst.Length; i++)
            {
                if (Regex.IsMatch(strInst[i], @"(\d+)\D"))
                    strInst[i] = strInst[i].Substring(0, strInst[i].Length - 1);
            }

            //Major
            if (int.TryParse(strInst[0], out vInst) && int.TryParse(strReq[0], out vReq))
            {
                if (majorEqual)
                    majorMatch = vInst == vReq;
                else
                    majorMatch = vInst >= vReq;

                prevWasEqual = vInst == vReq;
            }

            //Minor
            if (int.TryParse(strInst[1], out vInst) && int.TryParse(strReq[1], out vReq))
            {
                if (prevWasEqual)
                    minorMatch = vInst >= vReq;
                else
                    minorMatch = true;

                prevWasEqual = vInst == vReq;
            }

            //Patch
            if (!ignoreBuild)
            {
                if (int.TryParse(strInst[2], out vInst) && int.TryParse(strReq[2], out vReq))
                {
                    if (prevWasEqual)
                        patchMatch = vInst >= vReq;
                    else
                        patchMatch = true;
                }
            }
            else
                patchMatch = true;

            return majorMatch && minorMatch && patchMatch;
        }

        public static bool CheckPackageVersion(string packagePath, string packageName, string version)
        {
            try
            {
                string file = packagePath + "\\" + packageName + "\\manifest.json";
                if (File.Exists(file))
                {
                    string[] lines = File.ReadAllLines(file);
                    foreach (string line in lines)
                    {
                        if (Parameters.wasmRegex.IsMatch(line))
                        {
                            var matches = Parameters.wasmRegex.Matches(line);
                            if (matches.Count == 1 && matches[0].Groups.Count >= 2)
                                return CheckVersion(matches[0].Groups[1].Value, version, false, false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during CheckPackageVersion", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }

        public static string FindPackagePath(string confFile)
        {
            string[] lines = File.ReadAllLines(confFile);
            foreach (string line in lines)
            {
                if (line.StartsWith(Parameters.msStringPackage))
                {
                    return line.Replace("\"", "").Substring(Parameters.msStringPackage.Length) + "\\Community";
                }
            }

            return "";
        }

        public static bool CheckInstalledMSFS(out string packagePath)
        {
            try
            {
                if (File.Exists(Parameters.msConfigStore))
                {
                    packagePath = FindPackagePath(Parameters.msConfigStore);
                    return !string.IsNullOrWhiteSpace(packagePath) && Directory.Exists(packagePath);
                }
                else if (File.Exists(Parameters.msConfigSteam))
                {
                    packagePath = FindPackagePath(Parameters.msConfigSteam);
                    return !string.IsNullOrWhiteSpace(packagePath) && Directory.Exists(packagePath);
                }

                packagePath = "";
                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during CheckInstalledMSFS", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            packagePath = "";
            return false;
        }

        public static string RunCommand(string command)
        {
            var pProcess = new Process();
            pProcess.StartInfo.FileName = "cmd.exe";
            pProcess.StartInfo.Arguments = "/C" + command;
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.Start();
            string strOutput = pProcess.StandardOutput.ReadToEnd();
            pProcess.WaitForExit();

            return strOutput ?? "";
        }

        public static bool StringGreaterEqual(string input, int compare)
        {
            if (int.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out int numA) && numA >= compare)
                return true;
            else
                return false;
        }

        public static bool StringEqual(string input, int compare)
        {
            if (int.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out int numA) && numA == compare)
                return true;
            else
                return false;
        }

        public static bool StringGreater(string input, int compare)
        {
            if (int.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out int numA) && numA > compare)
                return true;
            else
                return false;
        }

        public static bool CheckDotNet()
        {
            try
            {
                bool installedDesktop = false;

                string output = RunCommand("dotnet --list-runtimes");

                var matches = Parameters.netDesktop.Matches(output);
                foreach (Match match in matches)
                {
                    if (!match.Success || match.Groups.Count != 5)
                        continue;
                    if (!StringEqual(match.Groups[2].Value, Parameters.netMajor))
                        continue;
                    if ((StringEqual(match.Groups[3].Value, Parameters.netMinor) && StringGreaterEqual(match.Groups[4].Value, Parameters.netPatch))
                        || StringGreater(match.Groups[3].Value, Parameters.netMinor))
                        installedDesktop = true;
                }

                return installedDesktop;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during CheckDotNet", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        #endregion
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLink
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}
