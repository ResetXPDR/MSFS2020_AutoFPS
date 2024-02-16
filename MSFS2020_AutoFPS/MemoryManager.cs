using System;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Shapes;

namespace MSFS2020_AutoFPS
{
    public class MemoryManager
    {
        private ServiceModel Model;

        private long addrTLOD;
        private long addrOLOD;
        private long addrTLOD_VR;
        private long addrOLOD_VR;
        private long addrCloudQ;
        private long addrCloudQ_VR;
        private long addrVrMode;
        private long addrFgMode;
        private long offsetPointerAnsioFilter = -0x18;
        private long offsetWaterWaves = 0x3C;
        private bool allowMemoryWrites = false;
        private bool isDX12 = false;

        public MemoryManager(ServiceModel model)
        {
            try
            {
                this.Model = model;

                MemoryInterface.Attach(Model.SimBinary);

                GetActiveDXVersion();
                Logger.Log(LogLevel.Debug, "MemoryManager:MemoryManager", $"Trying offsetModuleBase: 0x{model.OffsetModuleBase.ToString("X8")}");
                GetMSFSMemoryAddresses();
                if (addrTLOD > 0) MemoryBoundaryTest();
                if (!allowMemoryWrites)
                {
                    Logger.Log(LogLevel.Debug, "MemoryManager:MemoryManager", $"Boundary tests failed - possible MSFS memory map change");
                    ModuleOffsetSearch();
                }
                else Logger.Log(LogLevel.Debug, "MemoryManager:MemoryManager", $"Boundary tests passed - memory writes enabled");
                if (!allowMemoryWrites) Logger.Log(LogLevel.Debug, "MemoryManager:MemoryManager", $"Boundary test failed - memory writes disabled");
                GetMSFSMemoryAddresses();

            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:MemoryManager", $"Exception {ex}: {ex.Message}");
            }
        }

