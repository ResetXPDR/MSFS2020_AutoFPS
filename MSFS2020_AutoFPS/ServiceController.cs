using System;
using System.Diagnostics.Eventing.Reader;
using System.Threading;

namespace MSFS2020_AutoFPS
{
    public class ServiceController
    {
        protected ServiceModel Model;
        protected int Interval = 1000;

        public ServiceController(ServiceModel model)
        {
            this.Model = model;
        }

        public void Run()
        {
            try
            {
                    Logger.Log(LogLevel.Information, "ServiceController:Run", $"Service starting ...");
                while (!Model.CancellationRequested && Model.AppEnabled)
                {
                    if (Wait())
                    {
                        ServiceLoop();
                    }
                    else
                    {
                        if (!IPCManager.IsSimRunning())
                        {
                            Model.CancellationRequested = true;
                            Model.ServiceExited = true;
                            Logger.Log(LogLevel.Critical, "ServiceController:Run", $"Session aborted, Retry not possible - exiting Program");
                            return;
                        }
                        else
                        {
                            Reset();
                            Logger.Log(LogLevel.Information, "ServiceController:Run", $"Session aborted, Retry possible - Waiting for new Session");
                        }
                    }
                }
                if (!Model.AppEnabled) Logger.Log(LogLevel.Critical, "ServiceController:Run", "MSFS compatibility test failed - app disabled.");
              
                IPCManager.CloseSafe();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ServiceController:Run", $"Critical Exception occured: {ex.Source} - {ex.Message}");
            }
        }

        protected bool Wait()
        {
            if (!IPCManager.WaitForSimulator(Model))
            {
                Model.IsSimRunning = false;
                return false;
            }
            else
                Model.IsSimRunning = true;

            if (!IPCManager.WaitForConnection(Model))
                return false;

            if (!IPCManager.WaitForSessionReady(Model))
            {
                Model.IsSessionRunning = false;
                return false;
            }
            else
                Model.IsSessionRunning = true;

            return true;
        }

        protected void Reset()
        {
            try
            {
                IPCManager.SimConnect?.Disconnect();
                IPCManager.SimConnect = null;
                Model.IsSessionRunning = false;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ServiceController:Reset", $"Exception during Reset: {ex.Source} - {ex.Message}");
            }
        }

