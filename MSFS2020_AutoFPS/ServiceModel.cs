using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

namespace MSFS2020_AutoFPS
{
    public class ServiceModel
    {
        public static readonly int maxProfile = 6;
        private static readonly int BuildConfigVersion = 1;
        public int ConfigVersion { get; set; }
        public bool ServiceExited { get; set; } = false;
        public bool CancellationRequested { get; set; } = false;

        public bool IsSimRunning { get; set; } = false;
        public bool IsSessionRunning { get; set; } = false;

        public MemoryManager MemoryAccess { get; set; } = null;
        public int VerticalTrend { get; set; }
        public bool OnGround { get; set; } = true;
        public float tlod { get; set; } = 0;
        public int cloudQ { get; set; }
        public int cloudQ_VR { get; set; }
        public bool VrModeActive { get; set; } 
        public bool ActiveWindowMSFS {  get; set; }

        public const int FPSSettleSeconds = 6;
        public int FPSSettleCounter { get; set; } = FPSSettleSeconds;
        public string ActiveGraphicsMode { get; set; } = "PC";
        public bool ActiveGraphicsModeChanged { get; set; } = false;
        public bool FgModeActive { get; set; }
        public bool UseExpertOptions { get; set; }
        public bool IsAppPriorityFPS { get; set; } = true;
        public int TargetFPS { get; set; }
        public int TargetFPS_PC { get; set; }
        public int TargetFPS_VR { get; set; }
        public int TargetFPS_FG { get; set; }
        public int FPSTolerance { get; set; }
        public int CloudRecoveryTLOD { get; set; }
        public bool DecCloudQActive { get; set; }
        public bool PauseMSFSFocusLost { get; set; } = true;
        public bool TLODMinGndLanding { get; set; }
        public float MinTLOD { get; set; }
        public float MaxTLOD { get; set; }
        public float SimMinLOD { get; set; }
        public float DefaultTLOD { get; set; } = 100;
        public float DefaultTLOD_VR { get; set; } = 100;
        public int DefaultCloudQ { get; set; } = 2;
        public int DefaultCloudQ_VR { get; set; } = 2;
        public bool DefaultSettingsRead { get; set; } = false;
        public int LodStepMaxInc { get; set; }
        public int LodStepMaxDec { get; set; }
        public bool tlod_step { get; set; } = false;
        public bool olod_step { get; set; } = false;


        public string LogLevel { get; set; }
        public static int MfLvarsPerFrame { get; set; }
        public bool WaitForConnect { get; set; }
        public bool OpenWindow { get; set; }
        public bool DecCloudQ { get; set; }
        public bool LodStepMax { get; set; }
        public string SimBinary { get; set; }
        public string SimModule { get; set; }
        public long OffsetModuleBase { get; set; }
        public long OffsetPointerMain { get; set; }
        public long OffsetPointerTlod { get; set; }
        public long OffsetPointerTlodVr { get; set; }
        public long OffsetPointerOlod { get; set; }
        public long OffsetPointerCloudQ { get; set; }
        public long OffsetPointerCloudQVr { get; set; }
        public long OffsetPointerVrMode { get; set; }
        public long OffsetPointerFgMode { get; set; }
        public bool TestVersion { get; set; } = false;

        protected ConfigurationFile ConfigurationFile = new();

        public ServiceModel()
        {
            LoadConfiguration();
        }