        private void ModuleOffsetSearch()
        {
            long offsetBase = 0x00400000;
            bool offsetFound = false;
            long offset = 0;

            long moduleBase = MemoryInterface.GetModuleAddress(Model.SimModule);

            // 0x004AF3C8 was muumimorko version offsetBase
            // 0x004B2368 was Fragtality version offsetBase
            Logger.Log(LogLevel.Debug, "MemoryManager:ModuleOffsetSearch", $"OffsetModuleBase search started");
            
            while (offset < 0x100000 && !offsetFound)
            {
                addrTLOD = MemoryInterface.ReadMemory<long>(moduleBase + offsetBase + offset) + Model.OffsetPointerMain;
                if (addrTLOD > 0)
                {
                    addrTLOD_VR = MemoryInterface.ReadMemory<long>(addrTLOD) + Model.OffsetPointerTlodVr;
                    addrTLOD = MemoryInterface.ReadMemory<long>(addrTLOD) + Model.OffsetPointerTlod;
                    addrOLOD_VR = addrTLOD_VR + Model.OffsetPointerOlod;
                    addrOLOD = addrTLOD + Model.OffsetPointerOlod;
                    addrCloudQ = addrTLOD + Model.OffsetPointerCloudQ;
                    addrCloudQ_VR = addrCloudQ + Model.OffsetPointerCloudQVr;
                    addrVrMode = addrTLOD - Model.OffsetPointerVrMode;
                    addrFgMode = addrTLOD - Model.OffsetPointerFgMode;
                    MemoryBoundaryTest();
                }
                if (allowMemoryWrites) offsetFound = true;
                else offset++;
            }
            if (offsetFound)
            {
                Model.SetSetting("offsetModuleBase", "0x" + (offsetBase + offset).ToString("X8"));
                Logger.Log(LogLevel.Debug, "MemoryManager:ModuleOffsetSearch", $"New offsetModuleBase found and saved: 0x{(offsetBase + offset).ToString("X8")}");
            }
            else Logger.Log(LogLevel.Debug, "MemoryManager:ModuleOffsetSearch", $"OffsetModuleBase not found after {offset} iterations");

        }
        private void MemoryBoundaryTest()
        {
            // Boundary check a few known setting memory addresses to see if any fail which likely indicates MSFS memory map has changed
            if (GetTLOD_PC() < 10 || GetTLOD_PC() > 400 || GetTLOD_VR() < 10 || GetTLOD_VR() > 400
                || GetOLOD_PC() < 10 || GetOLOD_PC() > 400 || GetOLOD_VR() < 10 || GetOLOD_VR() > 400
                || GetCloudQ_PC() < 0 || GetCloudQ_PC() > 3 || GetCloudQ_VR() < 0 || GetCloudQ_VR() > 3
                || MemoryInterface.ReadMemory<int>(addrVrMode) < 0 || MemoryInterface.ReadMemory<int>(addrVrMode) > 1
                || MemoryInterface.ReadMemory<int>(addrTLOD + offsetPointerAnsioFilter) < 1 || MemoryInterface.ReadMemory<int>(addrTLOD + offsetPointerAnsioFilter) > 16
                || !(MemoryInterface.ReadMemory<int>(addrTLOD + offsetWaterWaves) == 128 || MemoryInterface.ReadMemory<int>(addrTLOD + offsetWaterWaves) == 256 || MemoryInterface.ReadMemory<int>(addrTLOD + offsetWaterWaves) == 512))
                allowMemoryWrites = false;
            else allowMemoryWrites = true;
 
        }
        private void GetMSFSMemoryAddresses()
        {
            long moduleBase = MemoryInterface.GetModuleAddress(Model.SimModule);

            addrTLOD = MemoryInterface.ReadMemory<long>(moduleBase + Model.OffsetModuleBase) + Model.OffsetPointerMain;
            if (addrTLOD > 0)
            {
                addrTLOD_VR = MemoryInterface.ReadMemory<long>(addrTLOD) + Model.OffsetPointerTlodVr;
                addrTLOD = MemoryInterface.ReadMemory<long>(addrTLOD) + Model.OffsetPointerTlod;
                addrOLOD_VR = addrTLOD_VR + Model.OffsetPointerOlod;
                addrOLOD = addrTLOD + Model.OffsetPointerOlod;
                addrCloudQ = addrTLOD + Model.OffsetPointerCloudQ;
                addrCloudQ_VR = addrCloudQ + Model.OffsetPointerCloudQVr;
                addrVrMode = addrTLOD - Model.OffsetPointerVrMode;
                addrFgMode = addrTLOD - Model.OffsetPointerFgMode;
                if (allowMemoryWrites)
                {
                    Logger.Log(LogLevel.Debug, "MemoryManager:GetMSFSMemoryAddresses", $"Address TLOD: 0x{addrTLOD:X} / {addrTLOD}");
                    Logger.Log(LogLevel.Debug, "MemoryManager:GetMSFSMemoryAddresses", $"Address OLOD: 0x{addrOLOD:X} / {addrOLOD}");
                    Logger.Log(LogLevel.Debug, "MemoryManager:GetMSFSMemoryAddresses", $"Address CloudQ: 0x{addrCloudQ:X} / {addrCloudQ}");
                    Logger.Log(LogLevel.Debug, "MemoryManager:GetMSFSMemoryAddresses", $"Address TLOD VR: 0x{addrTLOD_VR:X} / {addrTLOD_VR}");
                    Logger.Log(LogLevel.Debug, "MemoryManager:GetMSFSMemoryAddresses", $"Address OLOD VR: 0x{addrOLOD_VR:X} / {addrOLOD_VR}");
                    Logger.Log(LogLevel.Debug, "MemoryManager:GetMSFSMemoryAddresses", $"Address CloudQ VR: 0x{addrCloudQ_VR:X} / {addrCloudQ_VR}");
                    Logger.Log(LogLevel.Debug, "MemoryManager:GetMSFSMemoryAddresses", $"Address VrMode: 0x{addrVrMode:X} / {addrVrMode}");
                    Logger.Log(LogLevel.Debug, "MemoryManager:GetMSFSMemoryAddresses", $"Address FgMode: 0x{addrFgMode:X} / {addrFgMode}");
                }
            }
        }
        private void GetActiveDXVersion()
        {
            string filecontents;
            string MSFSOptionsFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft Flight Simulator\UserCfg.opt";
            if (File.Exists(MSFSOptionsFile))
            {
                StreamReader sr = new StreamReader(MSFSOptionsFile);
                filecontents = sr.ReadToEnd();
                if (filecontents.Contains("PreferD3D12 1")) isDX12 = true;
                sr.Close();
                Logger.Log(LogLevel.Debug, "MemoryManager:GetActiveDXVersion", $"Steam MSFS version detected - " + (isDX12 ? "DX12" : "DX11"));
            }
            else
            {
                MSFSOptionsFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\UserCfg.opt";
                if (File.Exists(MSFSOptionsFile))
                {
                    StreamReader sr = new StreamReader(MSFSOptionsFile);
                    filecontents = sr.ReadToEnd();
                    if (filecontents.Contains("PreferD3D12 1")) isDX12 = true;
                    sr.Close();
                    Logger.Log(LogLevel.Debug, "MemoryManager:GetActiveDXVersion", $"MS Store MSFS version detected - " + (isDX12 ? "DX12" : "DX11"));
                }
            }

        }

