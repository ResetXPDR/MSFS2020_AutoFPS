using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace MSFS2020_AutoFPS
{
    public class ServiceModel
    {
        public static readonly int maxProfile = 6;
        private static readonly int BuildConfigVersion = 1;
        public int ConfigVersion { get; set; }
        public bool ServiceExited { get; set; } = false;
        public bool CancellationRequested { get; set; } = false;
        public bool AppEnabled { get; set; } = true;

        public bool IsSimRunning { get; set; } = false;
        public bool IsSessionRunning { get; set; } = false;

        public MemoryManager MemoryAccess { get; set; } = null;
        public int VerticalTrend { get; set; }
        public bool OnGround { get; set; } = true;
        public int groundSpeed { get; set; } = 0;

        public float tlod { get; set; } = 0;
        public float olod { get; set; } = 0;
        public float altAboveGnd { get; set; } = 0;
        public int cloudQ { get; set; }
        public int cloudQ_VR { get; set; }
        public bool VrModeActive { get; set; } 
        public bool ActiveWindowMSFS {  get; set; }

        public const int FPSSettleSeconds = 10;
        public int FPSSettleCounter { get; set; } = FPSSettleSeconds * 2;

        public bool AutoTargetFPS { get; set; } = false;
        public bool UpdateTargetFPS { get; set; } = false;
        public bool ForceAutoFPSCal { get; set; } = true;

        public string ActiveGraphicsMode { get; set; } = "PC";
        public bool ActiveGraphicsModeChanged { get; set; } = false;
        public bool FgModeEnabled { get; set; }
        public bool UseExpertOptions { get; set; }
        public bool FlightTypeIFR { get; set; }
        public bool LogSimValues { get; set; }
        public bool IsAppPriorityFPS { get; set; } = true;
        public int TargetFPS { get; set; }
        public int TargetFPS_PC { get; set; }
        public int TargetFPS_VR { get; set; }
        public int TargetFPS_FG { get; set; }
        public int TargetFPS_PC_VFR { get; set; }
        public int TargetFPS_VR_VFR { get; set; }
        public int TargetFPS_FG_VFR { get; set; }
        public bool PauseMSFSFocusLost { get; set; } 

        public enum appProfiles 
        { 
            NonExpert,
            IFR_Expert,
            VFR_Expert
        }
        public int[] TLODAutoMethod { get; set; } = new int [3];
        public int[] FPSTolerance { get; set; } = new int[3];
        public float[] MinTLOD { get; set; } = new float[3];
        public bool[] MinTLODExtra { get; set; } = new bool[3];
        public float[] MaxTLOD { get; set; } = new float[3];
        public float[] AltTLODBase { get; set; } = new float[3];
        public float[] AltTLODTop { get; set; } = new float[3];
        public float[] AvgDescentRate { get; set; } = new float [3];
        public bool[] DecCloudQ { get; set; } = new bool[3];
        public float[] CloudRecoveryTLOD { get; set; } = new float[3];
        public bool[] CloudRecoveryPlus { get; set; } = new bool[3];
        public bool[] CustomAutoOLOD { get; set; } = new bool[3];
        public float[] OLODAtBase { get; set; } = new float[3];
        public float[] AltOLODBase { get; set; } = new float[3];
        public float[] OLODAtTop { get; set; } = new float[3];
        public float[] AltOLODTop { get; set; } = new float[3];
        public int activeProfile {  get; set; }
 
        public float SimMinLOD { get; set; }
        public float DefaultTLOD { get; set; } = 100;
        public float DefaultTLOD_VR { get; set; } = 100;
        public float DefaultOLOD { get; set; } = 100;
        public float DefaultOLOD_VR { get; set; } = 100;
        public int DefaultCloudQ { get; set; } = 2;
        public int DefaultCloudQ_VR { get; set; } = 2;
        public bool DefaultSettingsRead { get; set; } = false;
        public bool tlod_step { get; set; } = false;
        public bool olod_step { get; set; } = false;
        public bool DecCloudQActive { get; set; }
        public bool MinTLODExtraActive { get; set; }
        public float activeMinTLOD { get; set; }
        public float activeMaxTLOD { get; set; }
        public float activeOLODAtBase { get; set; }
        public float activeOLODAtTop { get; set; }

        public string LogLevel { get; set; }
        public static int MfLvarsPerFrame { get; set; }
        public bool WaitForConnect { get; set; }
        public bool OpenWindow { get; set; }
        public int windowTop {  get; set; }
        public int windowLeft { get; set; }
        public bool RememberWindowPos { get; set; }

        public bool OnTop { get; set; }
        public bool StartMinimized { get; set; }
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
        public bool SimPaused { get; set; } = false;
        public bool AppPaused { get; set; } = false;

        public float gpuUsage { get; set; } = -1;

        public const bool TestVersion = false;

        public ConfigurationFile ConfigurationFile = new();

        public ServiceModel()
        {
            TLODAutoMethod[(int)appProfiles.NonExpert] = 0;
            FPSTolerance[(int)appProfiles.NonExpert] = 5;
            MinTLODExtra[(int)appProfiles.NonExpert] = true;
            AvgDescentRate[(int)appProfiles.NonExpert] = 2000;
            DecCloudQ[(int)appProfiles.NonExpert] = true;
            CloudRecoveryTLOD[(int)appProfiles.NonExpert] = 100;
            CloudRecoveryPlus[(int)appProfiles.NonExpert] = false;
            CustomAutoOLOD[(int)appProfiles.NonExpert] = false;
            AltOLODBase[(int)appProfiles.NonExpert] = 2000;
            AltOLODTop[(int)appProfiles.NonExpert] = 10000;
 
            LoadConfiguration();
        }

        protected void LoadConfiguration()
        {
            ConfigurationFile.LoadConfiguration();

            LogLevel = Convert.ToString(ConfigurationFile.GetSetting("logLevel", "Debug"));
            MfLvarsPerFrame = Convert.ToInt32(ConfigurationFile.GetSetting("mfLvarPerFrame", "15"));
            ConfigVersion = Convert.ToInt32(ConfigurationFile.GetSetting("ConfigVersion", "1"));
            OpenWindow = Convert.ToBoolean(ConfigurationFile.GetSetting("openWindow", "true"));
            RememberWindowPos = Convert.ToBoolean(ConfigurationFile.GetSetting("RememberWindowPos", "true"));
            windowTop = Convert.ToInt32(ConfigurationFile.GetSetting("windowTop", "50"));
            windowLeft = Convert.ToInt32(ConfigurationFile.GetSetting("windowLeft", "50"));
            WaitForConnect = Convert.ToBoolean(ConfigurationFile.GetSetting("waitForConnect", "true"));
            FlightTypeIFR = Convert.ToBoolean(ConfigurationFile.GetSetting("FlightTypeIFR", "true"));
            SimBinary = Convert.ToString(ConfigurationFile.GetSetting("simBinary", "FlightSimulator"));
            SimModule = Convert.ToString(ConfigurationFile.GetSetting("simModule", "WwiseLibPCx64P.dll"));
            UseExpertOptions = Convert.ToBoolean(ConfigurationFile.GetSetting("useExpertOptions", "false"));
            LogSimValues = Convert.ToBoolean(ConfigurationFile.GetSetting("LogSimValues", "false"));
            AutoTargetFPS = Convert.ToBoolean(ConfigurationFile.GetSetting("AutoTargetFPS", "false"));
            OnTop = Convert.ToBoolean(ConfigurationFile.GetSetting("OnTop", "false"));
            StartMinimized = Convert.ToBoolean(ConfigurationFile.GetSetting("StartMinimized", "false"));
            PauseMSFSFocusLost = Convert.ToBoolean(ConfigurationFile.GetSetting("PauseMSFSFocusLost", "false"));
            TargetFPS_PC = Convert.ToInt32(ConfigurationFile.GetSetting("targetFpsPC", "40"));
            TargetFPS_VR = Convert.ToInt32(ConfigurationFile.GetSetting("targetFpsVR", "40"));
            TargetFPS_FG = Convert.ToInt32(ConfigurationFile.GetSetting("targetFpsFG", "40"));
            TargetFPS_PC_VFR = Convert.ToInt32(ConfigurationFile.GetSetting("targetFpsPCVFR", Convert.ToString(TargetFPS_PC, CultureInfo.InvariantCulture)));
            TargetFPS_VR_VFR = Convert.ToInt32(ConfigurationFile.GetSetting("targetFpsVRVFR", Convert.ToString(TargetFPS_VR, CultureInfo.InvariantCulture)));
            TargetFPS_FG_VFR = Convert.ToInt32(ConfigurationFile.GetSetting("targetFpsFGVFR", Convert.ToString(TargetFPS_FG, CultureInfo.InvariantCulture)));
            if (!AutoTargetFPS)
            {
                if (ActiveGraphicsMode == "VR") TargetFPS = FlightTypeIFR ? TargetFPS_VR : TargetFPS_VR_VFR;
                else if (ActiveGraphicsMode == "FG") TargetFPS = FlightTypeIFR ? TargetFPS_FG : TargetFPS_FG_VFR;
                else TargetFPS = FlightTypeIFR ? TargetFPS_PC : TargetFPS_PC_VFR;
            }
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

            if (!UseExpertOptions) activeProfile = (int)appProfiles.NonExpert;
            else if (FlightTypeIFR) activeProfile = (int)appProfiles.IFR_Expert;
            else activeProfile = (int)appProfiles.VFR_Expert;
            TLODAutoMethod[(int)appProfiles.IFR_Expert] = Convert.ToInt32(ConfigurationFile.GetSetting("TLODAutoMethod", "0"));
            FPSTolerance[(int)appProfiles.IFR_Expert] = Convert.ToInt32(ConfigurationFile.GetSetting("FpsTolerance", "5"));
            MinTLOD[(int)appProfiles.IFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("minTLod", "50"));
            MinTLODExtra[(int)appProfiles.IFR_Expert] = Convert.ToBoolean(ConfigurationFile.GetSetting("MinTLODExtra", "false"));
            MaxTLOD[(int)appProfiles.IFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("maxTLod", "200"));
            AltTLODBase[(int)appProfiles.IFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("AltTLODBase", "1000"));
            AltTLODTop[(int)appProfiles.IFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("AltTLODTop", "5000"));
            AvgDescentRate[(int)appProfiles.IFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("AvgDescentRate", "2000"));
            DecCloudQ[(int)appProfiles.IFR_Expert] = Convert.ToBoolean(ConfigurationFile.GetSetting("DecCloudQ", "true"));
            CloudRecoveryTLOD[(int)appProfiles.IFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("CloudRecoveryTLOD", "100"));
            CloudRecoveryPlus[(int)appProfiles.IFR_Expert] = Convert.ToBoolean(ConfigurationFile.GetSetting("CloudRecoveryPlus", "false"));
            CustomAutoOLOD[(int)appProfiles.IFR_Expert] = Convert.ToBoolean(ConfigurationFile.GetSetting("customAutoOLOD", "true"));
            OLODAtBase[(int)appProfiles.IFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("OLODAtBase", "100"));
            OLODAtTop[(int)appProfiles.IFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("OLODAtTop", "20"));
            AltOLODBase[(int)appProfiles.IFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("AltOLODBase", "2000"));
            AltOLODTop[(int)appProfiles.IFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("AltOLODTop", "10000"));

            TLODAutoMethod[(int)appProfiles.VFR_Expert] = Convert.ToInt32(ConfigurationFile.GetSetting("TLODAutoMethod_VFR", TLODAutoMethod[(int)appProfiles.IFR_Expert].ToString()));
            FPSTolerance[(int)appProfiles.VFR_Expert] = Convert.ToInt32(ConfigurationFile.GetSetting("FpsTolerance_VFR", FPSTolerance[(int)appProfiles.IFR_Expert].ToString()));
            MinTLOD[(int)appProfiles.VFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("minTLod_VFR", (MinTLOD[(int)appProfiles.IFR_Expert] * 2).ToString()));
            MinTLODExtra[(int)appProfiles.VFR_Expert] = Convert.ToBoolean(ConfigurationFile.GetSetting("MinTLODExtra_VFR", MinTLODExtra[(int)appProfiles.IFR_Expert].ToString()));
            MaxTLOD[(int)appProfiles.VFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("maxTLod_VFR", (Math.Round(MaxTLOD[(int)appProfiles.IFR_Expert] * 1.5)).ToString()));
            AltTLODBase[(int)appProfiles.VFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("AltTLODBase_VFR", "100"));
            AltTLODTop[(int)appProfiles.VFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("AltTLODTop_VFR", "3000"));
            AvgDescentRate[(int)appProfiles.VFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("AvgDescentRate_VFR", "1000"));
            DecCloudQ[(int)appProfiles.VFR_Expert] = Convert.ToBoolean(ConfigurationFile.GetSetting("DecCloudQ_VFR", DecCloudQ[(int)appProfiles.IFR_Expert].ToString()));
            CloudRecoveryTLOD[(int)appProfiles.VFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("CloudRecoveryTLOD_VFR", CloudRecoveryTLOD[(int)appProfiles.IFR_Expert].ToString()));
            CloudRecoveryPlus[(int)appProfiles.VFR_Expert] = Convert.ToBoolean(ConfigurationFile.GetSetting("CloudRecoveryPlus_VFR", CloudRecoveryPlus[(int)appProfiles.IFR_Expert].ToString()));
            CustomAutoOLOD[(int)appProfiles.VFR_Expert] = Convert.ToBoolean(ConfigurationFile.GetSetting("customAutoOLOD_VFR", CustomAutoOLOD[(int)appProfiles.IFR_Expert].ToString()));
            OLODAtBase[(int)appProfiles.VFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("OLODAtBase_VFR", (Math.Round(OLODAtBase[(int)appProfiles.IFR_Expert] * 1.5)).ToString()));
            OLODAtTop[(int)appProfiles.VFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("OLODAtTop_VFR", OLODAtTop[(int)appProfiles.IFR_Expert].ToString()));
            AltOLODBase[(int)appProfiles.VFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("AltOLODBase_VFR", AltOLODBase[(int)appProfiles.IFR_Expert].ToString()));
            AltOLODTop[(int)appProfiles.VFR_Expert] = Convert.ToSingle(ConfigurationFile.GetSetting("AltOLODTop_VFR", AltOLODTop[(int)appProfiles.IFR_Expert].ToString()));

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

        public void ResetCloudsTLOD(bool ResetTLOD = true)
        {
            MinTLODExtraActive = false;
            if (MemoryAccess != null && DefaultSettingsRead)
            {
                if (DecCloudQActive)
                {
                    if (VrModeActive) MemoryAccess.SetCloudQ_VR(DefaultCloudQ_VR);
                    else MemoryAccess.SetCloudQ(DefaultCloudQ);
                    DecCloudQActive = false;
                }
                if (ResetTLOD && OnGround)
                {
                    if (UseExpertOptions) MemoryAccess.SetTLOD(MinTLOD[activeProfile]);
                    else MemoryAccess.SetTLOD(Math.Max((VrModeActive ? DefaultTLOD_VR : DefaultTLOD) * (FlightTypeIFR ? 0.5f : 1.0f), 10.0f));
                }
            }
        }
            public string CloudQualityText(int CloudQuality)
        {
            if (CloudQuality == 0) return "Low";
            else if (CloudQuality == 1) return "Medium";
            else if (CloudQuality == 2) return "High";
            else if (CloudQuality == 3) return "Ultra";
            else return "n/a";
        }

    }
}