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
        public float AltLead { get; set; }
        public bool OnGround { get; set; } = true;
        public bool ForceEvaluation { get; set; } = false;

        public int SelectedProfile { get; set; } = 0;
        public List<List<(float, float)>> PairsTLOD { get; set; }
        public int CurrentPairTLOD;
        public List<List<(float, float)>> PairsOLOD { get; set; }
        public int CurrentPairOLOD;
        public bool fpsMode { get; set; }
        public bool UseExpertOptions { get; set; }
        public bool IsAppPriorityFPS { get; set; } = true;
        public int TargetFPS { get; set; }
        public int FPSTolerance { get; set; }
        public int CloudRecoveryTLOD { get; set; }
        public bool DecCloudQActive { get; set; }
        public bool TLODMinGndLanding { get; set; }
        public int ConstraintTicks { get; set; }
        public int ConstraintDelayTicks { get; set; }
        public float DecreaseTLOD { get; set; }
        public float DecreaseOLOD { get; set; }
        public float MinTLOD { get; set; }
        public float MaxTLOD { get; set; }
        public float SimMinLOD { get; set; }
        public float DefaultTLOD { get; set; } = 100;
        public float DefaultTLOD_VR { get; set; } = 100;
        public float DefaultOLOD { get; set; } = 100;
        public float DefaultOLOD_VR { get; set; } = 100;
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
            CurrentPairTLOD = 0;
            CurrentPairOLOD = 0;
            LoadConfiguration();
        }

        protected void LoadConfiguration()
        {
            ConfigurationFile.LoadConfiguration();

            TestVersion = true;
            LogLevel = Convert.ToString(ConfigurationFile.GetSetting("logLevel", "Debug"));
            MfLvarsPerFrame = Convert.ToInt32(ConfigurationFile.GetSetting("mfLvarPerFrame", "15"));
            ConfigVersion = Convert.ToInt32(ConfigurationFile.GetSetting("ConfigVersion", "1"));
            WaitForConnect = Convert.ToBoolean(ConfigurationFile.GetSetting("waitForConnect", "true"));
            OpenWindow = Convert.ToBoolean(ConfigurationFile.GetSetting("openWindow", "true"));
            DecCloudQ = Convert.ToBoolean(ConfigurationFile.GetSetting("DecCloudQ", "true"));
            TLODMinGndLanding = Convert.ToBoolean(ConfigurationFile.GetSetting("TLODMinGndLanding", "false"));
            SimBinary = Convert.ToString(ConfigurationFile.GetSetting("simBinary", "FlightSimulator"));
            SimModule = Convert.ToString(ConfigurationFile.GetSetting("simModule", "WwiseLibPCx64P.dll"));
            UseExpertOptions = Convert.ToBoolean(ConfigurationFile.GetSetting("useExpertOptions", "false"));
            TargetFPS = Convert.ToInt32(ConfigurationFile.GetSetting("targetFps", "40"));
            FPSTolerance = Convert.ToInt32(ConfigurationFile.GetSetting("FpsTolerance", "5"));
            CloudRecoveryTLOD = Convert.ToInt32(ConfigurationFile.GetSetting("CloudRecoveryTLOD", "100"));
            ConstraintTicks = Convert.ToInt32(ConfigurationFile.GetSetting("constraintTicks", "60"));
            ConstraintDelayTicks = Convert.ToInt32(ConfigurationFile.GetSetting("constraintDelayTicks", "1"));
            DecreaseTLOD = Convert.ToSingle(ConfigurationFile.GetSetting("decreaseTlod", "50"), new RealInvariantFormat(ConfigurationFile.GetSetting("decreaseTlod", "50")));
            DecreaseOLOD = Convert.ToSingle(ConfigurationFile.GetSetting("decreaseOlod", "50"), new RealInvariantFormat(ConfigurationFile.GetSetting("decreaseOlod", "50")));
            MinTLOD = Convert.ToSingle(ConfigurationFile.GetSetting("minTLod", "50"), new RealInvariantFormat(ConfigurationFile.GetSetting("minTLod", "50")));
            MaxTLOD = Convert.ToSingle(ConfigurationFile.GetSetting("maxTLod", "200"), new RealInvariantFormat(ConfigurationFile.GetSetting("maxTLod", "200")));
            LodStepMaxInc = Convert.ToInt32(ConfigurationFile.GetSetting("LodStepMaxInc", "5"));
            LodStepMaxDec = Convert.ToInt32(ConfigurationFile.GetSetting("LodStepMaxDec", "5"));
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
 
            SelectedProfile = Convert.ToInt32(ConfigurationFile.GetSetting("selectedProfile", "0"));
            PairsTLOD = new();
            PairsOLOD = new();

            for (int i = 0; i < maxProfile; i++)
            {
                PairsTLOD.Add(LoadPairs(ConfigurationFile.GetSetting($"tlodPairs{i}", "0:100|1500:150|5000:200")));
                PairsOLOD.Add(LoadPairs(ConfigurationFile.GetSetting($"olodPairs{i}", "0:100|2500:50|7500:10")));
            }
            CurrentPairTLOD = 0;
            CurrentPairOLOD = 0;
            ForceEvaluation = true;


            if (ConfigVersion < BuildConfigVersion)
            {
                //CHANGE SETTINGS IF NEEDED, Example:

                SetSetting("ConfigVersion", Convert.ToString(BuildConfigVersion));
            }
        }

        public static List<(float, float)> LoadPairs(string settings)
        {
            List<(float, float)> pairsList = new();

            string[] strPairs = settings.Split('|');
            int alt;
            float lod;
            foreach (string pair in strPairs)
            {
                string[] parts = pair.Split(':');
                alt = Convert.ToInt32(parts[0]);
                lod = Convert.ToSingle(parts[1], new RealInvariantFormat(parts[1]));
                pairsList.Add((alt, lod));
            }
            SortTupleList(pairsList);

            return pairsList;
        }

        public static void SortTupleList(List<(float, float)> pairsList)
        {
            pairsList.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        }

        public static string CreateLodString(List<(float, float)> pairsList)
        {
            string result = "";
            bool first = true;

            foreach (var pair in pairsList)
            {
                if (first)
                    first = false;
                else
                    result += "|";

                result += $"{Convert.ToString((int)pair.Item1)}:{Convert.ToString(pair.Item2, CultureInfo.InvariantCulture)}";
            }

            return result;
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

        public void SavePairs()
        {
            for (int i = 0; i < maxProfile; i++)
            {
                ConfigurationFile[$"tlodPairs{i}"] = CreateLodString(PairsTLOD[i]);
                ConfigurationFile[$"olodPairs{i}"] = CreateLodString(PairsOLOD[i]);
            }
            LoadConfiguration();
        }
    }
}