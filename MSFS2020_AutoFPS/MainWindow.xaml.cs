using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Threading;
using static System.Net.WebRequestMethods;

namespace MSFS2020_AutoFPS
{
    public partial class MainWindow : Window
    {
        protected NotifyIconViewModel notifyModel;
        protected ServiceModel serviceModel;
        protected DispatcherTimer timer;

        private int logTimer = 0;
        private int logTimerInterval = 8;

        public MainWindow(NotifyIconViewModel notifyModel, ServiceModel serviceModel)
        {
            InitializeComponent();
            this.notifyModel = notifyModel;
            this.serviceModel = serviceModel;

             string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            assemblyVersion = assemblyVersion[0..assemblyVersion.LastIndexOf('.')];
            Title += " (" + assemblyVersion + (ServiceModel.TestVersion ? "-test" : "")+ ")";

            if (serviceModel.UseExpertOptions) stkpnlMSFSSettings.Visibility = Visibility.Visible;
            else stkpnlMSFSSettings.Visibility = Visibility.Collapsed;
            if (ServiceModel.TestVersion) chkTestLogSimValues.Visibility = Visibility.Visible;
            else chkTestLogSimValues.Visibility = Visibility.Hidden;
 
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += OnTick;

            string latestAppVersionStr = GetFinalRedirect("https://github.com/ResetXPDR/MSFS2020_AutoFPS/releases/latest");
            lblappUrl.Visibility = Visibility.Hidden;
            if (int.TryParse(assemblyVersion.Replace(".", ""), CultureInfo.InvariantCulture, out int currentAppVersion) &&  latestAppVersionStr != null && latestAppVersionStr.Length > 50)
            { 
                latestAppVersionStr = latestAppVersionStr.Substring(latestAppVersionStr.Length - 5, 5);
                if (int.TryParse(latestAppVersionStr.Replace(".", ""), CultureInfo.InvariantCulture, out int LatestAppVersion))
                { 
                    if ((ServiceModel.TestVersion && LatestAppVersion >= currentAppVersion) || LatestAppVersion > currentAppVersion)
                    {
                        lblStatusMessage.Content = "Newer app version " + (latestAppVersionStr) + " now available";
                        lblStatusMessage.Foreground = new SolidColorBrush(Colors.Green);
                        lblappUrl.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        lblStatusMessage.Content = "Latest app version is installed";
                        lblStatusMessage.Foreground = new SolidColorBrush(Colors.Green);
                    }
                }   
            }
            if (ServiceModel.TestVersion)
            {
                lblStatusMessage.Content = "Test version installed";
                lblStatusMessage.Foreground = new SolidColorBrush(Colors.Green);
            }

        }
        public static string GetFinalRedirect(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            int maxRedirCount = 8;  // prevent infinite loops
            string newUrl = url;
            do
            {
                HttpWebRequest req = null;
                HttpWebResponse resp = null;
                try
                {
                    req = (HttpWebRequest)HttpWebRequest.Create(url);
                    req.Method = "HEAD";
                    req.AllowAutoRedirect = false;
                    resp = (HttpWebResponse)req.GetResponse();
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            return newUrl;
                        case HttpStatusCode.Redirect:
                        case HttpStatusCode.MovedPermanently:
                        case HttpStatusCode.RedirectKeepVerb:
                        case HttpStatusCode.RedirectMethod:
                            newUrl = resp.Headers["Location"];
                            if (newUrl == null)
                                return url;

                            if (newUrl.IndexOf("://", System.StringComparison.Ordinal) == -1)
                            {
                                // Doesn't have a URL Schema, meaning it's a relative or absolute URL
                                Uri u = new Uri(new Uri(url), newUrl);
                                newUrl = u.ToString();
                            }
                            break;
                        default:
                            return newUrl;
                    }
                    url = newUrl;
                }
                catch (WebException)
                {
                    // Return the last known good URL
                    return newUrl;
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, "MainWindow.xaml:GetFinalRedirect", $"Exception {ex}: {ex.Message}");
                    return null;
                }
                finally
                {
                    if (resp != null)
                        resp.Close();
                }
            } while (maxRedirCount-- > 0);

