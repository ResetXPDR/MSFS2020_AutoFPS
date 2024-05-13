using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Policy;
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

        public MainWindow(NotifyIconViewModel notifyModel, ServiceModel serviceModel)
        {
            InitializeComponent();
            this.notifyModel = notifyModel;
            this.serviceModel = serviceModel;

            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Title += " (" + assemblyVersion + (ServiceModel.TestVersion ? "-test" : "") + ")";

            if (serviceModel.UseExpertOptions) stkpnlExpertSettings.Visibility = Visibility.Visible;
            else stkpnlExpertSettings.Visibility = Visibility.Collapsed;
 
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += OnTick;

            this.Resources["CustomLabelColor"] = new SolidColorBrush(Colors.Aqua);

            if (serviceModel.windowPanelState >= 1) stkpnlExpertSettings.Visibility = Visibility.Collapsed;
            if (serviceModel.windowPanelState == 2) stkpnlGeneral.Visibility = Visibility.Collapsed;

            string latestAppVersionStr = GetFinalRedirect("https://github.com/ResetXPDR/MSFS2020_AutoFPS/releases/latest");
            lblappUrl.Visibility = Visibility.Hidden;
            if (int.TryParse(assemblyVersion.Replace(".", ""), CultureInfo.InvariantCulture, out int currentAppVersion) &&  latestAppVersionStr != null && latestAppVersionStr.Length > 50)
            { 
                string latestAppVersionStr2 = latestAppVersionStr = latestAppVersionStr.Substring(latestAppVersionStr.IndexOf("v") + 1);
                if (latestAppVersionStr.Substring(latestAppVersionStr.LastIndexOf(".") + 1).Length == 1)
                    latestAppVersionStr2 = latestAppVersionStr.Substring(0, latestAppVersionStr.Length - 1) + "0" + latestAppVersionStr.Substring(latestAppVersionStr.LastIndexOf(".") + 1);
                if (int.TryParse(latestAppVersionStr2.Replace(".", ""), CultureInfo.InvariantCulture, out int LatestAppVersion))
                {
                    if ((ServiceModel.TestVersion && LatestAppVersion >= currentAppVersion) || LatestAppVersion > currentAppVersion)
                    {
                        lblStatusMessage.Content = "Newer app version " + (latestAppVersionStr) + " now available";
                        lblStatusMessage.Foreground = new SolidColorBrush(Colors.Green);
                        lblappUrl.Visibility = Visibility.Visible;
                    }
                    else if (ServiceModel.TestVersion)
                    {
                        lblStatusMessage.Content = "Test version installed";
                        lblStatusMessage.Foreground = new SolidColorBrush(Colors.Green);
                    }
                    else
                    {
                        lblStatusMessage.Content = "Latest app version is installed";
                        lblStatusMessage.Foreground = new SolidColorBrush(Colors.Green);
                    }
                }   
            }
            else if (ServiceModel.TestVersion)
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
            if (serviceModel.RememberWindowPos)
            {
                Top = serviceModel.windowTop;
                Left = serviceModel.windowLeft;
            }
            if (serviceModel.TLODAutoMethod[serviceModel.activeProfile] == 2)
            {
                lblTLODAuto1.Content = "LOD Step";
                lblTLODAuto2.Content = "";
                lblTLODMin.Content = "TLOD @ Base";
                lblTLODMax.Content = "TLOD @ Top";
                lblAltTLOD1.Content = "Alt TLOD Top";
                lblAltTLOD2.Content = "ft";
                txtAvgDescentRate.ToolTip = "TLOD Max will be locked above this altitude";
                txtAvgDescentRate.Text = Convert.ToString(serviceModel.AltTLODTop[serviceModel.activeProfile], CultureInfo.CurrentUICulture);
                chkMinTLODExtra.Visibility = Visibility.Hidden;
                lblCloudMethod.Visibility = Visibility.Hidden;
                cbCloudMethod.Visibility = Visibility.Hidden;
                stkpnlCloudQualityOptions2.Visibility = Visibility.Collapsed;
                stkpnlCloudQualityOptions3.Visibility = Visibility.Visible;
                chkDecCloudQ.IsChecked = serviceModel.DecCloudQ[serviceModel.activeProfile];
                serviceModel.AutoTargetFPS = false;
                chkAutoTargetFPS.Visibility = Visibility.Hidden;
                lblTargetFPS.Visibility= Visibility.Hidden;
                txtTargetFPS.Visibility = Visibility.Hidden;
                chkPauseMSFSFocusLost.Visibility = Visibility.Hidden;
                serviceModel.PauseMSFSFocusLost = false;
            }
            else
            {
                lblTLODAuto1.Content = "";
                lblTLODAuto2.Content = "%"; 
                lblTLODMin.Content = "TLOD Min    +";
                lblTLODMax.Content = "TLOD Max";
                lblAltTLOD1.Content = "Avg Descent Rate";
                lblAltTLOD2.Content = "fpm";
                txtAvgDescentRate.ToolTip = "Determines what altitude TLOD will start reducing towards TLOD Min";
                txtAvgDescentRate.Text = Convert.ToString(serviceModel.AvgDescentRate[serviceModel.activeProfile], CultureInfo.CurrentUICulture);
                chkMinTLODExtra.IsChecked = serviceModel.MinTLODExtra[serviceModel.activeProfile];
                chkMinTLODExtra.Visibility = Visibility.Visible;
                chkDecCloudQ.IsChecked = serviceModel.DecCloudQ[serviceModel.activeProfile];
                serviceModel.AutoTargetFPS = Convert.ToBoolean(serviceModel.ConfigurationFile.GetSetting("AutoTargetFPS", "false"));
                lblCloudMethod.Visibility = Visibility.Visible;
                cbCloudMethod.Visibility = Visibility.Visible;
                cbCloudMethod.SelectedIndex = serviceModel.DecCloudQMethod[serviceModel.activeProfile];
                if (serviceModel.DecCloudQMethod[serviceModel.activeProfile] == 0)
                {
                    stkpnlCloudQualityOptions2.Visibility = Visibility.Visible;
                    stkpnlCloudQualityOptions3.Visibility = Visibility.Collapsed;
                }
                else 
                {
                    stkpnlCloudQualityOptions2.Visibility = Visibility.Collapsed;
                    stkpnlCloudQualityOptions3.Visibility = Visibility.Visible;
                }
                chkAutoTargetFPS.Visibility = Visibility.Visible;
                lblTargetFPS.Visibility = Visibility.Visible;
                txtTargetFPS.Visibility = Visibility.Visible;
                chkPauseMSFSFocusLost.Visibility = Visibility.Visible;
                serviceModel.PauseMSFSFocusLost = Convert.ToBoolean(serviceModel.ConfigurationFile.GetSetting("PauseMSFSFocusLost", "false"));
                if (serviceModel.UseExpertOptions && serviceModel.CloudRecoveryPlus[serviceModel.activeProfile] && serviceModel.MinTLOD[serviceModel.activeProfile] + serviceModel.CloudRecoveryTLOD[serviceModel.activeProfile] > serviceModel.MaxTLOD[serviceModel.activeProfile])
                {
                    serviceModel.SetSetting(serviceModel.activeProfile == (int)ServiceModel.appProfiles.IFR_Expert ? "CloudRecoveryTLOD" : "CloudRecoveryTLOD_VFR", Convert.ToString((int)(2 * (serviceModel.MaxTLOD[serviceModel.activeProfile] - serviceModel.MinTLOD[serviceModel.activeProfile]) / 5), CultureInfo.InvariantCulture));
                }
                else if (serviceModel.UseExpertOptions && !serviceModel.CloudRecoveryPlus[serviceModel.activeProfile] && (serviceModel.CloudRecoveryTLOD[serviceModel.activeProfile] <= serviceModel.MinTLOD[serviceModel.activeProfile] || serviceModel.CloudRecoveryTLOD[serviceModel.activeProfile] >= serviceModel.MaxTLOD[serviceModel.activeProfile]))
                {
                    serviceModel.SetSetting(serviceModel.activeProfile == (int)ServiceModel.appProfiles.IFR_Expert ? "CloudRecoveryTLOD" : "CloudRecoveryTLOD_VFR", Convert.ToString((int)(serviceModel.MinTLOD[serviceModel.activeProfile] + 2 * (float)(serviceModel.MaxTLOD[serviceModel.activeProfile] - serviceModel.MinTLOD[serviceModel.activeProfile]) / 5), CultureInfo.InvariantCulture));
                }
                txtCloudRecoveryTLOD.Text = Convert.ToString(serviceModel.CloudRecoveryTLOD[serviceModel.activeProfile], CultureInfo.CurrentUICulture);
            }
            txtCloudDecreaseGPUPct.Text = Convert.ToString(serviceModel.CloudDecreaseGPUPct, CultureInfo.CurrentUICulture);
            txtCloudRecoverGPUPct.Text = Convert.ToString(serviceModel.CloudRecoverGPUPct, CultureInfo.CurrentUICulture);
            chkUseExpertOptions.IsChecked = serviceModel.UseExpertOptions;
            if (serviceModel.FlightTypeIFR) optIFRFlight.IsChecked = true;
            else optVFRFlight.IsChecked= true;
            chkAutoTargetFPS.IsChecked = serviceModel.AutoTargetFPS;
            if (serviceModel.AutoTargetFPS || serviceModel.TLODAutoMethod[serviceModel.activeProfile] == 2) txtTargetFPS.IsEnabled = false;
            else txtTargetFPS.IsEnabled = true;
            if (serviceModel.OnTop) AutoFPS.Topmost = true; 
            else AutoFPS.Topmost = false;
            chkOnTop.IsChecked = serviceModel.OnTop;
            if (!serviceModel.UseExpertOptions) serviceModel.activeProfile = (int)ServiceModel.appProfiles.NonExpert;
            else if (serviceModel.FlightTypeIFR) serviceModel.activeProfile = (int)ServiceModel.appProfiles.IFR_Expert;
            else serviceModel.activeProfile = (int)ServiceModel.appProfiles.VFR_Expert;

            if (serviceModel.AutoTargetFPS && serviceModel.ForceAutoFPSCal) txtTargetFPS.Text = "auto";
            else if (serviceModel.AutoTargetFPS) txtTargetFPS.Text = Convert.ToString(serviceModel.TargetFPS, CultureInfo.CurrentUICulture);
            else if (serviceModel.ActiveGraphicsMode == "VR") txtTargetFPS.Text = Convert.ToString(serviceModel.FlightTypeIFR ? serviceModel.TargetFPS_VR : serviceModel.TargetFPS_VR_VFR, CultureInfo.CurrentUICulture);
            else if (serviceModel.ActiveGraphicsMode == "FG") txtTargetFPS.Text = Convert.ToString(serviceModel.FlightTypeIFR ? serviceModel.TargetFPS_FG : serviceModel.TargetFPS_FG_VFR, CultureInfo.CurrentUICulture);
            else txtTargetFPS.Text = Convert.ToString(serviceModel.FlightTypeIFR ? serviceModel.TargetFPS_PC : serviceModel.TargetFPS_PC_VFR, CultureInfo.CurrentUICulture);
            serviceModel.ActiveGraphicsModeChanged = false;
            txtFPSTolerance.Text = Convert.ToString(serviceModel.FPSTolerance[serviceModel.activeProfile], CultureInfo.CurrentUICulture);
            cbTLODAutoMethod.SelectedIndex = serviceModel.TLODAutoMethod[serviceModel.activeProfile];
            if (serviceModel.TLODAutoMethod[serviceModel.activeProfile] != 0) lblTLODAuto2.Visibility = Visibility.Visible;
            else lblTLODAuto2.Visibility= Visibility.Hidden;
            txtMinTLod.Text = Convert.ToString(serviceModel.MinTLOD[serviceModel.activeProfile], CultureInfo.CurrentUICulture);
            txtMaxTLod.Text = Convert.ToString(serviceModel.MaxTLOD[serviceModel.activeProfile], CultureInfo.CurrentUICulture);
            chkCustomAutoOLOD.IsChecked = serviceModel.CustomAutoOLOD[serviceModel.activeProfile];
            if (serviceModel.CustomAutoOLOD[serviceModel.activeProfile] && serviceModel.UseExpertOptions) stkpnlCustomAutoOLOD.Visibility = Visibility.Visible;
            else stkpnlCustomAutoOLOD.Visibility = Visibility.Collapsed;
            txtOLODAtBase.Text = Convert.ToString(serviceModel.OLODAtBase[serviceModel.activeProfile], CultureInfo.CurrentUICulture);
            txtOLODAtTop.Text = Convert.ToString(serviceModel.OLODAtTop[serviceModel.activeProfile], CultureInfo.CurrentUICulture);
            txtAltOLODBase.Text = Convert.ToString(serviceModel.AltOLODBase[serviceModel.activeProfile], CultureInfo.CurrentUICulture);
            txtAltOLODTop.Text = Convert.ToString(serviceModel.AltOLODTop[serviceModel.activeProfile], CultureInfo.CurrentUICulture);
            txtAltTLODBase.Text = Convert.ToString(serviceModel.AltTLODBase[serviceModel.activeProfile], CultureInfo.CurrentUICulture);
            chkPauseMSFSFocusLost.IsChecked = serviceModel.PauseMSFSFocusLost;
            chkCloudRecoveryPlus.IsChecked = serviceModel.CloudRecoveryPlus[serviceModel.activeProfile];
            if (ServiceModel.TestVersion || serviceModel.LogSimValues) Logger.Log(LogLevel.Information, "MainWindow:LoadSettings", $"Expert: {serviceModel.UseExpertOptions} Mode: {serviceModel.ActiveGraphicsMode} ATgtFPS: {serviceModel.AutoTargetFPS} FltType: {(serviceModel.FlightTypeIFR ? "IFR" : "VFR")} TgtFPS: {txtTargetFPS.Text} TLODAMtd: {serviceModel.TLODAutoMethod[serviceModel.activeProfile]} Tol: {txtFPSTolerance.Text}" + (!serviceModel.UseExpertOptions && serviceModel.MemoryAccess == null ? "" : $" TMin: {txtMinTLod.Text} TMax: {txtMaxTLod.Text} OLODB: {serviceModel.OLODAtBase[serviceModel.activeProfile]} OLODT: {serviceModel.OLODAtTop[serviceModel.activeProfile]} OLODBAlt: {serviceModel.AltOLODBase[serviceModel.activeProfile]}") +$" TMinEx: {(!serviceModel.UseExpertOptions || serviceModel.MinTLODExtra[serviceModel.activeProfile] ? "true" : "false")} CloudQ: {serviceModel.DecCloudQ[serviceModel.activeProfile]} CRecovT: {txtCloudRecoveryTLOD.Text} Pause: {serviceModel.PauseMSFSFocusLost} TLODBAlt: {serviceModel.AltTLODBase[serviceModel.activeProfile]} MaxDesRate {serviceModel.AvgDescentRate[serviceModel.activeProfile]} CustomOLOD: {serviceModel.CustomAutoOLOD[serviceModel.activeProfile]} OLODTAlt: {serviceModel.AltOLODTop[serviceModel.activeProfile]}");
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
                lblStatusMessage.Content = serviceModel.MemoryAccess.IsDX12() ? "DX12" : " DX11";
                if (serviceModel.VrModeActive)
                {
                    lblSimCloudQs.Content = serviceModel.CloudQualityText(serviceModel.cloudQ_VR);
                    lblStatusMessage.Content += " | VR Mode";
                }
                else
                {
                    lblSimCloudQs.Content = serviceModel.CloudQualityText(serviceModel.cloudQ);
                    lblStatusMessage.Content += (serviceModel.FgModeEnabled ? (serviceModel.ActiveWindowMSFS ? " | FG Active" : " | FG Inactive") : " | PC Mode");
                }
                if (serviceModel.IsSessionRunning)
                {
                    float TargetFPS = (serviceModel.FgModeEnabled && !serviceModel.ActiveWindowMSFS ? serviceModel.TargetFPS / 2 : serviceModel.TargetFPS);
                    if (serviceModel.TLODAutoMethod[serviceModel.activeProfile] != 2 && serviceModel.FPSSettleCounter > 0) lblStatusMessage.Content += " | FPS Settling " + serviceModel.FPSSettleCounter.ToString("F0") + "s";
                    else if (serviceModel.TLODAutoMethod[serviceModel.activeProfile] == 2) lblStatusMessage.Content += " | Auto TLOD";
                    else lblStatusMessage.Content += serviceModel.IsAppPriorityFPS ? " | FPS Priority" : " | TLOD" + ((!serviceModel.UseExpertOptions || serviceModel.MinTLODExtra[serviceModel.activeProfile]) && serviceModel.MinTLODExtraActive ? "+ " : " ") + serviceModel.activeMinTLOD.ToString("F0") + " Priority";
                    if (serviceModel.gpuUsage > 0) lblStatusMessage.Content += " | GPU " + serviceModel.gpuUsage.ToString("F0") + "%";
                    else if ((serviceModel.TLODAutoMethod[serviceModel.activeProfile] == 2 || serviceModel.DecCloudQMethod[serviceModel.activeProfile] == 1) && serviceModel.DecCloudQ[serviceModel.activeProfile]) lblStatusMessage.Content += " | Start GPU-Z for Clouds";

                    if (serviceModel.AppPaused) lblStatusMessage.Content += " | Auto PAUSED";

                    lblTargetFPS.Content = "Target " + serviceModel.ActiveGraphicsMode + (serviceModel.ActiveGraphicsMode == "FG" ? " Active" : "") + " FPS";
                    if (serviceModel.ActiveGraphicsModeChanged) LoadSettings();

                    float ToleranceFPS = TargetFPS * serviceModel.FPSTolerance[serviceModel.activeProfile] / 100.0f;
                    if (serviceModel.TLODAutoMethod[serviceModel.activeProfile] == 2) lblSimFPS.Foreground = new SolidColorBrush(Colors.Black);
                    else if (GetAverageFPS() < TargetFPS - ToleranceFPS) lblSimFPS.Foreground = new SolidColorBrush(Colors.Red);
                    else if (GetAverageFPS() > TargetFPS + ToleranceFPS) lblSimFPS.Foreground = new SolidColorBrush(Colors.Green);
                    else lblSimFPS.Foreground = new SolidColorBrush(Colors.Black);
 
                    if ((serviceModel.tlod == serviceModel.activeMinTLOD || serviceModel.tlod == serviceModel.activeMaxTLOD) && (GetAverageFPS() > TargetFPS || serviceModel.TLODAutoMethod[serviceModel.activeProfile] == 2)) lblSimTLOD.Foreground = new SolidColorBrush(Colors.Green);
                    else if (serviceModel.tlod == serviceModel.activeMinTLOD && GetAverageFPS() < TargetFPS) lblSimTLOD.Foreground = new SolidColorBrush(Colors.Red);
                    else if (serviceModel.tlod_step && !serviceModel.AppPaused) lblSimTLOD.Foreground = new SolidColorBrush(Colors.Orange);
                    else lblSimTLOD.Foreground = new SolidColorBrush(Colors.Black);

                    if (serviceModel.DecCloudQ[serviceModel.activeProfile] && serviceModel.DecCloudQActive) lblSimCloudQs.Foreground = new SolidColorBrush(Colors.Red);
                    else lblSimCloudQs.Foreground = new SolidColorBrush(Colors.Black);
 
                    if ((!serviceModel.UseExpertOptions || serviceModel.CustomAutoOLOD[serviceModel.activeProfile]) && (serviceModel.olod == serviceModel.activeOLODAtBase || serviceModel.olod == serviceModel.activeOLODAtTop)) lblSimOLOD.Foreground = new SolidColorBrush(Colors.Green);
                    else if (serviceModel.olod_step && !serviceModel.AppPaused) lblSimOLOD.Foreground = new SolidColorBrush(Colors.Orange);
                    else lblSimOLOD.Foreground = new SolidColorBrush(Colors.Black);

                    if (serviceModel.AutoTargetFPS && serviceModel.UpdateTargetFPS)
                    {
                        txtTargetFPS.Text = Convert.ToString(serviceModel.TargetFPS, CultureInfo.CurrentUICulture);
                        serviceModel.UpdateTargetFPS = false;
                    }
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

            if (serviceModel.RememberWindowPos && !serviceModel.VrModeActive)
            {
                if ((int)Top != serviceModel.windowTop)
                {
                    serviceModel.windowTop = (int)Top;
                    serviceModel.SetSetting("windowTop", serviceModel.windowTop.ToString().ToLower());
                }
                if ((int)Left != serviceModel.windowLeft)
                {
                    serviceModel.windowLeft = (int)Left;
                    serviceModel.SetSetting("windowLeft", serviceModel.windowLeft.ToString().ToLower());
                }
            }

            UpdateStatus();
            if (serviceModel.AppEnabled) UpdateLiveValues();
            else
            {
                lblStatusMessage.Content = "MSFS compatibility test failed - app disabled. See readme to resolve.";
                lblStatusMessage.Foreground = new SolidColorBrush(Colors.Red);
                lblSimTLOD.Content = "n/a";
                lblSimTLOD.Foreground = new SolidColorBrush(Colors.Red);
                lblSimOLOD.Content = "n/a";
                lblSimOLOD.Foreground = new SolidColorBrush(Colors.Red);
                lblSimCloudQs.Content = "n/a";
                lblSimCloudQs.Foreground = new SolidColorBrush(Colors.Red);
                lblSimFPS.Content = "n/a";
                lblSimFPS.Foreground = new SolidColorBrush(Colors.Red);
                lblPlaneAGL.Content = "n/a";
                lblPlaneAGL.Foreground = new SolidColorBrush(Colors.Red);
                lblPlaneVS.Content = "n/a";
                lblPlaneVS.Foreground = new SolidColorBrush(Colors.Red);
            }
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
                timer.Start();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void chkAutoTargetFPS_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("AutoTargetFPS", chkAutoTargetFPS.IsChecked.ToString().ToLower());
            if ((bool)chkAutoTargetFPS.IsChecked)
            {
                serviceModel.ForceAutoFPSCal = true;
                serviceModel.FPSSettleCounter = ServiceModel.FPSSettleSeconds;
            }
            else
            {
                if (serviceModel.VrModeActive) serviceModel.TargetFPS = serviceModel.FlightTypeIFR ? serviceModel.TargetFPS_VR : serviceModel.TargetFPS_VR_VFR;
                else if (serviceModel.FgModeEnabled) serviceModel.TargetFPS = serviceModel.FlightTypeIFR ? serviceModel.TargetFPS_FG : serviceModel.TargetFPS_FG_VFR;
                else serviceModel.TargetFPS = serviceModel.FlightTypeIFR ? serviceModel.TargetFPS_PC : serviceModel.TargetFPS_PC_VFR;
            }
            serviceModel.ResetCloudsTLOD(); 
            LoadSettings();
        }
        private void chkOnTop_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("OnTop", chkOnTop.IsChecked.ToString().ToLower());
            LoadSettings();
        }
        private void chkFlightType_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("FlightTypeIFR", optIFRFlight.IsChecked.ToString().ToLower());
            serviceModel.ResetCloudsTLOD();
            LoadSettings();
        }
        private void chkUseExpertOptions_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("useExpertOptions", chkUseExpertOptions.IsChecked.ToString().ToLower());
            serviceModel.ResetCloudsTLOD();
            LoadSettings();
            if (serviceModel.UseExpertOptions) stkpnlExpertSettings.Visibility = Visibility.Visible;
            else stkpnlExpertSettings.Visibility = Visibility.Collapsed;
        }
        private void cbTLODAutoMethod_SelectionChange(object sender, EventArgs e)
        {
            if (serviceModel != null)
            {
                serviceModel.SetSetting(serviceModel.activeProfile == (int)ServiceModel.appProfiles.IFR_Expert ? "TLODAutoMethod" : "TLODAutoMethod_VFR", cbTLODAutoMethod.SelectedIndex.ToString());
                serviceModel.TLODAutoMethod[serviceModel.activeProfile] = cbTLODAutoMethod.SelectedIndex;
                serviceModel.ResetCloudsTLOD();
                LoadSettings();
            }
        }
        private void chkCustomAutoOLOD_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting(serviceModel.activeProfile == (int)ServiceModel.appProfiles.IFR_Expert ? "customAutoOLOD" : "customAutoOLOD_VFR", chkCustomAutoOLOD.IsChecked.ToString().ToLower());
            LoadSettings();
            if (serviceModel.MemoryAccess != null && serviceModel.CustomAutoOLOD[serviceModel.activeProfile])
            {
                serviceModel.MemoryAccess.SetOLOD_PC(serviceModel.DefaultOLOD);
                serviceModel.MemoryAccess.SetOLOD_VR(serviceModel.DefaultOLOD_VR);
            }
        }
        private void cbCloudMethod_SelectionChange(object sender, EventArgs e)
        {
            if (serviceModel != null)
            {
                serviceModel.SetSetting("DecCloudQMethod", cbCloudMethod.SelectedIndex.ToString(), true);
                serviceModel.SetSetting("DecCloudQMethod_VFR", cbCloudMethod.SelectedIndex.ToString());
                serviceModel.DecCloudQMethod[(int)ServiceModel.appProfiles.IFR_Expert] = serviceModel.DecCloudQMethod[(int)ServiceModel.appProfiles.VFR_Expert] = cbCloudMethod.SelectedIndex;
                serviceModel.ResetCloudsTLOD();
                LoadSettings();
            }
        }
        private void chkCloudRecoveryPlus_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting(serviceModel.activeProfile == (int)ServiceModel.appProfiles.IFR_Expert ? "CloudRecoveryPlus" : "CloudRecoveryPlus_VFR", chkCloudRecoveryPlus.IsChecked.ToString().ToLower());
            if (((bool)chkCloudRecoveryPlus.IsChecked && (serviceModel.CloudRecoveryTLOD[serviceModel.activeProfile] < 5 || serviceModel.CloudRecoveryTLOD[serviceModel.activeProfile] > serviceModel.MaxTLOD[serviceModel.activeProfile] - serviceModel.MinTLOD[serviceModel.activeProfile] - 5)) || (!(bool)chkCloudRecoveryPlus.IsChecked && (serviceModel.CloudRecoveryTLOD[serviceModel.activeProfile] < serviceModel.MinTLOD[serviceModel.activeProfile] + 5 || serviceModel.CloudRecoveryTLOD[serviceModel.activeProfile] > serviceModel.MaxTLOD[serviceModel.activeProfile] - 5))) serviceModel.SetSetting(serviceModel.activeProfile == (int)ServiceModel.appProfiles.IFR_Expert ? "CloudRecoveryTLOD" : "CloudRecoveryTLOD_VFR", Convert.ToString(Math.Round(((bool)chkCloudRecoveryPlus.IsChecked ? 0 : serviceModel.MinTLOD[serviceModel.activeProfile]) + 2 * (serviceModel.MaxTLOD[serviceModel.activeProfile] - serviceModel.MinTLOD[serviceModel.activeProfile]) / 5), CultureInfo.InvariantCulture));
            serviceModel.ResetCloudsTLOD();
            LoadSettings();
        }
        private void chkMinTLODExtra_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting(serviceModel.activeProfile == (int)ServiceModel.appProfiles.IFR_Expert ? "MinTLODExtra" : "MinTLODExtra_VFR", chkMinTLODExtra.IsChecked.ToString().ToLower());
            serviceModel.ResetCloudsTLOD();
            LoadSettings();
        }
 
        private void chkDecCloudQ_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting(serviceModel.activeProfile == (int)ServiceModel.appProfiles.IFR_Expert ? "DecCloudQ" : "DecCloudQ_VFR", chkDecCloudQ.IsChecked.ToString().ToLower());
            LoadSettings();
            serviceModel.ResetCloudsTLOD();
         }
        private void chkPauseMSFSFocusLost_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("PauseMSFSFocusLost", chkPauseMSFSFocusLost.IsChecked.ToString().ToLower());
            LoadSettings();
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
                case "txtCloudDecreaseGPUPct":
                    key = "CloudDecreaseGPUPct";
                    break;
                case "txtCloudRecoverGPUPct":
                    key = "CloudRecoverGPUPct";
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
                        if (serviceModel.ActiveGraphicsMode == "VR") key = serviceModel.FlightTypeIFR ? "targetFpsVR" : "targetFpsVRVFR";
                        else if (serviceModel.ActiveGraphicsMode == "FG") key = serviceModel.FlightTypeIFR ? "targetFpsFG" : "targetFpsFGVFR";
                        else key = serviceModel.FlightTypeIFR ? "targetFpsPC" : "targetFpsPCVFR";
                        break;
                    case "FpsTolerance":
                        if (iValue > 20) iValue = serviceModel.FPSTolerance[serviceModel.activeProfile];
                        if (serviceModel.activeProfile != (int)ServiceModel.appProfiles.IFR_Expert) key += "_VFR";
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
                        if (fValue < 10 || fValue > serviceModel.MaxTLOD[serviceModel.activeProfile] - 10) fValue = serviceModel.MinTLOD[serviceModel.activeProfile];
                        if (serviceModel.TLODAutoMethod[serviceModel.activeProfile] != 2 && (serviceModel.CloudRecoveryPlus[serviceModel.activeProfile] ? serviceModel.CloudRecoveryTLOD[serviceModel.activeProfile] + fValue > serviceModel.MaxTLOD[serviceModel.activeProfile] - 10 : serviceModel.CloudRecoveryTLOD[serviceModel.activeProfile] < fValue + 10)) serviceModel.SetSetting(serviceModel.activeProfile == (int)ServiceModel.appProfiles.IFR_Expert ? "CloudRecoveryTLOD" : "CloudRecoveryTLOD_VFR", Convert.ToString(Math.Round((serviceModel.CloudRecoveryPlus[serviceModel.activeProfile] ? 0 : serviceModel.MinTLOD[serviceModel.activeProfile]) + 2 * (serviceModel.MaxTLOD[serviceModel.activeProfile] - fValue) / 5), CultureInfo.InvariantCulture));
                        break;
                    case "maxTLod":
                        if (fValue < serviceModel.MinTLOD[serviceModel.activeProfile] + 10 || fValue > 1000) fValue = serviceModel.MaxTLOD[serviceModel.activeProfile];
                        if (serviceModel.TLODAutoMethod[serviceModel.activeProfile] != 2 && ((serviceModel.CloudRecoveryPlus[serviceModel.activeProfile] ? serviceModel.MinTLOD[serviceModel.activeProfile] : 0) + serviceModel.CloudRecoveryTLOD[serviceModel.activeProfile] > fValue - 10)) serviceModel.SetSetting(serviceModel.activeProfile == (int)ServiceModel.appProfiles.IFR_Expert ? "CloudRecoveryTLOD" : "CloudRecoveryTLOD_VFR", Convert.ToString(Math.Round((serviceModel.CloudRecoveryPlus[serviceModel.activeProfile] ? 0 : serviceModel.MinTLOD[serviceModel.activeProfile]) + 2 * (fValue - serviceModel.MinTLOD[serviceModel.activeProfile]) / 5), CultureInfo.InvariantCulture));
                        break;
                    case "CloudRecoveryTLOD":
                        if (serviceModel.CloudRecoveryPlus[serviceModel.activeProfile] && (fValue < 5 || fValue > serviceModel.MaxTLOD[serviceModel.activeProfile] - serviceModel.MinTLOD[serviceModel.activeProfile] - 5)) fValue = serviceModel.CloudRecoveryTLOD[serviceModel.activeProfile];
                        else if (!serviceModel.CloudRecoveryPlus[serviceModel.activeProfile] && (fValue < serviceModel.MinTLOD[serviceModel.activeProfile] + 5 || fValue > serviceModel.MaxTLOD[serviceModel.activeProfile] - 5)) fValue = serviceModel.CloudRecoveryTLOD[serviceModel.activeProfile];
                        break;
                    case "CloudDecreaseGPUPct":
                        if (fValue < 50 || fValue > 100 || fValue < serviceModel.CloudRecoverGPUPct + 10) fValue = serviceModel.CloudDecreaseGPUPct;
                        break;
                    case "CloudRecoverGPUPct":
                        if (fValue < 5 || fValue > 90 || fValue > serviceModel.CloudDecreaseGPUPct - 10) fValue = serviceModel.CloudRecoverGPUPct;
                        break;
                    case "OLODAtBase":
                        if (fValue < 10 || fValue > 1000) fValue = serviceModel.OLODAtBase[serviceModel.activeProfile];
                        break;
                    case "OLODAtTop":
                        if (fValue < 10 || fValue > 1000) fValue = serviceModel.OLODAtTop[serviceModel.activeProfile];
                        break;
                    case "AltOLODBase":
                        if (fValue < 1000 || fValue >= 100000 || fValue > serviceModel.AltOLODTop[serviceModel.activeProfile] - 1) fValue = serviceModel.AltOLODBase[serviceModel.activeProfile];
                        break;
                    case "AltOLODTop":
                        if (fValue < 2000 || fValue > 100000 || fValue < serviceModel.AltOLODBase[serviceModel.activeProfile] + 1) fValue = serviceModel.AltOLODTop[serviceModel.activeProfile];
                        break;
                    case "AltTLODBase":
                        if (fValue < 0 || fValue >= 100000) fValue = serviceModel.AltTLODBase[serviceModel.activeProfile];
                        break;
                    case "AvgDescentRate":
                        if (serviceModel.TLODAutoMethod[serviceModel.activeProfile] == 2)
                        {
                            key = "AltTLODTop";
                            if (fValue < 1000 || fValue > 100000 || fValue < serviceModel.AltTLODBase[serviceModel.activeProfile] + 1) fValue = serviceModel.AltTLODTop[serviceModel.activeProfile];
                        }
                        if (fValue < 200 || fValue > 10000) fValue = serviceModel.AvgDescentRate[serviceModel.activeProfile];
                        break;
                    default:
                        break;
                }
                if (serviceModel.activeProfile != (int)ServiceModel.appProfiles.IFR_Expert && key != "CloudDecreaseGPUPct" && key != "CloudRecoverGPUPct") key += "_VFR";
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

        private void chkCustomAutoOLOD_Checked(object sender, RoutedEventArgs e)
        {

        }
        private void chkCloudRecoveryPlus_Checked(object sender, RoutedEventArgs e)
        {

        }
        private void chkMinTLODExtra_Checked(object sender, RoutedEventArgs e)
        {

        }
        private void AutoFPS_DoubleClick(Object sender, MouseEventArgs e)
        {
            if (serviceModel.windowPanelState == 0 && !serviceModel.UseExpertOptions) serviceModel.windowPanelState = 1;
            if (serviceModel.windowPanelState == 0)
            {
                stkpnlExpertSettings.Visibility = Visibility.Collapsed;
                serviceModel.windowPanelState = 1;
            }
            else if (serviceModel.windowPanelState == 1)
            {
                stkpnlGeneral.Visibility = Visibility.Collapsed;
                serviceModel.windowPanelState = 2;
            }
            else 
            {
                stkpnlGeneral.Visibility = Visibility.Visible;
                if (serviceModel.UseExpertOptions) stkpnlExpertSettings.Visibility = Visibility.Visible;
                serviceModel.windowPanelState = 0;
            }
            serviceModel.SetSetting("windowPanelState", serviceModel.windowPanelState.ToString().ToLower(), true);
        }
    }
}