        protected void LoadConfiguration()
        {
            ConfigurationFile.LoadConfiguration();

            LogLevel = Convert.ToString(ConfigurationFile.GetSetting("logLevel", "Debug"));
            MfLvarsPerFrame = Convert.ToInt32(ConfigurationFile.GetSetting("mfLvarPerFrame", "15"));
            ConfigVersion = Convert.ToInt32(ConfigurationFile.GetSetting("ConfigVersion", "1"));
            WaitForConnect = Convert.ToBoolean(ConfigurationFile.GetSetting("waitForConnect", "true"));
            OpenWindow = Convert.ToBoolean(ConfigurationFile.GetSetting("openWindow", "true"));
            DecCloudQ = Convert.ToBoolean(ConfigurationFile.GetSetting("DecCloudQ", "true"));
            TLODMinGndLanding = Convert.ToBoolean(ConfigurationFile.GetSetting("TLODMinGndLanding", "true"));
            SimBinary = Convert.ToString(ConfigurationFile.GetSetting("simBinary", "FlightSimulator"));
            SimModule = Convert.ToString(ConfigurationFile.GetSetting("simModule", "WwiseLibPCx64P.dll"));
            UseExpertOptions = Convert.ToBoolean(ConfigurationFile.GetSetting("useExpertOptions", "false"));
            PauseMSFSFocusLost = Convert.ToBoolean(ConfigurationFile.GetSetting("PauseMSFSFocusLost", "true"));
            TargetFPS_PC = Convert.ToInt32(ConfigurationFile.GetSetting("targetFpsPC", "40"));
            TargetFPS_VR = Convert.ToInt32(ConfigurationFile.GetSetting("targetFpsVR", "40"));
            TargetFPS_FG = Convert.ToInt32(ConfigurationFile.GetSetting("targetFpsFG", "40"));
            if (ActiveGraphicsMode == "VR") TargetFPS = TargetFPS_VR;
            else if (ActiveGraphicsMode == "FG") TargetFPS = TargetFPS_FG;
            else TargetFPS = TargetFPS_PC;
            TargetFPS = Convert.ToInt32(ConfigurationFile.GetSetting("targetFps", "40"));
            FPSTolerance = Convert.ToInt32(ConfigurationFile.GetSetting("FpsTolerance", "5"));
            CloudRecoveryTLOD = Convert.ToInt32(ConfigurationFile.GetSetting("CloudRecoveryTLOD", "100"));
            MinTLOD = Convert.ToSingle(ConfigurationFile.GetSetting("minTLod", "50"), new RealInvariantFormat(ConfigurationFile.GetSetting("minTLod", "50")));
            MaxTLOD = Convert.ToSingle(ConfigurationFile.GetSetting("maxTLod", "200"), new RealInvariantFormat(ConfigurationFile.GetSetting("maxTLod", "200")));
            OffsetModuleBase = Convert.ToInt64(ConfigurationFile.GetSetting("offsetModuleBase", "0x004B2368"), 16);
            OffsetPointerMain = Convert.ToInt64(ConfigurationFile.GetSetting("offsetPointerMain", "0x3D0"), 16);
            OffsetPointerTlod = Convert.ToInt64(ConfigurationFile.GetSetting("offsetPointerTlod", "0xC"), 16);
            OffsetPointerTlodVr = Convert.ToInt64(ConfigurationFile.GetSetting("offsetPointerTlodVr", "0x114"), 16);
            OffsetPointerOlod = Convert.ToInt64(ConfigurationFile.GetSetting("offsetPointerOlod", "0xC"), 16);
            OffsetPointerCloudQ = Convert.ToInt64(ConfigurationFile.GetSetting("offsetPointerCloudQ", "0x44"), 16);
            OffsetPointerCloudQVr = Convert.ToInt64(ConfigurationFile.GetSetting("offsetPointerCloudQVr", "0x108"), 16);
            OffsetPointerVrMode = Convert.ToInt64(ConfigurationFile.GetSetting("offsetPointerVrMode", "0x1C"), 16);
            OffsetPointerFgMode = Convert.ToInt64(ConfigurationFile.GetSetting("offsetPointerFgMode", "0x4A"), 16);
            SimMinLOD = Convert.ToSingle(ConfigurationFile.GetSetting("simMinLod", "10"), new RealInvariantFormat(ConfigurationFile.GetSetting("simMinLod", "10")));
 
            if (ConfigVersion < BuildConfigVersion)
            {
                //CHANGE SETTINGS IF NEEDED, Example:

                SetSetting("ConfigVersion", Convert.ToString(BuildConfigVersion));
            }
        }
 
        public string GetSetting(string key, string defaultValue = "")
        {
            return ConfigurationFile[key] ?? defaultValue;
        }

        public void SetSetting(string key, string value, bool noLoad = false)
        {
            ConfigurationFile[key] = value;
            if (!noLoad)
                LoadConfiguration();
        }


    }
}