            return newUrl;
        }
        protected void LoadSettings()
        {
            chkOpenWindow.IsChecked = serviceModel.OpenWindow;
            chkUseExpertOptions.IsChecked = serviceModel.UseExpertOptions;
            chkCustomAutoOLOD.IsChecked = serviceModel.CustomAutoOLOD;
            if (serviceModel.CustomAutoOLOD && serviceModel.UseExpertOptions) stkpnlCustomAutoOLOD.Visibility = Visibility.Visible;
            else stkpnlCustomAutoOLOD.Visibility = Visibility.Collapsed;
            chkTestLogSimValues.IsChecked = serviceModel.TestLogSimValues;
            if (serviceModel.OnTop) AutoFPS.Topmost = true; 
            else AutoFPS.Topmost = false;
            chkOnTop.IsChecked = serviceModel.OnTop;
            if (serviceModel.ActiveGraphicsMode == "VR") txtTargetFPS.Text = Convert.ToString(serviceModel.TargetFPS_VR, CultureInfo.CurrentUICulture);
            else if (serviceModel.ActiveGraphicsMode == "FG") txtTargetFPS.Text = Convert.ToString(serviceModel.TargetFPS_FG, CultureInfo.CurrentUICulture);
            else txtTargetFPS.Text = Convert.ToString(serviceModel.TargetFPS_PC, CultureInfo.CurrentUICulture);
            serviceModel.ActiveGraphicsModeChanged = false;
            txtFPSTolerance.Text = Convert.ToString(serviceModel.FPSTolerance, CultureInfo.CurrentUICulture);
            txtMinTLod.Text = Convert.ToString(serviceModel.MinTLOD, CultureInfo.CurrentUICulture);
            txtMaxTLod.Text = Convert.ToString(serviceModel.MaxTLOD, CultureInfo.CurrentUICulture);
            txtOLODAtBase.Text = Convert.ToString(serviceModel.OLODAtBase, CultureInfo.CurrentUICulture);
            txtOLODAtTop.Text = Convert.ToString(serviceModel.OLODAtTop, CultureInfo.CurrentUICulture);
            txtAltOLODBase.Text = Convert.ToString(serviceModel.AltOLODBase, CultureInfo.CurrentUICulture);
            txtAltOLODTop.Text = Convert.ToString(serviceModel.AltOLODTop, CultureInfo.CurrentUICulture);
            txtAltTLODBase.Text = Convert.ToString(serviceModel.AltTLODBase, CultureInfo.CurrentUICulture);
            txtAvgDescentRate.Text = Convert.ToString(serviceModel.AvgDescentRate, CultureInfo.CurrentUICulture);
            chkDecCloudQ.IsChecked = serviceModel.DecCloudQ;
            chkPauseMSFSFocusLost.IsChecked = serviceModel.PauseMSFSFocusLost;
            chkTLODMinGndLanding.IsChecked = serviceModel.TLODMinGndLanding;
            if (serviceModel.TLODMinGndLanding && serviceModel.UseExpertOptions) stkpnlTLODMinOptions.Visibility = Visibility.Visible;
            else stkpnlTLODMinOptions.Visibility = Visibility.Collapsed;
            txtCloudRecoveryTLOD.Text = Convert.ToString(serviceModel.CloudRecoveryTLOD, CultureInfo.CurrentUICulture);
            if (ServiceModel.TestVersion && serviceModel.TestLogSimValues) Logger.Log(LogLevel.Information, "MainWindow:LoadSettings", $"Expert: {serviceModel.UseExpertOptions} Mode: {serviceModel.ActiveGraphicsMode} Target: {txtTargetFPS.Text} Tol: {txtFPSTolerance.Text} TMin: {txtMinTLod.Text} TMax: {txtMaxTLod.Text} CloudQ: {serviceModel.DecCloudQ} CRecovT: {txtCloudRecoveryTLOD.Text} Pause: {serviceModel.PauseMSFSFocusLost} TMinGL: {serviceModel.TLODMinGndLanding} TLODBAlt: {serviceModel.AltTLODBase} MaxDescRate {serviceModel.AvgDescentRate} CustomOLOD: {serviceModel.CustomAutoOLOD} OLODB: {serviceModel.OLODAtBase} OLODT: {serviceModel.OLODAtTop} OLODBAlt: {serviceModel.AltOLODBase} OLODTAlt: {serviceModel.AltOLODTop}");
        }

        protected void UpdateStatus()
        {
            if (serviceModel.IsSimRunning)
                lblConnStatMSFS.Foreground = new SolidColorBrush(Colors.DarkGreen);
            else
                lblConnStatMSFS.Foreground = new SolidColorBrush(Colors.Red);

            if (IPCManager.SimConnect != null && IPCManager.SimConnect.IsReady)
                lblConnStatSimConnect.Foreground = new SolidColorBrush(Colors.DarkGreen);
            else
                lblConnStatSimConnect.Foreground = new SolidColorBrush(Colors.Red);

            if (serviceModel.IsSessionRunning)
                lblConnStatSession.Foreground = new SolidColorBrush(Colors.DarkGreen);
            else
                lblConnStatSession.Foreground = new SolidColorBrush(Colors.Red);
        }

        protected string CloudQualityLabel(int CloudQuality)
        {
            if (CloudQuality == 0) return "Low";
            else if (CloudQuality == 1) return "Medium";
            else if (CloudQuality == 2) return "High";
            else if (CloudQuality == 3) return "Ultra";
            else return "n/a";
        }

        protected float GetAverageFPS()
        {
            if (serviceModel.MemoryAccess != null && serviceModel.FgModeEnabled && serviceModel.ActiveWindowMSFS)
                return IPCManager.SimConnect.GetAverageFPS() * 2.0f;
            else
                return IPCManager.SimConnect.GetAverageFPS();
        }
        protected void UpdateLiveValues()
        {
            if (IPCManager.SimConnect != null && IPCManager.SimConnect.IsConnected)
                lblSimFPS.Content = GetAverageFPS().ToString("F0");
            else
            {
                lblSimFPS.Content = "n/a";
                lblSimFPS.Foreground = new SolidColorBrush(Colors.Black);
            }
 
            if (serviceModel.MemoryAccess != null)
            {
                lblappUrl.Visibility = Visibility.Hidden;
                lblStatusMessage.Foreground = new SolidColorBrush(Colors.Black);
                lblSimTLOD.Content = serviceModel.tlod.ToString("F0");
                lblSimOLOD.Content = serviceModel.olod.ToString("F0");
                if (serviceModel.MemoryAccess.MemoryWritesAllowed())
                {
                    lblStatusMessage.Content = serviceModel.MemoryAccess.IsDX12() ? "DX12" : " DX11";
                    if (serviceModel.VrModeActive)
                    {
                        lblSimCloudQs.Content = CloudQualityLabel(serviceModel.cloudQ_VR);
                        lblStatusMessage.Content += " | VR Mode";
                    }
                    else
                    {
                        lblSimCloudQs.Content = CloudQualityLabel(serviceModel.cloudQ);
                        lblStatusMessage.Content += (serviceModel.FgModeEnabled ? (serviceModel.ActiveWindowMSFS ? " | FG Active" : " | FG Inactive") : " | PC Mode");
                    }
                    if (!serviceModel.ActiveWindowMSFS && serviceModel.UseExpertOptions && serviceModel.PauseMSFSFocusLost) lblStatusMessage.Content += " | Auto PAUSED";
                    else if (serviceModel.FPSSettleCounter > 0) lblStatusMessage.Content += " | FPS Settling for " + serviceModel.FPSSettleCounter.ToString("F0") + " second" + (serviceModel.FPSSettleCounter != 1 ? "s" : "");
                    else lblStatusMessage.Content += serviceModel.IsAppPriorityFPS ? " | FPS priority" : " | TLOD Min priority";
                }
                else
                {
                    lblStatusMessage.Content = "MSFS compatibility test failed - Read Only mode";
                    lblStatusMessage.Foreground = new SolidColorBrush(Colors.Red);
                }
                if (serviceModel.IsSessionRunning)
                {
                    bool TLODMinGndLanding;
                    float MinTLOD = serviceModel.MinTLOD;
                    float MaxTLOD = serviceModel.MaxTLOD;
                    float TargetFPS = (serviceModel.FgModeEnabled && !serviceModel.ActiveWindowMSFS ? serviceModel.TargetFPS / 2 : serviceModel.TargetFPS);
                    if (serviceModel.UseExpertOptions)
                    {
                        TLODMinGndLanding = serviceModel.TLODMinGndLanding;
                        MinTLOD = serviceModel.MinTLOD;
                        MaxTLOD = serviceModel.MaxTLOD;
                    }
                    else
                    {
                        TLODMinGndLanding = true;
                        if (serviceModel.VrModeActive)
                        {
                            MinTLOD = Math.Max(serviceModel.DefaultTLOD_VR * 0.5f, 10);
                            MaxTLOD = serviceModel.DefaultTLOD_VR * 2.0f;
                        }
                        else
                        {
                            MinTLOD = Math.Max(serviceModel.DefaultTLOD * 0.5f, 10);
                            MaxTLOD = serviceModel.DefaultTLOD * 2.0f;
                        }
                    }

                    lblTargetFPS.Content = "Target " + serviceModel.ActiveGraphicsMode + (serviceModel.ActiveGraphicsMode == "FG" ? " Active" : "") + " FPS";
                    if (serviceModel.ActiveGraphicsModeChanged) LoadSettings();
                    float ToleranceFPS = TargetFPS * (serviceModel.UseExpertOptions ? serviceModel.FPSTolerance : 5.0f) / 100.0f;
                    if (TLODMinGndLanding)
                    {
                        if (GetAverageFPS() < TargetFPS - ToleranceFPS) lblSimFPS.Foreground = new SolidColorBrush(Colors.Red);
                        else if (GetAverageFPS() > TargetFPS + ToleranceFPS) lblSimFPS.Foreground = new SolidColorBrush(Colors.Green);
                        else lblSimFPS.Foreground = new SolidColorBrush(Colors.Black);
                    }
                    else
                    {
                        if (GetAverageFPS() < TargetFPS - ToleranceFPS && serviceModel.tlod == MinTLOD) lblSimFPS.Foreground = new SolidColorBrush(Colors.Red);
                        else if (Math.Abs(GetAverageFPS() - TargetFPS) <= ToleranceFPS) lblSimFPS.Foreground = new SolidColorBrush(Colors.Green);
                        else lblSimFPS.Foreground = new SolidColorBrush(Colors.Black);
                    }
                    if (serviceModel.tlod == MinTLOD && (!TLODMinGndLanding || GetAverageFPS() < TargetFPS)) lblSimTLOD.Foreground = new SolidColorBrush(Colors.Red);
                    else if ((!TLODMinGndLanding && serviceModel.tlod == MaxTLOD) || (TLODMinGndLanding && serviceModel.tlod == MinTLOD && GetAverageFPS() > TargetFPS)) lblSimTLOD.Foreground = new SolidColorBrush(Colors.Green);
                    else if (serviceModel.tlod_step) lblSimTLOD.Foreground = new SolidColorBrush(Colors.Orange);
                    else lblSimTLOD.Foreground = new SolidColorBrush(Colors.Black);

                    if (serviceModel.DecCloudQ && serviceModel.DecCloudQActive) lblSimCloudQs.Foreground = new SolidColorBrush(Colors.Red);
                    else lblSimCloudQs.Foreground = new SolidColorBrush(Colors.Black);
                    if (serviceModel.CustomAutoOLOD && serviceModel.UseExpertOptions && (serviceModel.olod == serviceModel.OLODAtBase || serviceModel.olod == serviceModel.OLODAtTop)) lblSimOLOD.Foreground = new SolidColorBrush(Colors.Green);
                    else if (serviceModel.olod_step) lblSimOLOD.Foreground = new SolidColorBrush(Colors.Orange);
                    else lblSimOLOD.Foreground= new SolidColorBrush(Colors.Black);

                    if (ServiceModel.TestVersion && serviceModel.TestLogSimValues && logTimer == 0 && serviceModel.FPSSettleCounter == 0 && !(!serviceModel.ActiveWindowMSFS && serviceModel.UseExpertOptions && serviceModel.PauseMSFSFocusLost))
                    {
                        Logger.Log(LogLevel.Information, "MainWindow:UpdateLiveValues", $"FPS: {lblSimFPS.Content} TLOD: {lblSimTLOD.Content} OLOD: {lblSimOLOD.Content} AGL: {lblPlaneAGL.Content} FPM: {lblPlaneVS.Content} Clouds: {lblSimCloudQs.Content}");
                        logTimer = logTimerInterval;
                    }
                    else if (--logTimer < 0) logTimer = 0;
                }
                else lblSimFPS.Foreground = new SolidColorBrush(Colors.Black);
            }
            else
            {
                lblSimTLOD.Content = "n/a";
                lblSimTLOD.Foreground = new SolidColorBrush(Colors.Black);
                lblSimOLOD.Content = "n/a";
                lblSimOLOD.Foreground = new SolidColorBrush(Colors.Black);
                lblSimCloudQs.Content = "n/a";
                lblSimCloudQs.Foreground = new SolidColorBrush(Colors.Black);
                lblSimFPS.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        protected void UpdateAircraftValues()
        {
            if (IPCManager.SimConnect != null && IPCManager.SimConnect.IsConnected)
            {
                var simConnect = IPCManager.SimConnect;
                lblPlaneAGL.Content = simConnect.ReadSimVar("PLANE ALT ABOVE GROUND", "feet").ToString("F0");
                lblPlaneVS.Content = (simConnect.ReadSimVar("VERTICAL SPEED", "feet per second") * 60.0f).ToString("F0");
            }
            else
            {
                lblPlaneAGL.Content = "n/a";
                lblPlaneVS.Content = "n/a";
            }
        }

        protected void OnTick(object sender, EventArgs e)
        {
            UpdateStatus();
            UpdateLiveValues();
            UpdateAircraftValues();
        }

        protected void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                notifyModel.CanExecuteHideWindow = false;
                notifyModel.CanExecuteShowWindow = true;
                timer.Stop();
            }
            else
            {
                LoadSettings();
                chkCloudRecoveryTLOD_WindowVisibility();
                timer.Start();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void chkTestLogSimValues_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("testLogSimValues", chkTestLogSimValues.IsChecked.ToString().ToLower());
            LoadSettings();
        }
        private void chkOnTop_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("OnTop", chkOnTop.IsChecked.ToString().ToLower());
            LoadSettings();
        }
        private void chkUseExpertOptions_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("useExpertOptions", chkUseExpertOptions.IsChecked.ToString().ToLower());
            LoadSettings();
            if (serviceModel.UseExpertOptions) stkpnlMSFSSettings.Visibility = Visibility.Visible;
            else stkpnlMSFSSettings.Visibility = Visibility.Collapsed;
        }
        private void chkCustomAutoOLOD_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("customAutoOLOD", chkCustomAutoOLOD.IsChecked.ToString().ToLower());
            LoadSettings();
        }
        private void chkOpenWindow_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("openWindow", chkOpenWindow.IsChecked.ToString().ToLower());
            LoadSettings();
        }

        private void chkTLODMinGndLanding_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("TLODMinGndLanding", chkTLODMinGndLanding.IsChecked.ToString().ToLower());
            LoadSettings();
            if (serviceModel.MemoryAccess != null)
            {
                if (serviceModel.VrModeActive) serviceModel.MemoryAccess.SetCloudQ_VR(serviceModel.DefaultCloudQ_VR);
                else serviceModel.MemoryAccess.SetCloudQ(serviceModel.DefaultCloudQ);
                serviceModel.DecCloudQActive = false;
            }
        }
        private void chkDecCloudQ_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("DecCloudQ", chkDecCloudQ.IsChecked.ToString().ToLower());
            LoadSettings();
            chkCloudRecoveryTLOD_WindowVisibility();

        }
        private void chkPauseMSFSFocusLost_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("PauseMSFSFocusLost", chkPauseMSFSFocusLost.IsChecked.ToString().ToLower());
            LoadSettings();
            chkCloudRecoveryTLOD_WindowVisibility();

        }
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox_SetSetting(sender as TextBox);
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || e.Key != Key.Return)
                return;

            TextBox_SetSetting(sender as TextBox);
        }

        private void TextBox_SetSetting(TextBox sender)
        {
            if (sender == null || string.IsNullOrWhiteSpace(sender.Text))
                return;

            string key;
            bool intValue = false;
            bool notNegative = true;
            bool zeroAllowed = false;
            switch (sender.Name)
            {
                case "txtTargetFPS":
                    key = "targetFps";
                    intValue = true;
                    break;
                case "txtFPSTolerance":
                    key = "FpsTolerance";
                    intValue = true;
                    break;
                case "txtCloudRecoveryTLOD":
                    key = "CloudRecoveryTLOD";
                    break;
                case "txtMinTLod":
                    key = "minTLod";
                    break;
                case "txtMaxTLod":
                    key = "maxTLod";
                    break;
                case "txtOLODAtBase":
                    key = "OLODAtBase";
                    break;
                case "txtOLODAtTop":
                    key = "OLODAtTop";
                    break;
                case "txtAltOLODBase":
                    key = "AltOLODBase";
                    break;
                case "txtAltOLODTop":
                    key = "AltOLODTop";
                    break;
                case "txtAltTLODBase":
                    key = "AltTLODBase";
                    break;
                case "txtAvgDescentRate":
                    key = "AvgDescentRate";
                    break;
                default:
                    key = "";
                    break;
            }

            if (key == "")
                return;

            if (intValue && int.TryParse(sender.Text, CultureInfo.InvariantCulture, out int iValue) && (iValue != 0 || zeroAllowed))
            {
                if (notNegative)
                    iValue = Math.Abs(iValue);
                switch (key)
                {
                    case "targetFps":
                        if (iValue < 10 || iValue > 200) iValue = serviceModel.TargetFPS;
                        if (serviceModel.ActiveGraphicsMode == "VR") key = "targetFpsVR";
                        else if (serviceModel.ActiveGraphicsMode == "FG") key = "targetFpsFG";
                        else key = "targetFpsPC";
                        break;
                    case "FpsTolerance":
                        if (iValue > 20) iValue = serviceModel.FPSTolerance;
                        break;
                    default:
                        break;
                }
                serviceModel.SetSetting(key, Convert.ToString(iValue, CultureInfo.InvariantCulture));
            }

            if (!intValue && float.TryParse(sender.Text, new RealInvariantFormat(sender.Text), out float fValue))
            {
                if (notNegative)
                    fValue = Math.Abs(fValue);
                switch (key)
                {
                    case "minTLod":
                        if (fValue < 10 || fValue > ServiceModel.TLODMinLockAlt || fValue > serviceModel.MaxTLOD - 10) fValue = serviceModel.MinTLOD;
                        if (serviceModel.CloudRecoveryTLOD < fValue + 10) serviceModel.SetSetting("CloudRecoveryTLOD", Convert.ToString(Math.Round(2 * (fValue + serviceModel.MaxTLOD) / 5), CultureInfo.InvariantCulture));
                        break;
                    case "maxTLod":
                        if (fValue < serviceModel.MinTLOD + 10 || fValue > 1000) fValue = serviceModel.MaxTLOD;
                        if (serviceModel.CloudRecoveryTLOD > fValue - 10) serviceModel.SetSetting("CloudRecoveryTLOD", Convert.ToString(Math.Round(2 * (fValue + serviceModel.MinTLOD) / 5), CultureInfo.InvariantCulture));
                        break;
                    case "CloudRecoveryTLOD":
                        if (fValue < serviceModel.MinTLOD + 5 || fValue > serviceModel.MaxTLOD - 5) fValue = serviceModel.CloudRecoveryTLOD;
                        break;
                    case "OLODAtBase":
                        if (fValue < 10 || fValue > 1000) fValue = serviceModel.OLODAtBase;
                        break;
                    case "OLODAtTop":
                        if (fValue < 10 || fValue > 1000) fValue = serviceModel.OLODAtTop;
                        break;
                    case "AltOLODBase":
                        if (fValue < 1000 || fValue >= 100000 || fValue > serviceModel.AltOLODTop - 1) fValue = serviceModel.AltOLODBase;
                        break;
                    case "AltOLODTop":
                        if (fValue < 2000 || fValue > 100000 || fValue < serviceModel.AltOLODBase + 1) fValue = serviceModel.AltOLODTop;
                        break;
                    case "AltTLODBase":
                        if (fValue < 0 || fValue >= 100000) fValue = serviceModel.AltTLODBase;
                        break;
                    case "AvgDescentRate":
                        if (fValue < 200 || fValue > 10000) fValue = serviceModel.AvgDescentRate;
                        break;
                    default:
                        break;
                }
                serviceModel.SetSetting(key, Convert.ToString(fValue, CultureInfo.InvariantCulture));
            }

            LoadSettings();
        }

        private void txtLodStepMaxInc_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void txtLodStepMaxDec_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void chkDecCloudQ_Checked(object sender, RoutedEventArgs e)
        {

        }
        
        private void chkPauseMSFSFocusLost_Checked(object sender, RoutedEventArgs e)
        {

        }
        private void chkTLODMinGndLanding_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                var myProcess = new Process();
                myProcess.StartInfo.UseShellExecute = true;
                myProcess.StartInfo.FileName = "https://github.com/ResetXPDR/MSFS2020_AutoFPS/releases/latest";
                myProcess.Start();
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MainWindow.xaml:Hyperlink_RequestNavigate", $"Exception {ex}: {ex.Message}");
            }
        }
        private void chkCloudRecoveryTLOD_WindowVisibility()
        {
            if (serviceModel.DecCloudQ)
            {
                lblCloudRecoveryTLOD.Visibility = Visibility.Visible;
                txtCloudRecoveryTLOD.Visibility = Visibility.Visible;
            }
            else
            {
                lblCloudRecoveryTLOD.Visibility = Visibility.Hidden;
                txtCloudRecoveryTLOD.Visibility = Visibility.Hidden;
            }
        }

        private void chkCustomAutoOLOD_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
