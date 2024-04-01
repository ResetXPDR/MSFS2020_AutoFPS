using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace MSFS2020_AutoFPS
{
    public class LODController
    {
        private MobiSimConnect SimConnect;
        private ServiceModel Model;

        private int[] verticalStats = new int[5];
        private float[] verticalStatsVS = new float[5];
        private int verticalIndex = 0;
        private float vs;
        private const float CloudRecoveryExtraTolerance = 0.15f;
        private const float MinTLODExtraActiveTolerance = 0.15f;
        private const float AutoTargetFPSRecalTolerance = 0.20f;
        private const float VFRMinAltTLODLocks = 100.0f;
        private bool AutoFPSInFlightPhase = false;
        private bool MinTLODExtraActiveForDescentPrimed = false;
        private int CloudRecoveryExtraNonExpert = 0;
        private int CloudRecoveryTickMark = 0;
        private int TotalTicks = 0;
        private float altAboveGnd = 0;
        private float groundSpeed = 0;
        private float altAboveGndLast = 0;
        private float groundSpeedLast = 0;
        private float TLODStep;
        private float newTLOD;
        private float TLODMinAltBand;
        private float MinTLODNoExtra;
        private float MinTLODExtraAmount;
        private float AltTLODBase;
        private const float FPSPriorityBaseAlt = 1000.0f;


        GpuzWrapper gpuz = new GpuzWrapper();
        int gpuzGPULoadSensorIndex;

        public LODController(ServiceModel model)
        {
            Model = model;

            SimConnect = IPCManager.SimConnect;
            SimConnect.SubscribeSimVar("VERTICAL SPEED", "feet per second");
            SimConnect.SubscribeSimVar("PLANE ALT ABOVE GROUND", "feet");
            SimConnect.SubscribeSimVar("PLANE ALT ABOVE GROUND MINUS CG", "feet");
            SimConnect.SubscribeSimVar("SIM ON GROUND", "Bool");
            SimConnect.SubscribeSimVar("GROUND VELOCITY", "knots");
            GetMSFSState();
            if (gpuz.Open())
            {
                Logger.Log(LogLevel.Information, "LODController:LODController", "GPU-Z companion app running");
                for (gpuzGPULoadSensorIndex = 0; gpuz.SensorName(gpuzGPULoadSensorIndex) != String.Empty && gpuz.SensorName(gpuzGPULoadSensorIndex) != "GPU Load"; gpuzGPULoadSensorIndex++) ;
                if (gpuz.SensorName(gpuzGPULoadSensorIndex) != "GPU Load")
                {
                    Logger.Log(LogLevel.Information, "LODController:LODController", "GPU-Z GPU Load sensor not found");
                    gpuzGPULoadSensorIndex = -1;
                }
                else Logger.Log(LogLevel.Information, "LODController:LODController", "GPU-Z GPU Load sensor found");
            }
            else
            {
                Logger.Log(LogLevel.Information, "LODController:LODController", "GPU-Z companion app not running");
                gpuzGPULoadSensorIndex = -1;
            }
            SetActiveLODs();
        }

        private void UpdateVariables()
        {
            vs = SimConnect.ReadSimVar("VERTICAL SPEED", "feet per second");
            Model.OnGround = SimConnect.ReadSimVar("SIM ON GROUND", "Bool") == 1.0f;
            verticalStatsVS[verticalIndex] = vs;
            if (vs >= 8.0f)
                verticalStats[verticalIndex] = 1;
            else if (vs <= -2.0f)
                verticalStats[verticalIndex] = -1;
            else
                verticalStats[verticalIndex] = 0;

            verticalIndex++;
            if (verticalIndex >= verticalStats.Length || verticalIndex >= verticalStatsVS.Length)
                verticalIndex = 0;

            Model.VerticalTrend = VerticalAverage();

            Model.altAboveGnd = (int)(altAboveGnd = SimConnect.ReadSimVar("PLANE ALT ABOVE GROUND", "feet"));
            if (Model.altAboveGnd == 0 && !Model.OnGround)
                Model.altAboveGnd = (int)SimConnect.ReadSimVar("PLANE ALT ABOVE GROUND MINUS CG", "feet");

            Model.groundSpeed = (int)(groundSpeed = SimConnect.ReadSimVar("GROUND VELOCITY", "knots"));
            GetMSFSState();
            if (gpuzGPULoadSensorIndex >= 0) Model.gpuUsage = (float)gpuz.SensorValue(gpuzGPULoadSensorIndex);
            else Model.gpuUsage = -1;

            if (groundSpeed > 1 && altAboveGnd == altAboveGndLast && groundSpeed == groundSpeedLast) Model.SimPaused = true;
            else Model.SimPaused = false;
            altAboveGndLast = altAboveGnd;
            groundSpeedLast = groundSpeed;
            if ((!Model.ActiveWindowMSFS && ((Model.UseExpertOptions && Model.PauseMSFSFocusLost) || (!Model.UseExpertOptions && Model.FgModeEnabled))) || Model.SimPaused) Model.AppPaused = true;
            else Model.AppPaused = false;
            SetActiveLODs();
        }

        public void RunTick()
        {
            UpdateVariables();

            if (Model.UseExpertOptions) AltTLODBase = Model.AltTLODBase[Model.activeProfile];
            else
            {
                AltTLODBase = Model.AltTLODBase[(int)ServiceModel.appProfiles.NonExpert] = Model.FlightTypeIFR ? 1000.0f : 100.0f;
                Model.CloudRecoveryTLOD[Model.activeProfile] = (float)Math.Min(Math.Round(2 * (Model.activeMinTLOD + Model.activeMaxTLOD) / 5), Model.activeMinTLOD + 50.0f) + CloudRecoveryExtraNonExpert;
            }
            TLODStep = Math.Max(2.0f, Model.FPSTolerance[Model.activeProfile]);
            TLODMinAltBand = Model.AvgDescentRate[Model.activeProfile] / 60 * ((Model.activeMaxTLOD - MinTLODNoExtra) / TLODStep);

            if (Model.altAboveGnd >= AltTLODBase + TLODMinAltBand) Model.IsAppPriorityFPS = true; 
            else Model.IsAppPriorityFPS = false;

            if (Model.altAboveGnd >= AltTLODBase + TLODMinAltBand)
            {
                AutoFPSInFlightPhase = true;
                if (!Model.UseExpertOptions || Model.MinTLODExtra[Model.activeProfile])
                {
                    if (!MinTLODExtraActiveForDescentPrimed && Model.tlod > Model.MinTLOD[Model.activeProfile] + MinTLODExtraAmount) 
                        MinTLODExtraActiveForDescentPrimed = true;
                    Model.MinTLODExtraActive = false;
                }
            }
            else if ((!Model.UseExpertOptions || Model.MinTLODExtra[Model.activeProfile]) && MinTLODExtraActiveForDescentPrimed && Model.tlod > Model.MinTLOD[Model.activeProfile] + MinTLODExtraAmount) 
            {
                if (Model.TLODAutoMethod[Model.activeProfile] != 2)
                {
                    Model.MinTLODExtraActive = true;
                    SetActiveLODs();
                }
                MinTLODExtraActiveForDescentPrimed = false;
            }

            if (Model.AutoTargetFPS && Model.OnGround && Model.groundSpeed == 0 && AutoFPSInFlightPhase)
            {
                Model.FPSSettleCounter = ServiceModel.FPSSettleSeconds;
                AutoFPSInFlightPhase = false;
                Model.ForceAutoFPSCal = true;
                Model.ResetCloudsTLOD(); 
                Logger.Log(LogLevel.Information, "LODController:RunTick", "Recalibrating Auto Target FPS on arrival at new location");
            }

            if (!Model.AppPaused && (Model.FPSSettleCounter == 0 || Model.TLODAutoMethod[Model.activeProfile] == 2))
            {
                float deltaFPS = GetAverageFPS() - Model.TargetFPS;

                if (Model.AutoTargetFPS && !(Model.FgModeEnabled && !Model.ActiveWindowMSFS) && (Model.ForceAutoFPSCal || (!(Model.FgModeEnabled && !Model.ActiveWindowMSFS) && ((deltaFPS <= -Model.TargetFPS * AutoTargetFPSRecalTolerance && (!Model.DecCloudQ[Model.activeProfile] || Model.DecCloudQActive) && Model.tlod == Model.activeMinTLOD))))) 
                {
                    if (!Model.ForceAutoFPSCal && (!Model.UseExpertOptions || Model.MinTLODExtra[Model.activeProfile]) && Model.MinTLODExtraActive) Model.MinTLODExtraActive = false;
                    else
                    {
                        Model.TargetFPS = (int)(GetAverageFPS() * ((Model.FlightTypeIFR ? 0.95f : 0.90f) - Math.Min(0.1f, Model.altAboveGnd / 30000)));
                        Model.UpdateTargetFPS = true;
                        Model.ForceAutoFPSCal = false;
                        if (deltaFPS <= -Model.TargetFPS * AutoTargetFPSRecalTolerance && (!Model.DecCloudQ[Model.activeProfile] || Model.DecCloudQActive) && Model.tlod == Model.activeMinTLOD) Logger.Log(LogLevel.Information, "LODController:UpdateVariables", "FPS too " + (Model.tlod == Model.activeMinTLOD ? "low" : "high") + " for auto target FPS settings. Auto Target FPS updated to " + $"{Model.TargetFPS}");
                        else
                        {
                            deltaFPS = GetAverageFPS() - Model.TargetFPS;
                            Logger.Log(LogLevel.Information, "LODController:UpdateVariables", "Auto Target FPS set to " + $"{Model.TargetFPS}");
                        }
                    }
                }

                if (Model.TLODAutoMethod[Model.activeProfile] == 2)
                {
                    if (Model.altAboveGnd < Model.AltTLODBase[Model.activeProfile]) newTLOD = Model.MinTLOD[Model.activeProfile];
                    else if (Model.altAboveGnd > Model.AltTLODTop[Model.activeProfile]) newTLOD = Model.MaxTLOD[Model.activeProfile];
                    else
                    {
                        newTLOD = Model.MinTLOD[Model.activeProfile] + (Model.MaxTLOD[Model.activeProfile] - Model.MinTLOD[Model.activeProfile]) * (Model.altAboveGnd - Model.AltTLODBase[Model.activeProfile]) / (Model.AltTLODTop[Model.activeProfile] - Model.AltTLODBase[Model.activeProfile]);
                        if (Math.Abs(newTLOD - Model.tlod) < 5) newTLOD = Model.tlod;
                        else if (Math.Abs(newTLOD - Model.tlod) > 10) newTLOD = Model.tlod + Math.Sign(newTLOD - Model.tlod) * 10;
                        else newTLOD = Model.tlod + Math.Sign(newTLOD - Model.tlod) * 5;
                    }
                }
                else if ((!Model.UseExpertOptions || Model.MinTLODExtra[Model.activeProfile]) && !(Model.FgModeEnabled && !Model.ActiveWindowMSFS) && !Model.DecCloudQActive && !Model.MinTLODExtraActive && Model.OnGround && deltaFPS >= Model.TargetFPS * MinTLODExtraActiveTolerance)
                {
                    Model.MinTLODExtraActive = true;
                    SetActiveLODs();
                    newTLOD = Model.activeMinTLOD;
                    if (newTLOD != Model.tlod)
                    {
                        Model.MemoryAccess.SetTLOD(newTLOD);
                        Model.tlod = newTLOD;
                        Model.tlod_step = true;
                        Model.FPSSettleCounter = ServiceModel.FPSSettleSeconds * 2;
                    }
                }
                else
                {
                    if (Model.TLODAutoMethod[Model.activeProfile] == 0) newTLOD = Model.tlod + Math.Sign(deltaFPS) * (Math.Min((float)Math.Pow(Math.Abs(deltaFPS) / (GetAverageFPS() > 1 ? GetAverageFPS() : 1) * TLODStep * 10, 1.4f), TLODStep * 2));
                    else newTLOD = Model.tlod + Math.Sign(deltaFPS) * TLODStep * (Math.Abs(deltaFPS) >= Model.TargetFPS * 2 * Model.FPSTolerance[Model.activeProfile] / 100 ? 2 : 1) * (Model.altAboveGnd < FPSPriorityBaseAlt && !Model.OnGround ? (float)Model.altAboveGnd / FPSPriorityBaseAlt : 1);
                    if (Model.altAboveGnd < AltTLODBase) newTLOD = Model.activeMinTLOD;
                    else newTLOD = Math.Min(Model.activeMinTLOD + (Model.activeMaxTLOD - MinTLODNoExtra) * (Model.altAboveGnd - AltTLODBase) / TLODMinAltBand, newTLOD);
                }
                newTLOD = (float)Math.Round(Math.Min(Model.activeMaxTLOD, Math.Max(Model.activeMinTLOD, newTLOD)));
                if (Math.Abs(Model.tlod - newTLOD) >= 1)
                {
                    Model.MemoryAccess.SetTLOD(newTLOD);
                    Model.tlod = newTLOD;
                    Model.tlod_step = true;
                }
                else Model.tlod_step = false;

                if ((!Model.UseExpertOptions || Model.MinTLODExtra[Model.activeProfile]) && Model.MinTLODExtraActive && !Model.OnGround && deltaFPS <= -Model.TargetFPS * Model.FPSTolerance[Model.activeProfile] / 100)
                    Model.MinTLODExtraActive = false;

                if (Model.TLODAutoMethod[Model.activeProfile] != 2 && Model.DecCloudQ[Model.activeProfile] && !Model.DecCloudQActive && Model.tlod == Model.activeMinTLOD && deltaFPS <= -Model.TargetFPS * Model.FPSTolerance[Model.activeProfile] / 100)
                {
                    if (Model.VrModeActive && Model.DefaultCloudQ_VR >= 1)
                    {
                        Model.MemoryAccess.SetCloudQ_VR(Model.DefaultCloudQ_VR - 1);
                        Model.DecCloudQActive = true;
                    }
                    if (!Model.VrModeActive && Model.DefaultCloudQ >= 1)
                    {
                        Model.MemoryAccess.SetCloudQ(Model.DefaultCloudQ - 1);
                        Model.DecCloudQActive = true;
                    }
                    CloudRecoveryTickMark = TotalTicks;
                }

                if (Model.DecCloudQ[Model.activeProfile] && Model.DecCloudQActive && ((deltaFPS >= Model.TargetFPS * CloudRecoveryExtraTolerance) || (newTLOD >= (Model.UseExpertOptions && Model.CloudRecoveryPlus[Model.activeProfile] ? Model.activeMinTLOD + Model.CloudRecoveryTLOD[Model.activeProfile] : Model.CloudRecoveryTLOD[Model.activeProfile]) && deltaFPS >= 0)))  
                {
                    Model.ResetCloudsTLOD(false); 
                    if ((!Model.UseExpertOptions || Model.MinTLODExtra[Model.activeProfile]) && !Model.MinTLODExtraActive && Model.OnGround) Model.FPSSettleCounter = ServiceModel.FPSSettleSeconds;
                    if (!Model.UseExpertOptions && Model.altAboveGnd > AltTLODBase && TotalTicks < CloudRecoveryTickMark + 30) CloudRecoveryExtraNonExpert += 20;
                }

                if (!Model.UseExpertOptions || Model.CustomAutoOLOD[Model.activeProfile])
                {
                    float newOLOD;
                    if (Model.altAboveGnd < Model.AltOLODBase[Model.activeProfile]) newOLOD = Model.activeOLODAtBase;
                    else if (Model.altAboveGnd > Model.AltOLODTop[Model.activeProfile]) newOLOD = Model.activeOLODAtTop;
                    else
                    {
                        newOLOD = Model.activeOLODAtBase + (Model.activeOLODAtTop - Model.activeOLODAtBase) * (Model.altAboveGnd - Model.AltOLODBase[Model.activeProfile]) / (Model.AltOLODTop[Model.activeProfile] - Model.AltOLODBase[Model.activeProfile]);
                        if (Math.Abs(newOLOD - Model.olod) < 5) newOLOD = Model.olod;
                        else if (Math.Abs(newOLOD - Model.olod) > 10) newOLOD = Model.olod + Math.Sign(newOLOD - Model.olod) * 10;
                        else newOLOD = Model.olod + Math.Sign(newOLOD - Model.olod) * 5;
                    }
                    if (newOLOD != Model.olod && Math.Abs(newOLOD - Model.olod) >= 1)
                    {
                        Model.MemoryAccess.SetOLOD(newOLOD);
                        Model.olod = newOLOD;
                        Model.olod_step = true;
                    }
                    else Model.olod_step = false;
                }
                else
                {
                    Model.olod_step = true;
                    if (Model.VrModeActive && Model.olod != Model.DefaultOLOD_VR)
                        Model.MemoryAccess.SetOLOD_VR(Model.DefaultOLOD_VR);
                    else if (!Model.VrModeActive && Model.olod != Model.DefaultOLOD)
                        Model.MemoryAccess.SetOLOD_PC(Model.DefaultOLOD);
                    else Model.olod_step = false;
                }
            }
            else if (!Model.AppPaused && Model.FPSSettleCounter != 0) --Model.FPSSettleCounter;
            TotalTicks++;
        }
        public int VerticalAverage()
        {
            return verticalStats.Sum();
        }
        public float VSAverage()
        {
            return verticalStatsVS.Average();
        }

        public void SetActiveLODs()
        {
            if (Model.UseExpertOptions)
            {
                MinTLODNoExtra = Model.MinTLOD[Model.activeProfile];
                MinTLODExtraAmount = (float)Math.Min(50.0f, 0.5 * (Model.MaxTLOD[Model.activeProfile] - MinTLODNoExtra));
                Model.activeMinTLOD = MinTLODNoExtra + (Model.MinTLODExtraActive && Model.TLODAutoMethod[Model.activeProfile] != 2 ? MinTLODExtraAmount : 0);
                Model.activeMaxTLOD = Model.MaxTLOD[Model.activeProfile];
                Model.activeOLODAtBase = Model.OLODAtBase[Model.activeProfile];
                Model.activeOLODAtTop = Model.OLODAtTop[Model.activeProfile];
            }
            else
            {
                float defaultTLOD = Model.VrModeActive ? Model.DefaultTLOD_VR : Model.DefaultTLOD;
                float defaultOLOD = Model.VrModeActive ? Model.DefaultOLOD_VR : Model.DefaultOLOD;
                MinTLODNoExtra = Math.Max(defaultTLOD * (Model.FlightTypeIFR ? 0.5f : 1.0f), 10.0f);
                Model.activeMaxTLOD = Model.MaxTLOD[(int)ServiceModel.appProfiles.NonExpert] = defaultTLOD * (Model.FlightTypeIFR ? 2.0f : 3.0f);
                MinTLODExtraAmount = (float)Math.Min(50.0f, 0.5 * (Model.activeMaxTLOD - MinTLODNoExtra));
                Model.activeMinTLOD = Model.MinTLOD[(int)ServiceModel.appProfiles.NonExpert] = MinTLODNoExtra + (Model.MinTLODExtraActive ? MinTLODExtraAmount : 0);
                Model.activeOLODAtBase = Model.OLODAtBase[(int)ServiceModel.appProfiles.NonExpert] = Model.FlightTypeIFR ? defaultOLOD : defaultOLOD * 1.5f;
                Model.activeOLODAtTop = Model.OLODAtTop[(int)ServiceModel.appProfiles.NonExpert] = Math.Max(Model.activeOLODAtBase * 0.1f, 10.0f);
            }
        }
        public float GetAverageFPS()
        {
            if (Model.FgModeEnabled)
                return (float)Math.Round(IPCManager.SimConnect.GetAverageFPS() * 2.0f);
            else
                return (float)Math.Round(IPCManager.SimConnect.GetAverageFPS());
        }
        private void GetMSFSState()
        {
            Model.tlod = Model.MemoryAccess.GetTLOD_PC();
            Model.olod = Model.MemoryAccess.GetOLOD_PC();
            Model.cloudQ = Model.MemoryAccess.GetCloudQ_PC();
            Model.cloudQ_VR = Model.MemoryAccess.GetCloudQ_VR();
            Model.VrModeActive = Model.MemoryAccess.IsVrModeActive();
            Model.FgModeEnabled = Model.MemoryAccess.IsFgModeEnabled();
            if (!Model.ActiveWindowMSFS && Model.MemoryAccess.IsActiveWindowMSFS() && (Model.FgModeEnabled || (Model.UseExpertOptions && Model.PauseMSFSFocusLost))) Model.FPSSettleCounter = ServiceModel.FPSSettleSeconds;
            Model.ActiveWindowMSFS = Model.MemoryAccess.IsActiveWindowMSFS();
            string ActiveGraphicsMode = Model.ActiveGraphicsMode;
            if (Model.VrModeActive)
            {
                Model.ActiveGraphicsMode = "VR";
                if (!Model.AutoTargetFPS) Model.TargetFPS = Model.FlightTypeIFR ? Model.TargetFPS_VR : Model.TargetFPS_VR_VFR;
            }
            else if (Model.FgModeEnabled)
            {
                Model.ActiveGraphicsMode = "FG";
                if (!Model.AutoTargetFPS) Model.TargetFPS = Model.FlightTypeIFR ? Model.TargetFPS_FG : Model.TargetFPS_FG_VFR;
            }
            else
            {
                Model.ActiveGraphicsMode = "PC";
                if (!Model.AutoTargetFPS) Model.TargetFPS = Model.FlightTypeIFR ? Model.TargetFPS_PC : Model.TargetFPS_PC_VFR;
            }
            if (Model.ActiveGraphicsMode != ActiveGraphicsMode)
            {
                Model.FPSSettleCounter = ServiceModel.FPSSettleSeconds;
                Model.ActiveGraphicsModeChanged = true;
                if (Model.AutoTargetFPS) Model.ForceAutoFPSCal = true;
                Model.ResetCloudsTLOD();
            }
        }
    }
}
