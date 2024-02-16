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

        protected int editPairTLOD = -1;
        protected int editPairOLOD = -1;

        public MainWindow(NotifyIconViewModel notifyModel, ServiceModel serviceModel)
        {
            InitializeComponent();
            this.notifyModel = notifyModel;
            this.serviceModel = serviceModel;

             string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            assemblyVersion = assemblyVersion[0..assemblyVersion.LastIndexOf('.')];
            Title += " (" + assemblyVersion + (serviceModel.TestVersion ? "-concept_demo" : "")+ ")";

            //FillIndices(dgTlodPairs);
            //FillIndices(dgOlodPairs);

             timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += OnTick;

            string latestAppVersionStr = GetFinalRedirect("https://github.com/ResetXPDR/MSFS2020_AutoFPS/releases/latest");
            lblappUrl.Visibility = Visibility.Hidden;
            if (int.TryParse(assemblyVersion.Replace(".", ""), CultureInfo.InvariantCulture, out int currentAppVersion) &&  latestAppVersionStr != null && latestAppVersionStr.Length > 70)
            { 
                latestAppVersionStr = latestAppVersionStr.Substring(latestAppVersionStr.Length - 5, 5);
                if (int.TryParse(latestAppVersionStr.Replace(".", ""), CultureInfo.InvariantCulture, out int LatestAppVersion))
                { 
                    if ((serviceModel.TestVersion && LatestAppVersion >= currentAppVersion) || LatestAppVersion > currentAppVersion)
                    {
                        lblsimCompatible.Content = "Newer app version " + (latestAppVersionStr) + " now available";
                        lblsimCompatible.Foreground = new SolidColorBrush(Colors.Green);
                        lblappUrl.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        if (serviceModel.TestVersion)
                        {
                            lblsimCompatible.Content = latestAppVersionStr + " version is latest formal release. Check link works";
                            lblsimCompatible.Foreground = new SolidColorBrush(Colors.Green);
                            lblappUrl.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            lblsimCompatible.Content = "Latest app version is installed";
                            lblsimCompatible.Foreground = new SolidColorBrush(Colors.Green);
                        }
                    }
                }   
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
            //chkUseTargetFPS.IsChecked = serviceModel.UseTargetFPS;
            //cbProfile.SelectedIndex = serviceModel.SelectedProfile;
            //dgTlodPairs.ItemsSource = serviceModel.PairsTLOD[serviceModel.SelectedProfile].ToDictionary(x => x.Item1, x => x.Item2);
            //dgOlodPairs.ItemsSource = serviceModel.PairsOLOD[serviceModel.SelectedProfile].ToDictionary(x => x.Item1, x => x.Item2);
            txtTargetFPS.Text = Convert.ToString(serviceModel.TargetFPS, CultureInfo.CurrentUICulture);
            txtFPSTolerance.Text = Convert.ToString(serviceModel.FPSTolerance, CultureInfo.CurrentUICulture);
            //txtDecreaseTlod.Text = Convert.ToString(serviceModel.DecreaseTLOD, CultureInfo.CurrentUICulture);
            //txtDecreaseOlod.Text = Convert.ToString(serviceModel.DecreaseOLOD, CultureInfo.CurrentUICulture);
            txtMinTLod.Text = Convert.ToString(serviceModel.MinTLOD, CultureInfo.CurrentUICulture);
            txtMaxTLod.Text = Convert.ToString(serviceModel.MaxTLOD, CultureInfo.CurrentUICulture);
            //txtConstraintTicks.Text = Convert.ToString(serviceModel.ConstraintTicks, CultureInfo.CurrentUICulture);
            //txtConstraintDelayTicks.Text = Convert.ToString(serviceModel.ConstraintDelayTicks, CultureInfo.CurrentUICulture);
            chkDecCloudQ.IsChecked = serviceModel.DecCloudQ;
            txtCloudRecoveryTLOD.Text = Convert.ToString(serviceModel.CloudRecoveryTLOD, CultureInfo.CurrentUICulture);
            txtLodStepMaxInc.Text = Convert.ToString(serviceModel.LodStepMaxInc, CultureInfo.CurrentUICulture);
            txtLodStepMaxDec.Text = Convert.ToString(serviceModel.LodStepMaxDec, CultureInfo.CurrentUICulture);
        }

        protected static void FillIndices(DataGrid dataGrid)
        {
            DataGridTextColumn column0 = new()
            {
                Header = "#",
                Width = 16
            };

            Binding bindingColumn0 = new()
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1),
                Converter = new RowToIndexConvertor()
            };

            column0.Binding = bindingColumn0;

            dataGrid.Columns.Add(column0);
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
            if (serviceModel.MemoryAccess != null && serviceModel.MemoryAccess.IsFgModeActive())
                return IPCManager.SimConnect.GetAverageFPS() * 2.0f;
            else
                return IPCManager.SimConnect.GetAverageFPS();
        }
        protected void UpdateLiveValues()
        {
            if (IPCManager.SimConnect != null && IPCManager.SimConnect.IsConnected)
                lblSimFPS.Content = GetAverageFPS().ToString("F0");
            else
                lblSimFPS.Content = "n/a";

            if (serviceModel.MemoryAccess != null)
            {
                lblappUrl.Visibility = Visibility.Hidden;
                lblSimTLOD.Content = serviceModel.MemoryAccess.GetTLOD_PC().ToString("F0");
                lblSimOLOD.Content = serviceModel.MemoryAccess.GetOLOD_PC().ToString("F0");
                if (serviceModel.MemoryAccess.IsVrModeActive())
                {
                    lblSimCloudQs.Content = CloudQualityLabel(serviceModel.MemoryAccess.GetCloudQ_VR());
                    lblIsVR.Content = "VR Mode active";
                }
                else
                {
                    lblSimCloudQs.Content = CloudQualityLabel(serviceModel.MemoryAccess.GetCloudQ_PC());
                    lblIsVR.Content = "PC Mode" + (serviceModel.MemoryAccess.IsFgModeActive() ? " & FG" : "") + " active";
                }
     
                if (serviceModel.MemoryAccess.MemoryWritesAllowed())
                {
                    lblsimCompatible.Visibility = Visibility.Hidden;

                }
                else
                {
                    lblsimCompatible.Content = "Incompatible MSFS version - Sim Values read only";
                    lblsimCompatible.Foreground = new SolidColorBrush(Colors.Red);
                }

            }
            else
            {
                lblSimTLOD.Content = "n/a";
                lblSimOLOD.Content = "n/a";
                lblSimCloudQs.Content = "n/a";
            }

            if (serviceModel.UseTargetFPS && serviceModel.IsSessionRunning)
            {
                if (GetAverageFPS() < serviceModel.TargetFPS)
                    lblSimFPS.Foreground = new SolidColorBrush(Colors.Red);
                else
                    lblSimFPS.Foreground = new SolidColorBrush(Colors.DarkGreen);
            }
            else
            {
                lblSimFPS.Foreground = new SolidColorBrush(Colors.Black);
            }

            lblSimTLOD.Foreground = new SolidColorBrush(Colors.Black);
            lblSimCloudQs.Foreground = new SolidColorBrush(Colors.Black);

            if (serviceModel.MemoryAccess != null)
            {
                if (serviceModel.MemoryAccess.GetTLOD_PC() == serviceModel.MinTLOD) lblSimTLOD.Foreground = new SolidColorBrush(Colors.Red);
                else if (serviceModel.MemoryAccess.GetTLOD_PC() == serviceModel.MaxTLOD) lblSimTLOD.Foreground = new SolidColorBrush(Colors.Green);
                else if (serviceModel.tlod_step) lblSimTLOD.Foreground = new SolidColorBrush(Colors.Orange);
            }
            if (serviceModel.DecCloudQ && serviceModel.DecCloudQActive) lblSimCloudQs.Foreground = new SolidColorBrush(Colors.Red);
        }

        protected void UpdateAircraftValues()
        {
            if (IPCManager.SimConnect != null && IPCManager.SimConnect.IsConnected)
            {
                var simConnect = IPCManager.SimConnect;
                lblPlaneAGL.Content = simConnect.ReadSimVar("PLANE ALT ABOVE GROUND", "feet").ToString("F0");
                lblPlaneVS.Content = (simConnect.ReadSimVar("VERTICAL SPEED", "feet per second") * 60.0f).ToString("F0");
                //if (serviceModel.OnGround)
                //    lblVSTrend.Content = "Ground";
                //else if (serviceModel.VerticalTrend > 0)
                //    lblVSTrend.Content = "Climb";
                //else if (serviceModel.VerticalTrend < 0)
                //    lblVSTrend.Content = "Descent";
                //else
                //    lblVSTrend.Content = "Cruise";
            }
            else
            {
                lblPlaneAGL.Content = "n/a";
                lblPlaneVS.Content = "n/a";
                //lblVSTrend.Content = "n/a";
            }
        }

        protected static void UpdateIndex(DataGrid grid, List<(float, float)> pairs, int index)
        {
            if (index >= 0 && index < pairs.Count)
                grid.SelectedIndex = index;
        }

        protected void OnTick(object sender, EventArgs e)
        {
            UpdateStatus();
            UpdateLiveValues();
            UpdateAircraftValues();

            //if (serviceModel.IsSessionRunning)
            //{
            //    UpdateIndex(dgTlodPairs, serviceModel.PairsTLOD[serviceModel.SelectedProfile], serviceModel.CurrentPairTLOD);
            //    UpdateIndex(dgOlodPairs, serviceModel.PairsOLOD[serviceModel.SelectedProfile], serviceModel.CurrentPairOLOD);
            //}
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

        //private void chkUseTargetFPS_Click(object sender, RoutedEventArgs e)
        //{
        //    serviceModel.SetSetting("useTargetFps", chkUseTargetFPS.IsChecked.ToString().ToLower());
        //    LoadSettings();
        //}

        private void chkOpenWindow_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("openWindow", chkOpenWindow.IsChecked.ToString().ToLower());
            LoadSettings();
        }

        private void chkDecCloudQ_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("DecCloudQ", chkDecCloudQ.IsChecked.ToString().ToLower());
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
                case "txtDecreaseTlod":
                    key = "decreaseTlod";
                    break;
                case "txtDecreaseOlod":
                    key = "decreaseOlod";
                    break;
                case "txtConstraintTicks":
                    key = "constraintTicks";
                    intValue = true;
                    break;
                case "txtConstraintDelayTicks":
                    key = "constraintDelayTicks";
                    intValue = true;
                    zeroAllowed = true;
                    break;
                case "txtCloudRecoveryTLOD":
                    key = "CloudRecoveryTLOD";
                    intValue = true;
                    zeroAllowed = true;
                    break;
                case "txtMinTLod":
                    key = "minTLod";
                    break;
                case "txtMaxTLod":
                    key = "maxTLod";
                    break;
                case "txtLodStepMaxInc":
                    key = "LodStepMaxInc";
                    intValue = true;
                    break;
                case "txtLodStepMaxDec":
                    key = "LodStepMaxDec";
                    intValue = true;
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
                serviceModel.SetSetting(key, Convert.ToString(iValue, CultureInfo.InvariantCulture));
            }

            if (!intValue && float.TryParse(sender.Text, new RealInvariantFormat(sender.Text), out float fValue))
            {
                if (notNegative)
                    fValue = Math.Abs(fValue);
                serviceModel.SetSetting(key, Convert.ToString(fValue, CultureInfo.InvariantCulture));
            }

            LoadSettings();
        }

        private static void SetPairTextBox(DataGrid sender, TextBox alt, TextBox value, ref int index)
        {
            if (sender.SelectedIndex == -1 || sender.SelectedItem == null)
                return;

            var item = (KeyValuePair<float, float>)sender.SelectedItem;
            alt.Text = Convert.ToString((int)item.Key, CultureInfo.CurrentUICulture);
            value.Text = Convert.ToString(item.Value, CultureInfo.CurrentUICulture);
            index = sender.SelectedIndex;
        }

        //private void dgTlodPairs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    SetPairTextBox(dgTlodPairs, txtTlodAlt, txtTlodValue, ref editPairTLOD);
        //}

        //private void dgOlodPairs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    SetPairTextBox(dgOlodPairs, txtOlodAlt, txtOlodValue, ref editPairOLOD);
        //}

        private void ChangeLodPair(ref int pairIndex, TextBox alt, TextBox value, List<(float, float)> pairs)
        {
            if (pairIndex == -1)
                return;

            if (pairIndex == 0 && alt.Text != "0")
                alt.Text = "0";

            if (int.TryParse(alt.Text, CultureInfo.InvariantCulture, out int agl) && float.TryParse(value.Text, new RealInvariantFormat(value.Text), out float lod)
                && pairIndex < pairs.Count && agl >= 0 && lod >= serviceModel.SimMinLOD)
            {
                var oldPair = pairs[pairIndex];
                pairs[pairIndex] = (agl, lod);
                if (pairs.Count(pair => pair.Item1 == agl) > 1)
                    pairs[pairIndex] = oldPair;
                serviceModel.SavePairs();
            }

            LoadSettings();
            alt.Text = "";
            value.Text = "";
            pairIndex = -1;
        }

        //private void btnTlodChange_Click(object sender, RoutedEventArgs e)
        //{
        //    ChangeLodPair(ref editPairTLOD, txtTlodAlt, txtTlodValue, serviceModel.PairsTLOD[serviceModel.SelectedProfile]);
        //}

        //private void btnOlodChange_Click(object sender, RoutedEventArgs e)
        //{
        //    ChangeLodPair(ref editPairOLOD, txtOlodAlt, txtOlodValue, serviceModel.PairsOLOD[serviceModel.SelectedProfile]);
        //}

        private void AddLodPair(ref int pairIndex, TextBox alt, TextBox value, List<(float, float)> pairs)
        {
            if (int.TryParse(alt.Text, CultureInfo.InvariantCulture, out int agl) && float.TryParse(value.Text, new RealInvariantFormat(value.Text), out float lod)
                && agl >= 0 && lod >= serviceModel.SimMinLOD
                && !pairs.Any(pair => pair.Item1 == agl))
            {
                pairs.Add((agl, lod));
                ServiceModel.SortTupleList(pairs);
                serviceModel.SavePairs();
            }

            LoadSettings();
            alt.Text = "";
            value.Text = "";
            pairIndex = -1;
        }

        //private void btnTlodAdd_Click(object sender, RoutedEventArgs e)
        //{
        //    AddLodPair(ref editPairTLOD, txtTlodAlt, txtTlodValue, serviceModel.PairsTLOD[serviceModel.SelectedProfile]);
        //}

        //private void btnOlodAdd_Click(object sender, RoutedEventArgs e)
        //{
        //    AddLodPair(ref editPairOLOD, txtOlodAlt, txtOlodValue, serviceModel.PairsOLOD[serviceModel.SelectedProfile]);
        //}

        private void RemoveLoadPair(ref int pairIndex, TextBox alt, TextBox value, List<(float, float)> pairs)
        {
            if (pairIndex < 1 || pairIndex >= pairs.Count)
                return;

            pairs.RemoveAt(pairIndex);
            ServiceModel.SortTupleList(pairs);
            serviceModel.SavePairs();
            LoadSettings();
            alt.Text = "";
            value.Text = "";
            pairIndex = -1;
        }

        //private void btnTlodRemove_Click(object sender, RoutedEventArgs e)
        //{
        //    RemoveLoadPair(ref editPairTLOD, txtTlodAlt, txtTlodValue, serviceModel.PairsTLOD[serviceModel.SelectedProfile]);
        //}

        //private void btnOlodRemove_Click(object sender, RoutedEventArgs e)
        //{
        //    RemoveLoadPair(ref editPairOLOD, txtOlodAlt, txtOlodValue, serviceModel.PairsOLOD[serviceModel.SelectedProfile]);
        //}

        private void txtLodStepMaxInc_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void txtLodStepMaxDec_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void chkDecCloudQ_Checked(object sender, RoutedEventArgs e)
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
    }
 
    public class RowToIndexConvertor : MarkupExtension, IValueConverter
    {
        static RowToIndexConvertor convertor;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is DataGridRow row)
            {
                return row.GetIndex();
            }
            else
            {
                return -1;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            convertor ??= new RowToIndexConvertor();

            return convertor;
        }


        public RowToIndexConvertor()
        {

        }
    }
}
