using System;
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
                while (!Model.CancellationRequested)
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
            Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", "Starting Service Loop");
            Model.DefaultTLOD = Model.MemoryAccess.GetTLOD_PC();
            Model.DefaultTLOD_VR = Model.MemoryAccess.GetTLOD_VR();
            Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", $"Initial TLODs " + $"PC / VR {Model.DefaultTLOD} / {Model.DefaultTLOD_VR}");
            Model.DefaultCloudQ = Model.MemoryAccess.GetCloudQ_PC();
            Model.DefaultCloudQ_VR = Model.MemoryAccess.GetCloudQ_VR();
            Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", $"Initial cloud quality PC {Model.DefaultCloudQ} / VR {Model.DefaultCloudQ_VR}");
            if (Model.UseExpertOptions || Model.TLODMinGndLanding)
            {
                Model.MemoryAccess.SetTLOD(Model.MinTLOD);
                Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", $"Setting Min TLOD on ground " + $"{Model.MinTLOD}");
            }
            Model.DefaultSettingsRead = true;
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
                Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", $"Sim still running, resetting TLODs to PC / VR {Model.DefaultTLOD} / {Model.DefaultTLOD_VR}");
                Model.MemoryAccess.SetTLOD_PC(Model.DefaultTLOD);
                Model.MemoryAccess.SetTLOD_VR(Model.DefaultTLOD_VR);
                Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", $"Sim still running, resetting cloud quality to {Model.DefaultCloudQ} / VR {Model.DefaultCloudQ_VR}");
                Model.MemoryAccess.SetCloudQ(Model.DefaultCloudQ);
                Model.MemoryAccess.SetCloudQ_VR(Model.DefaultCloudQ_VR);
            }

            Model.IsSessionRunning = false;

            Model.MemoryAccess = null;
        }
    }
}