        public bool MemoryWritesAllowed()
        {
            return allowMemoryWrites;
        }
        public bool IsVrModeActive()
        {
            try
            {
                return MemoryInterface.ReadMemory<int>(addrVrMode) == 1; 
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:IsVrModeActive", $"Exception {ex}: {ex.Message}");
            }

            return false;
        }
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private bool IsActiveWindowMSFS()
        {
            const int nChars = 256;
            string activeWindowTitle;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                activeWindowTitle = Buff.ToString();
                if (activeWindowTitle.Length > 26 && activeWindowTitle.Substring(0, 26) == "Microsoft Flight Simulator")
                    return true;
            }
            return false;
        }
        public bool IsFgModeActive()
        {
            try
            {
                if (isDX12 && !Model.MemoryAccess.IsVrModeActive() && IsActiveWindowMSFS()) 
                    return MemoryInterface.ReadMemory<byte>(addrFgMode) == 1;
                else return false;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:IsFgModeActive", $"Exception {ex}: {ex.Message}");
            }

            return false;
        }

        public float GetTLOD_PC()
        {
            try
            {
                return (float)Math.Round(MemoryInterface.ReadMemory<float>(addrTLOD) * 100.0f);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:GetTLOD", $"Exception {ex}: {ex.Message}");
            }

            return 0.0f;
        }

        public float GetTLOD_VR()
        {
            try
            {
                return (float)Math.Round(MemoryInterface.ReadMemory<float>(addrTLOD_VR) * 100.0f);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:GetTLOD_VR", $"Exception {ex}: {ex.Message}");
            }

            return 0.0f;
        }

        public float GetOLOD_PC()
        {
            try
            {
                return (float)Math.Round(MemoryInterface.ReadMemory<float>(addrOLOD) * 100.0f);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:GetOLOD", $"Exception {ex}: {ex.Message}");
            }

            return 0.0f;
        }

         public float GetOLOD_VR()
        {
            try
            {
                return (float)Math.Round(MemoryInterface.ReadMemory<float>(addrOLOD_VR) * 100.0f);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:GetOLOD_VR", $"Exception {ex}: {ex.Message}");
            }

            return 0.0f;
        }

        public int GetCloudQ_PC()
        {
            try
            {
                return MemoryInterface.ReadMemory<int>(addrCloudQ);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:GetCloudQ", $"Exception {ex}: {ex.Message}");
            }

            return -1;
        }
        public int GetCloudQ_VR()
        {
            try
            {
                return MemoryInterface.ReadMemory<int>(addrCloudQ_VR);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:GetCloudQ VR", $"Exception {ex}: {ex.Message}");
            }

            return -1;
        }
        public void SetTLOD(float value)
        {
            if (allowMemoryWrites)
            {   
                SetTLOD_PC(value);
                SetTLOD_VR(value);
            }
        }
        public void SetTLOD_PC(float value)
        {
            try
            {
                MemoryInterface.WriteMemory<float>(addrTLOD, value / 100.0f);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:SetTLOD", $"Exception {ex}: {ex.Message}");
            }
        }
        public void SetTLOD_VR(float value)
        {
            try
            {
                MemoryInterface.WriteMemory<float>(addrTLOD_VR, value / 100.0f);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:SetTLOD VR", $"Exception {ex}: {ex.Message}");
            }
        }
        public void SetOLOD(float value)
        {
            if (allowMemoryWrites)
            {
                SetOLOD_PC(value);
                SetOLOD_VR(value);
            }
        }
        public void SetOLOD_PC(float value)
        {
            try
            {
                MemoryInterface.WriteMemory<float>(addrOLOD, value / 100.0f);
                
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:SetOLOD", $"Exception {ex}: {ex.Message}");
            }
        }
        public void SetOLOD_VR(float value)
        {
            try
            {
                MemoryInterface.WriteMemory<float>(addrOLOD_VR, value / 100.0f);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryManager:SetOLOD VR", $"Exception {ex}: {ex.Message}");
            }
        }
        public void SetCloudQ(int value)
        {
            if (allowMemoryWrites)
            {
                try
                {
                    MemoryInterface.WriteMemory<int>(addrCloudQ, value);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, "MemoryManager:SetCloudQ", $"Exception {ex}: {ex.Message}");
                }
            }
        }
        public void SetCloudQ_VR(int value)
        {
            if (allowMemoryWrites)
            {
                try
                {
                    MemoryInterface.WriteMemory<int>(addrCloudQ_VR, value);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, "MemoryManager:SetCloudQ VR", $"Exception {ex}: {ex.Message}");
                }
            }
        }
    }
}