        protected void ServiceLoop()
        {
            Model.MemoryAccess = new MemoryManager(Model);
            var lodController = new LODController(Model);
            bool NormalStartup = true;
            if (Model.MemoryAccess.MemoryWritesAllowed())
            {
                Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", "Starting Service Loop");
                if (Model.ConfigurationFile.SettingExists("defaultTLOD"))
                {
                    Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", "MSFS or MSFS2020_AutoFPS did not exit properly last session. Getting default MSFS settings from MSFS2020_AutoFPS config file.");
                    NormalStartup = false;
                }
                else Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", "Normal startup detected. Getting default MSFS settings from MSFS."); 
                Model.DefaultTLOD = Convert.ToSingle(Model.ConfigurationFile.GetSetting("defaultTLOD", Model.MemoryAccess.GetTLOD_PC().ToString("F0")));
                Model.DefaultTLOD_VR = Convert.ToSingle(Model.ConfigurationFile.GetSetting("defaultTLOD_VR", Model.MemoryAccess.GetTLOD_VR().ToString("F0")));
                Model.DefaultOLOD = Convert.ToSingle(Model.ConfigurationFile.GetSetting("defaultOLOD", Model.MemoryAccess.GetOLOD_PC().ToString("F0")));
                Model.DefaultOLOD_VR = Convert.ToSingle(Model.ConfigurationFile.GetSetting("defaultOLOD_VR", Model.MemoryAccess.GetOLOD_VR().ToString("F0")));
                Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", $"Initial LODs PC {Model.DefaultTLOD} / {Model.DefaultOLOD} and VR {Model.DefaultTLOD_VR} / {Model.DefaultOLOD_VR}");
                Model.DefaultCloudQ = Convert.ToInt32(Model.ConfigurationFile.GetSetting("defaultCloudQ", Model.MemoryAccess.GetCloudQ_PC().ToString("F0")));
                Model.DefaultCloudQ_VR = Convert.ToInt32(Model.ConfigurationFile.GetSetting("defaultCloudQ_VR", Model.MemoryAccess.GetCloudQ_VR().ToString("F0")));
                Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", $"Initial cloud quality PC {Model.CloudQualityText(Model.DefaultCloudQ)} / VR {Model.CloudQualityText(Model.DefaultCloudQ_VR)}");
                if (!Model.UseExpertOptions)
                {
                    if (Model.VrModeActive)
                    {
                        float MinTLOD = Math.Max(Model.DefaultTLOD_VR * 0.5f, 10.0f);
                        Model.MemoryAccess.SetTLOD(MinTLOD);
                        Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", $"Setting TLOD Min on ground " + $"{MinTLOD}");
                    }
                    else
                    {
                        float MinTLOD = Math.Max(Model.DefaultTLOD * 0.5f, 10.0f);
                        Model.MemoryAccess.SetTLOD(MinTLOD);
                        Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", $"Setting TLOD Min on ground " + $"{MinTLOD}");
                    }
                }
                else
                {
                    Model.MemoryAccess.SetTLOD(Model.MinTLOD[Model.activeProfile]);
                    Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", $"Setting TLOD Min on ground " + $"{Model.MinTLOD[Model.activeProfile]}");
                }
                if (Model.CustomAutoOLOD[Model.activeProfile] && Model.UseExpertOptions)
                {
                    Model.MemoryAccess.SetOLOD(Model.OLODAtBase[Model.activeProfile]);
                    Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", $"Setting OLOD @ Base on ground " + $"{Model.OLODAtBase[Model.activeProfile]}");
                }
                Model.FPSSettleCounter = ServiceModel.FPSSettleSeconds * 2;
                Model.MinTLODExtraActive = false;
                Model.DecCloudQActive = false;
                Model.DefaultSettingsRead = true;
                if (!NormalStartup) Model.ResetCloudsTLOD(false);
                while (!Model.CancellationRequested && IPCManager.IsSimRunning() && IPCManager.IsCamReady())
                {
                    try
                    {
                        lodController.RunTick();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Critical, "ServiceController:ServiceLoop", $"Critical Exception during ServiceLoop() {ex.GetType()} {ex.Message} {ex.Source}");
                    }
                    
                    Thread.Sleep(Interval);
                }
                Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", "ServiceLoop ended");

                if (true && IPCManager.IsSimRunning())
                {
                    Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", $"Sim still running, resetting LODs to {Model.DefaultTLOD} / {Model.DefaultOLOD} and VR {Model.DefaultTLOD_VR} / {Model.DefaultOLOD_VR}");
                    Model.MemoryAccess.SetTLOD_PC(Model.DefaultTLOD);
                    Model.MemoryAccess.SetTLOD_VR(Model.DefaultTLOD_VR);
                    Model.MemoryAccess.SetOLOD_PC(Model.DefaultOLOD);
                    Model.MemoryAccess.SetOLOD_VR(Model.DefaultOLOD_VR);
                    Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", $"Sim still running, resetting cloud quality to {Model.CloudQualityText(Model.DefaultCloudQ)} / VR {Model.CloudQualityText(Model.DefaultCloudQ_VR)}");
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
                        Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", "Default MSFS settings reset successful. Removed back up default MSFS settings from MSFS2020_AutoFPS config file.");
                    }
                    else Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", "Default MSFS settings reset failed. Retained back up default MSFS settings in MSFS2020_AutoFPS config file.");
                }
            }
            else Model.AppEnabled = false;

            Model.IsSessionRunning = false;

            Model.MemoryAccess = null;
        }
    }
}
