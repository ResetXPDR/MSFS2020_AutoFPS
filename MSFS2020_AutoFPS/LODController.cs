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
using System.Windows;
using System.Windows.Documents;
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
        private float[] gpuUsage = new float[5];
        private int gpuIndex = 0;
        private const float CloudRecoveryExtraTolerance = 0.15f;
        private const float MinTLODExtraActiveTolerance = 0.15f;
        private const float AutoTargetFPSRecalTolerance = 0.20f;
        private const float MinTLODExtraDecentReduction = 0.50f;
        private const float MinTLODExtraSensitivity = 4.0f;
        private bool AutoFPSInFlightPhase = false;
        private bool MinTLODExtraInFlightPhase = false;
        private float MinTLODExtraAmountPreTakeoff;
        private int CloudRecoveryExtraNonExpert = 0;
        private long CloudRecoveryTickMark = 0;
        private long TotalTicks = 0;
        private float altAboveGndLast = 0;
        private float groundSpeedLast = 0;
        private float longitudinalAccel;
        private float throttlePosition;
        private float TLODStep;
        private float TLODStepAdjusted;
        private bool FPSToleranceExceeded;
        private float[] deltaFPSTrend = new float[ServiceModel.FPSSettleCountMax];
        private int deltaFPSTrendIndex = 0;
        private float[] deltaFPSTrendShort = new float[10];
        private int deltaFPSTrendShortIndex = 0;
        private float deltaFPSLowest;
        private float deltaFPSRange;
        private float deltaFPSRangeLowest;
        private float deltaFPSHighest;
        private float deltaFPSTrendMin = -1f;
        private float newTLOD = 0f;
        private float TLODMinAltBand;
        private float MinTLODNoExtra;
        private float AltTLODBase;
        private int VRStateCounter = 5;
        private int TLODExtraMtnsTimer;
        private int logTimer = 0;
        private int logTimerInterval = 9;
        private float logAltAboveGndLast = -1;
        private float logTlodLast = -1;
        private float logOlodLast = -1;
        private bool logDecCloudQActiveLast = true;
        private bool logIsAppPriorityFPSLast = false;
        private float logRangeMinTLODLast = -1;

        GpuzWrapper gpuz = new GpuzWrapper();
        int gpuzGPULoadSensorIndex = -1;

        public LODController(ServiceModel model)
        {
            Model = model;

            SimConnect = IPCManager.SimConnect;
            SimConnect.SubscribeSimVar("VERTICAL SPEED", "feet per second");
            SimConnect.SubscribeSimVar("PLANE ALT ABOVE GROUND", "feet");
            SimConnect.SubscribeSimVar("PLANE ALT ABOVE GROUND MINUS CG", "feet");
            SimConnect.SubscribeSimVar("SIM ON GROUND", "Bool");
            SimConnect.SubscribeSimVar("GROUND VELOCITY", "knots");
            SimConnect.SubscribeSimVar("PLANE ALTITUDE", "feet");
            SimConnect.SubscribeSimVar("ACCELERATION BODY Z", "feet per second squared");
            SimConnect.SubscribeSimVar("GENERAL ENG THROTTLE LEVER POSITION:1", "percent");
            GetMSFSState();
            if (!gpuzRunning())
            {
                Logger.Log(LogLevel.Information, "LODController:LODController", "GPU-Z companion app not running");
                gpuzGPULoadSensorIndex = -1;
            }
            SetActiveLODs(true);
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

            if (++verticalIndex >= verticalStats.Length || verticalIndex >= verticalStatsVS.Length)
                verticalIndex = 0;

            Model.VerticalTrend = VerticalAverage();

            Model.altAboveGnd = SimConnect.ReadSimVar("PLANE ALT ABOVE GROUND", "feet");
            if (Model.altAboveGnd == 0 && !Model.OnGround)
                Model.altAboveGnd = SimConnect.ReadSimVar("PLANE ALT ABOVE GROUND MINUS CG", "feet");

            Model.groundSpeed = SimConnect.ReadSimVar("GROUND VELOCITY", "knots");
            Model.altitude = SimConnect.ReadSimVar("PLANE ALTITUDE", "feet");
            longitudinalAccel = SimConnect.ReadSimVar("ACCELERATION BODY Z", "feet per second squared");
            throttlePosition = SimConnect.ReadSimVar("GENERAL ENG THROTTLE LEVER POSITION:1", "percent");
  

            GetMSFSState();
            if (gpuzRunning())
            {
                gpuUsage[gpuIndex++] = (float)gpuz.SensorValue(gpuzGPULoadSensorIndex);
                if (gpuIndex >= gpuUsage.Length) gpuIndex = 0;
                Model.gpuUsage = gpuUsage.Average();
            }
            else Model.gpuUsage = -1;

            if (Model.groundSpeed > 1 && Model.altAboveGnd == altAboveGndLast && Model.groundSpeed == groundSpeedLast) Model.SimPaused = true;
            else Model.SimPaused = false;
            if ((!Model.ActiveWindowMSFS && ((Model.UseExpertOptions && Model.PauseMSFSFocusLost) || (!Model.UseExpertOptions && Model.FgModeEnabled))) || Model.SimPaused) Model.AppPaused = true;
            else Model.AppPaused = false;

            deltaFPSTrendMin = -(Model.VrModeActive ? 1 : (Model.LsModeEnabled ? Model.LsModeMultiplier : (Model.FgModeEnabled && Model.ActiveWindowMSFS ? 2 : 1)));
            if (SimConnect.ReadSimVar("CAMERA STATE", "Enum") == 3) deltaFPSTrendMin *= 1.5f;

            SetActiveLODs();

            if ((ServiceModel.TestVersion || Model.LogSimValues) && logTimer == 0 && (!Model.FPSSettleActive || Model.TLODAutoMethod[Model.activeProfile] == 2) && (Math.Round(Model.altAboveGnd) != Math.Round(logAltAboveGndLast) || Math.Round(Model.tlod) != Math.Round(logTlodLast) || Math.Round(Model.olod) != Math.Round(logOlodLast) || logDecCloudQActiveLast != Model.DecCloudQActive || logIsAppPriorityFPSLast != Model.IsAppPriorityFPS || Math.Round(Model.rangeMinTLOD) != Math.Round(logRangeMinTLODLast)) && !(!Model.ActiveWindowMSFS && Model.UseExpertOptions && Model.PauseMSFSFocusLost))
            {
                Logger.Log(LogLevel.Information, "LODController:UpdateVariables", $"FPS: {GetAverageFPS()}" + (Model.FgModeEnabled ? " FGAct: " + Model.ActiveWindowMSFS : "") + $" Pri: {(Model.TLODAutoMethod[Model.activeProfile] == 2 ? "ATLOD" : (Model.IsAppPriorityFPS ? "FPS" : "TLOD"))}{(Model.MinTLODExtraActive ? "+ " + Model.MinTLODExtraAmount : "")} TLOD: {Math.Round(Model.tlod)} TLODRng: {Model.rangeMinTLOD}{(Model.rangeMinTLOD == Model.rangeMaxTLOD ? "" : "-" + Model.rangeMaxTLOD)} Mtns+: {Model.TLODExtraMtnsAmountResidual} OLOD: {Math.Round(Model.olod)} AGL: {Math.Round(Model.altAboveGnd)} FPM: {Math.Round(vs * 60.0f)} Clouds: {(Model.VrModeActive ? Model.CloudQualityText(Model.cloudQ_VR) : Model.CloudQualityText(Model.cloudQ))}" + (Model.gpuUsage >= 0 ? $" GPU: {Math.Round(Model.gpuUsage)}%" : ""));
                logTimer = logTimerInterval;
                logAltAboveGndLast = Model.altAboveGnd;
                logTlodLast = Model.tlod;
                logOlodLast = Model.olod;
                logDecCloudQActiveLast = Model.DecCloudQActive;
                logIsAppPriorityFPSLast = Model.IsAppPriorityFPS;
                logRangeMinTLODLast = Model.rangeMinTLOD;

            }
            else if (--logTimer < 0) logTimer = 0;
        }

        public void RunTick()
        {
            UpdateVariables();
            float deltaFPS = GetAverageFPS() - Model.TargetFPS;
            deltaFPSTrendShort[deltaFPSTrendShortIndex] = (float)deltaFPS;
            if (++deltaFPSTrendShortIndex >= deltaFPSTrendShort.Length) deltaFPSTrendShortIndex = 0;

            if (Model.AutoTargetFPS)
            {
                if (Model.OnGround && Model.groundSpeed == 0 && AutoFPSInFlightPhase)
                {
                    AutoFPSInFlightPhase = false;
                    Model.ResetCloudsTLOD(true, true);
                    Logger.Log(LogLevel.Information, "LODController:RunTick", "Recalibrating Auto Target FPS on arrival at new location");
                }
                else if (Model.altAboveGnd >= AltTLODBase) AutoFPSInFlightPhase = true;
            }

            if (Model.MinTLODExtra[Model.activeProfile])
            {
                if (Model.TLODAutoMethod[Model.activeProfile] == 2)
                {
                    if (!Model.MinTLODExtraPreTakeoff && Model.OnGround && Model.groundSpeed > 10 && longitudinalAccel > 5 && throttlePosition > 75 && Model.MinTLODExtraAmount > 0)
                    {
                        MinTLODExtraAmountPreTakeoff = Model.MinTLODExtraAmount;
                        Model.MinTLODExtraPreTakeoff = true;
                        Logger.Log(LogLevel.Information, "LODController:RunTick", "Saving TLOD Base + " + Model.MinTLODExtraAmount.ToString("F0") + " at start of takeoff roll");
                    }
                    else if (Model.MinTLODExtraPreTakeoff && ((Model.OnGround && Model.groundSpeed < 1) || (Model.altAboveGnd >= Model.AltTLODTop[Model.activeProfile] && !Model.FPSSettleActive)))
                    {
                        if (Model.MinTLODExtraAmount < MinTLODExtraAmountPreTakeoff && deltaFPSTrendShort.Average() > deltaFPSTrendMin)
                        {
                            if ((Model.MinTLODExtraAmount += TLODStep) >= MinTLODExtraAmountPreTakeoff)
                            {
                                Model.MinTLODExtraAmount = MinTLODExtraAmountPreTakeoff;
                                Model.MinTLODExtraPreTakeoff = false;
                                Logger.Log(LogLevel.Information, "LODController:RunTick", "Restored TLOD Base + to " + Model.MinTLODExtraAmount.ToString("F0") + " as of start of takeoff roll");
                            }
                        }
                        else Model.MinTLODExtraPreTakeoff = false;
                    }
                }

                if (Model.OnGround && Model.groundSpeed == 0 && MinTLODExtraInFlightPhase)
                {
                    MinTLODExtraInFlightPhase = false;
                    Model.ResetCloudsTLOD(true, true);
                    Logger.Log(LogLevel.Information, "LODController:RunTick", "Recalibrating TLOD Min/Base + on arrival at new location");
                }
                else if (Model.altAboveGnd >= AltTLODBase) MinTLODExtraInFlightPhase = true;
            }

            if (Model.FPSSettleActive && !Model.SimPaused)
            {
                if (!Model.FPSSettleActiveLast)
                {
                    deltaFPSTrendIndex = 0;
                    deltaFPSLowest = 100;
                    deltaFPSHighest = -100;
                    deltaFPSRangeLowest = 100;
                }
                deltaFPSTrend[deltaFPSTrendIndex] = deltaFPS;

                if (Model.MinTLODExtraSeeking)
                {
                    if (Model.TLODAutoMethod[Model.activeProfile] == 2 && Model.MinTLODExtra[Model.activeProfile])
                    {
                        if (deltaFPSTrendIndex >= 4 && deltaFPSTrend.Skip(deltaFPSTrendIndex - 4).Take(5).Min() > deltaFPSTrendMin)
                        {
                            Model.MinTLODExtraSeeking = false;
                            Model.FPSSettleActive = false;
                            if (Model.MinTLODExtraIterations == 1) Model.MinTLODExtraIterations = 0;
                        }
                        else if (deltaFPSTrendIndex >= (Model.MinTLODExtraIterations > 1 ? 14 : 9))
                        {
                            if (deltaFPSTrend.Skip(deltaFPSTrendIndex - 4).Take(5).Min() <= deltaFPSTrendMin && Model.MinTLODExtraIterations > 1)
                            {
                                Model.MinTLODExtraIterations = 1;
                            }
                            else if (Model.MinTLODExtraIterations == 1) Model.MinTLODExtraIterations = 0;
                            Model.MinTLODExtraSeeking = false;
                            Model.FPSSettleActive = false;
                        }
                    }
                    else if (deltaFPSTrendIndex >= 1)
                     { 
                        if (deltaFPSLowest > deltaFPS) deltaFPSLowest = deltaFPS;
                        else if (deltaFPSHighest < deltaFPS + 0.5) deltaFPSHighest = deltaFPS;
                        else Model.FPSSettleActive = false;
                    }
                }
                else
                {
                    if (!Model.FPSSettleInitial && deltaFPSTrendIndex >= 4) 
                    {
                        deltaFPSRange = deltaFPSTrend.Skip(deltaFPSTrendIndex - 4).Take(5).Max() - deltaFPSTrend.Skip(deltaFPSTrendIndex - 4).Take(5).Min();
                        if (deltaFPSRangeLowest > deltaFPSRange) deltaFPSRangeLowest = deltaFPSRange;
                        else if (deltaFPSRange < 0.05 * Model.TargetFPS) Model.FPSSettleActive = false;
                    }
                }
                if (Model.FPSSettleInitial) Model.FPSSettleCount = ServiceModel.FPSSettleCountMax - deltaFPSTrendIndex - 1;
                if (++deltaFPSTrendIndex >= deltaFPSTrend.Length)
                {
                    deltaFPSTrendIndex = 0;
                    Model.FPSSettleActive = false;
                    Model.FPSSettleInitial = false;
                    Model.FPSSettleCount = ServiceModel.FPSSettleCountMax;
                    Model.MinTLODExtraSeeking = false;
                }
            }
            Model.FPSSettleActiveLast = Model.FPSSettleActive;

            if (!Model.AppPaused && (!Model.FPSSettleActive || (Model.TLODAutoMethod[Model.activeProfile] == 2) && !Model.MinTLODExtra[Model.activeProfile]))
            {
                float FPSToleranceAmount = Model.TargetFPS * Model.FPSTolerance[Model.activeProfile] / 100.0f;
                FPSToleranceExceeded = Math.Abs(deltaFPS) >= FPSToleranceAmount;

                if (Model.AutoTargetFPS && !(Model.FgModeEnabled && !Model.ActiveWindowMSFS) && (Model.ForceAutoFPSCal || (!(Model.FgModeEnabled && !Model.ActiveWindowMSFS) && ((deltaFPS <= -Model.TargetFPS * AutoTargetFPSRecalTolerance && (!Model.DecCloudQ[Model.activeProfile] || Model.DecCloudQActive) && Model.tlod == Model.activeMinTLOD)))))
                {
                    if (!Model.ForceAutoFPSCal && (!Model.UseExpertOptions || Model.MinTLODExtra[Model.activeProfile]) && Model.MinTLODExtraActive)
                    {
                        Model.MinTLODExtraActive = false;
                        Model.MinTLODExtraAmount = 0;
                    }
                    else
                    {
                        Model.TargetFPS = (int)(GetAverageFPS() * ((Model.FlightTypeIFR ? 0.95f : 0.90f) - Math.Min(0.1f, Model.altAboveGnd / 30000)));
                        Model.UpdateTargetFPS = true;
                        Model.ForceAutoFPSCal = false;
                        if (deltaFPS <= -Model.TargetFPS * AutoTargetFPSRecalTolerance && (!Model.DecCloudQ[Model.activeProfile] || Model.DecCloudQActive) && Model.tlod == Model.activeMinTLOD) Logger.Log(LogLevel.Information, "LODController:RunTick", "FPS too " + (Model.tlod == Model.activeMinTLOD ? "low" : "high") + " for auto target FPS settings. Auto Target FPS updated to " + $"{Model.TargetFPS}");
                        else
                        {
                            deltaFPS = GetAverageFPS() - Model.TargetFPS;
                            Logger.Log(LogLevel.Information, "LODController:RunTick", "Auto Target FPS set to " + $"{Model.TargetFPS}");
                        }
                    }
                }

                if (Model.TLODAutoMethod[Model.activeProfile] == 2)
                {
                    if (Model.MinTLODExtra[Model.activeProfile] && !(Model.FgModeEnabled && !Model.ActiveWindowMSFS) && (Model.DecCloudQMethod[Model.activeProfile] == 1 || !Model.DecCloudQActive) && (deltaFPSTrendShort.Average() <= deltaFPSTrendMin || Model.MinTLODExtraIterations == 1) && Model.MinTLODExtraAmount > 0)
                    {
                        Model.MinTLODExtraAmount -= Model.MinTLOD[Model.activeProfile];
                        if (Model.MinTLODExtraAmount > 0)
                        {
                            Model.FPSSettleActive = true;
                            if (Model.MinTLODExtraIterations != 1) Model.MinTLODExtraSeeking = true;
                        }
                        else Model.MinTLODExtraAmount = 0;
                        Model.MinTLODExtraIterations = 0;
                        Logger.Log(LogLevel.Information, "LODController:RunTick", "TLOD Base + auto-reduced to " + $"{Model.MinTLODExtraAmount}");
                        SetActiveLODs(true);
                    }
                    else if (Model.MinTLODExtra[Model.activeProfile] && !(Model.FgModeEnabled && !Model.ActiveWindowMSFS) && (Model.DecCloudQMethod[Model.activeProfile] == 1 || !Model.DecCloudQActive) && !Model.MinTLODExtraActive && Model.MinTLODExtraIterations > 1 && deltaFPS >= 0 && (Math.Ceiling(Model.tlod) < Model.activeMaxTLOD || !Model.OnGround))
                    {
                        Model.MinTLODExtraAmount += Model.MinTLOD[Model.activeProfile];
                        if (Model.MinTLODExtraAmount + MinTLODNoExtra >= Model.MaxTLOD[Model.activeProfile])
                        {
                            Model.MinTLODExtraAmount = Model.MaxTLOD[Model.activeProfile] - MinTLODNoExtra;
                        }
                        else
                        {
                            Model.MinTLODExtraIterations--;
                            Model.FPSSettleActive = true;
                            Model.MinTLODExtraSeeking = true;
                        }
                        SetActiveLODs(true);
                        Logger.Log(LogLevel.Information, "LODController:RunTick", "TLOD Base + auto-increased to " + $"{Model.MinTLODExtraAmount}");

                    }
                    else Model.MinTLODExtraIterations = 0;
                    if (Model.MinTLODExtra[Model.activeProfile] && Model.MinTLODExtraAmount > 0 && !Model.MinTLODExtraSeeking) Model.MinTLODExtraActive = true;
                    else Model.MinTLODExtraActive = false;
                    if (Model.altAboveGnd < Model.AltTLODBase[Model.activeProfile]) newTLOD = Model.activeMinTLOD;
                    else if (Model.altAboveGnd > Model.AltTLODTop[Model.activeProfile]) newTLOD = Model.activeMaxTLOD;
                    else
                    {
                        newTLOD = Model.activeMinTLOD + (Model.activeMaxTLOD - Model.activeMinTLOD) * (Model.altAboveGnd - Model.AltTLODBase[Model.activeProfile]) / (Model.AltTLODTop[Model.activeProfile] - Model.AltTLODBase[Model.activeProfile]);

                        if (Model.MinTLODExtraSeeking)
                        {
                            newTLOD = (float)Math.Round(newTLOD / Model.FPSTolerance[Model.activeProfile]) * Model.FPSTolerance[Model.activeProfile];
                            if (Model.MinTLODExtraAmount + MinTLODNoExtra >= Model.MaxTLOD[Model.activeProfile] || Model.MinTLODExtraAmount == 0)
                            {
                                Model.MinTLODExtraIterations = 0;
                                Model.MinTLODExtraSeeking = false;
                            }
                        }
                        else
                        {
                            if (Math.Abs(newTLOD - Model.tlod) < Model.FPSTolerance[Model.activeProfile]) newTLOD = Model.tlod;
                            else if (Math.Abs(newTLOD - Model.tlod) > Model.FPSTolerance[Model.activeProfile] * 2) newTLOD = Model.tlod + Math.Sign(newTLOD - Model.tlod) * Model.FPSTolerance[Model.activeProfile] * 2;
                            else newTLOD = Model.tlod + Math.Sign(newTLOD - Model.tlod) * Model.FPSTolerance[Model.activeProfile];
                        }
                    }
                }
                else if (Model.MinTLODExtra[Model.activeProfile] && Model.MinTLODExtraActive && !(Model.FgModeEnabled && !Model.ActiveWindowMSFS) && (Model.DecCloudQMethod[Model.activeProfile] == 1 || !Model.DecCloudQActive) && Math.Round(Model.tlod) == Model.activeMinTLOD && deltaFPS <= -FPSToleranceAmount && Model.MinTLODExtraAmount > 0)
                {
                        Model.MinTLODExtraAmount = (float)Math.Floor(Model.MinTLODExtraAmount * 0.8 / 10) * 10;
                        Model.FPSSettleActive = true;
                        Model.MinTLODExtraSeeking = true;
                        Model.MinTLODExtraIterations = 1;
                        SetActiveLODs(true);
                        newTLOD = Model.activeMinTLOD;
                        Logger.Log(LogLevel.Information, "LODController:RunTick", "TLOD Min + auto-reduced to " + $"{Model.MinTLODExtraAmount}");

                }
                else if (Model.MinTLODExtra[Model.activeProfile] && !(Model.FgModeEnabled && !Model.ActiveWindowMSFS) && (Model.DecCloudQMethod[Model.activeProfile] == 1 || !Model.DecCloudQActive) && !Model.MinTLODExtraActive && Model.MinTLODExtraIterations > 1 && deltaFPS >= Model.TargetFPS * MinTLODExtraActiveTolerance && Math.Ceiling(Model.tlod) < Model.activeMaxTLOD)
                {
                    Model.MinTLODExtraAmount += 10.0f * (float)Math.Floor(Math.Pow(deltaFPS, 0.9f) * MinTLODExtraSensitivity / (Model.TargetFPS * MinTLODExtraActiveTolerance));
                    if (Model.MinTLODExtraAmount + MinTLODNoExtra >= Model.MaxTLOD[Model.activeProfile])
                    {
                        Model.MinTLODExtraAmount = Model.MaxTLOD[Model.activeProfile] - MinTLODNoExtra;
                        Model.MinTLODExtraIterations = 1;
                    }
                    else
                    {
                        Model.MinTLODExtraIterations--;
                        Model.FPSSettleActive = true;
                    }
                    Model.MinTLODExtraSeeking = true;
                    SetActiveLODs(true);
                    newTLOD = Model.activeMinTLOD;
                    Logger.Log(LogLevel.Information, "LODController:RunTick", "TLOD Min + auto-increased to " + $"{Model.MinTLODExtraAmount}");
                }
                else
                {
                    if (Model.MinTLODExtraSeeking)
                    {
                        Model.MinTLODExtraActive = true;
                        Model.MinTLODExtraSeeking = false;
                        Model.MinTLODExtraIterations = ServiceModel.MinTLODExtraIterationsMax;
                    }
                    else if (Model.altAboveGnd >= AltTLODBase + TLODMinAltBand && Model.MinTLODExtra[Model.activeProfile] && Math.Sign(deltaFPS) == 1)
                    {
                        Model.MinTLODExtraAmount = (float)Math.Floor((Math.Min((Model.tlod * MinTLODExtraDecentReduction), (Model.activeMaxTLOD - Model.TLODExtraMtnsAmountResidual) / 2) - MinTLODNoExtra) / 10) * 10;
                        Model.MinTLODExtraAmount += 10.0f * (float)Math.Floor(Math.Pow(deltaFPS, 0.85) * MinTLODExtraSensitivity * 4 / (Model.TargetFPS * MinTLODExtraActiveTolerance));
                        if (Model.MinTLODExtraAmount < 0) Model.MinTLODExtraAmount = 0;
                        if (Model.MinTLODExtraAmount + MinTLODNoExtra > Model.activeMaxTLOD - Model.TLODExtraMtnsAmountResidual) Model.MinTLODExtraAmount = Model.activeMaxTLOD - Model.TLODExtraMtnsAmountResidual - MinTLODNoExtra;
                        SetActiveLODs(true);
                        Model.MinTLODExtraActive = true;
                    }
                    if (Model.TLODAutoMethod[Model.activeProfile] == 0) newTLOD = Math.Sign(deltaFPS) * (Math.Min((float)Math.Pow(Math.Abs(deltaFPS) / (GetAverageFPS() > 1 ? GetAverageFPS() : 1) * TLODStepAdjusted * 10, 1.4f), TLODStepAdjusted * 2));
                    else if (FPSToleranceExceeded) newTLOD = Math.Sign(deltaFPS) * TLODStepAdjusted * (Math.Abs(deltaFPS) >= FPSToleranceAmount * 2 ? 2 : 1);
                    else newTLOD = 0;
                    if (Model.altAboveGnd <= AltTLODBase) newTLOD = Model.activeMinTLOD;
                    else newTLOD = Math.Min(Model.activeMinTLOD + (Model.activeMaxTLOD - Model.activeMinTLOD) * (Model.altAboveGnd - AltTLODBase) / TLODMinAltBand, Model.tlod + newTLOD);
                }
                newTLOD = (float)Math.Round(Math.Min(Model.activeMaxTLOD, Math.Max(Model.activeMinTLOD, newTLOD)));

                if (Math.Abs(Model.tlod - newTLOD) >= 1)
                {
                    Model.MemoryAccess.SetTLOD(Model.tlod = newTLOD);
                    Model.tlod_step = true;
                }
                else Model.tlod_step = false;

                if (Model.DecCloudQ[Model.activeProfile] && !Model.DecCloudQActive && !Model.MinTLODExtraSeeking && ((Model.TLODAutoMethod[Model.activeProfile] != 2 && Model.DecCloudQMethod[Model.activeProfile] != 1 && Model.tlod == Model.activeMinTLOD && deltaFPS <= -FPSToleranceAmount) || ((Model.TLODAutoMethod[Model.activeProfile] == 2 || Model.DecCloudQMethod[Model.activeProfile] == 1) && Model.gpuUsage > Model.CloudDecreaseGPUPct)))
                {
                    if (Model.VrModeActive && Model.DefaultCloudQ_VR >= 1)
                    {
                        Model.MemoryAccess.SetCloudQ_VR(Model.cloudQ_VR = (Model.DefaultCloudQ_VR - 1));
                        Model.DecCloudQActive = true;
                    }
                    if (!Model.VrModeActive && Model.DefaultCloudQ >= 1)
                    {
                        Model.MemoryAccess.SetCloudQ(Model.cloudQ = (Model.DefaultCloudQ - 1));
                        Model.DecCloudQActive = true;
                    }
                    CloudRecoveryTickMark = TotalTicks;
                }

                if (Model.DecCloudQ[Model.activeProfile] && Model.DecCloudQActive && ((Model.TLODAutoMethod[Model.activeProfile] != 2 && Model.DecCloudQMethod[Model.activeProfile] != 1 && ((deltaFPS >= Model.TargetFPS * CloudRecoveryExtraTolerance) || (newTLOD >= (Model.UseExpertOptions && Model.CloudRecoveryPlus[Model.activeProfile] ? Model.activeMinTLOD + Model.CloudRecoveryTLOD[Model.activeProfile] : Model.CloudRecoveryTLOD[Model.activeProfile]) && deltaFPS >= 0))) || ((Model.TLODAutoMethod[Model.activeProfile] == 2 || Model.DecCloudQMethod[Model.activeProfile] == 1) && Model.gpuUsage <= Model.CloudRecoverGPUPct)))
                {
                    Model.ResetCloudsTLOD(false);
                    if ((!Model.UseExpertOptions || Model.MinTLODExtra[Model.activeProfile]) && !Model.MinTLODExtraActive && Model.OnGround) Model.FPSSettleActive = true;
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
                        if (Math.Abs(newOLOD - Model.olod) < Model.FPSTolerance[Model.activeProfile]) newOLOD = Model.olod;
                        else if (Math.Abs(newOLOD - Model.olod) > Model.FPSTolerance[Model.activeProfile] * 2) newOLOD = Model.olod + Math.Sign(newOLOD - Model.olod) * Model.FPSTolerance[Model.activeProfile] * 2;
                        else newOLOD = Model.olod + Math.Sign(newOLOD - Model.olod) * Model.FPSTolerance[Model.activeProfile];
                    }
                    if (newOLOD != Model.olod && Math.Abs(newOLOD - Model.olod) >= 1)
                    {
                        Model.MemoryAccess.SetOLOD(Model.olod = newOLOD);
                        Model.olod_step = true;
                    }
                    else Model.olod_step = false;
                }
                else
                {
                    Model.olod_step = true;
                    if (Model.VrModeActive && Model.olod != Model.DefaultOLOD_VR)
                        Model.MemoryAccess.SetOLOD_VR(Model.olod = Model.DefaultOLOD_VR);
                    else if (!Model.VrModeActive && Model.olod != Model.DefaultOLOD)
                        Model.MemoryAccess.SetOLOD_PC(Model.olod = Model.DefaultOLOD);
                    else Model.olod_step = false;
                }
            }

            Model.rangeMinTLOD = Model.activeMinTLOD;
            if (Model.altAboveGnd >= AltTLODBase + TLODMinAltBand || Model.TLODAutoMethod[Model.activeProfile] == 2)
            {
                Model.IsAppPriorityFPS = true;
                Model.rangeMaxTLOD = Model.activeMaxTLOD;
            }
            else
            {
                Model.IsAppPriorityFPS = false;
                if (Model.altAboveGnd >= AltTLODBase) Model.rangeMaxTLOD = (float)Math.Round(Math.Min(Model.activeMinTLOD + (Model.activeMaxTLOD - Model.activeMinTLOD) * (Model.altAboveGnd - AltTLODBase) / TLODMinAltBand, Model.activeMaxTLOD) / 10) * 10;
                else Model.rangeMaxTLOD = Model.activeMinTLOD;
            }

            TotalTicks++;
            altAboveGndLast = Model.altAboveGnd;
            groundSpeedLast = Model.groundSpeed;
        }
        public int VerticalAverage()
        {
            return verticalStats.Sum();
        }
        public float VSAverage()
        {
            return verticalStatsVS.Average();
        }

        public bool gpuzRunning()
        {
            if (gpuzGPULoadSensorIndex >= 0) return true;
            else if (gpuzGPULoadSensorIndex == -1 && gpuz.Open())
            {
                Logger.Log(LogLevel.Information, "LODController:gpuzRunning", "GPU-Z companion app running");
                for (gpuzGPULoadSensorIndex = 0; gpuz.SensorName(gpuzGPULoadSensorIndex) != String.Empty && gpuz.SensorName(gpuzGPULoadSensorIndex) != "GPU Load"; gpuzGPULoadSensorIndex++) ;
                if (gpuz.SensorName(gpuzGPULoadSensorIndex) != "GPU Load")
                {
                    Logger.Log(LogLevel.Information, "LODController:gpuzRunning", "GPU-Z GPU Load sensor not found");
                    gpuzGPULoadSensorIndex = -2;
                }
                else Logger.Log(LogLevel.Information, "LODController:gpuzRunning", "GPU-Z GPU Load sensor found");
                return true;
            }
            else return false;
        }
        public void SetActiveLODs(bool SetTLODMinAltBand = false)
        {
            if (Model.UseExpertOptions)
            {
                MinTLODNoExtra = Model.MinTLOD[Model.activeProfile];
                Model.activeMinTLOD = MinTLODNoExtra + Model.MinTLODExtraAmount;
                Model.activeMaxTLOD = Model.MaxTLOD[Model.activeProfile];
                if (Model.TLODExtraMtns[Model.activeProfile] && Model.altitude - Model.altAboveGnd >= Model.TLODExtraMtnsTriggerAlt[Model.activeProfile])
                {
                    Model.TLODExtraMtnsAmountResidual = Model.TLODExtraMtnsAmount[Model.activeProfile];
                    Model.TLODExtraMtnsActive = true;
                    TLODExtraMtnsTimer = 300;
                }
                else if (Model.TLODExtraMtnsActive && --TLODExtraMtnsTimer < 0) Model.TLODExtraMtnsActive = false;
                else if (!Model.TLODExtraMtnsActive && Model.TLODExtraMtnsAmountResidual > 0)
                {
                    Model.TLODExtraMtnsAmountResidual -= 5;
                    if (Model.TLODExtraMtnsAmountResidual < 0) Model.TLODExtraMtnsAmountResidual = 0;
                }
                if (Model.TLODAutoMethod[Model.activeProfile] == 2 && Model.MinTLODExtra[Model.activeProfile])Model.activeMaxTLOD += Model.MinTLODExtraAmount;
                else Model.activeMaxTLOD += Model.TLODExtraMtnsAmountResidual;
                Model.activeOLODAtBase = Model.OLODAtBase[Model.activeProfile];
                Model.activeOLODAtTop = Model.OLODAtTop[Model.activeProfile];
                AltTLODBase = Model.AltTLODBase[Model.activeProfile];
            }
            else
            {
                float defaultTLOD = Model.VrModeActive ? Model.DefaultTLOD_VR : Model.DefaultTLOD;
                float defaultOLOD = Model.VrModeActive ? Model.DefaultOLOD_VR : Model.DefaultOLOD;
                MinTLODNoExtra = Math.Max(defaultTLOD * (Model.FlightTypeIFR ? 0.5f : 1.0f), 10.0f);
                Model.activeMaxTLOD = Model.MaxTLOD[(int)ServiceModel.appProfiles.NonExpert] = defaultTLOD * (Model.FlightTypeIFR ? 2.0f : 3.0f);
                Model.activeMinTLOD = Model.MinTLOD[(int)ServiceModel.appProfiles.NonExpert] = MinTLODNoExtra + Model.MinTLODExtraAmount;
                Model.activeOLODAtBase = Model.OLODAtBase[(int)ServiceModel.appProfiles.NonExpert] = Model.FlightTypeIFR ? defaultOLOD : defaultOLOD * 1.5f;
                Model.activeOLODAtTop = Model.OLODAtTop[(int)ServiceModel.appProfiles.NonExpert] = Math.Max(Model.activeOLODAtBase * 0.1f, 10.0f);
                AltTLODBase = Model.AltTLODBase[(int)ServiceModel.appProfiles.NonExpert] = Model.FlightTypeIFR ? 1000.0f : 100.0f;
                Model.CloudRecoveryTLOD[Model.activeProfile] = (float)Math.Min(Math.Round(2 * (Model.activeMinTLOD + Model.activeMaxTLOD) / 5), Model.activeMinTLOD + 50.0f) + CloudRecoveryExtraNonExpert;
            }
            TLODStep = TLODStepAdjusted = Math.Max(2.0f, Model.FPSTolerance[Model.activeProfile]);
            if (SetTLODMinAltBand) TLODMinAltBand = Math.Max(Model.AvgDescentRate[Model.activeProfile] / 60 * ((Model.activeMaxTLOD - Model.activeMinTLOD) / TLODStep), 1);
        }
        public float GetAverageFPS()
        {
            if (Model.VrModeActive) return (float)Math.Round(IPCManager.SimConnect.GetAverageFPS());
            else if (Model.LsModeEnabled) return (float)Math.Round(IPCManager.SimConnect.GetAverageFPS() * Model.LsModeMultiplier);
            else if (Model.FgModeEnabled) return (float)Math.Round(IPCManager.SimConnect.GetAverageFPS() * 2.0f);
            else return (float)Math.Round(IPCManager.SimConnect.GetAverageFPS());
        }
        private void GetMSFSState()
        {
            if (--VRStateCounter <= 0)
            {
                Model.VrModeActive = Model.MemoryAccess.IsVrModeActive();
                VRStateCounter = 5;
            }
            if (!Model.ActiveWindowMSFS && Model.MemoryAccess.IsActiveWindowMSFS() && (Model.FgModeEnabled || (Model.UseExpertOptions && Model.PauseMSFSFocusLost))) Model.FPSSettleActive = true;
            Model.ActiveWindowMSFS = Model.MemoryAccess.IsActiveWindowMSFS();
            string ActiveGraphicsMode = Model.ActiveGraphicsMode;
            if (Model.VrModeActive)
            {
                Model.ActiveGraphicsMode = "VR";
                if (!Model.AutoTargetFPS) Model.TargetFPS = Model.FlightTypeIFR ? Model.TargetFPS_VR : Model.TargetFPS_VR_VFR;
            }
            else if (Model.LsModeEnabled)
            {
                Model.ActiveGraphicsMode = "LSFG";
                if (!Model.AutoTargetFPS) Model.TargetFPS = Model.FlightTypeIFR ? Model.TargetFPS_LS : Model.TargetFPS_LS_VFR;
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
                Model.ActiveGraphicsModeChanged = true;
                Model.ResetCloudsTLOD(true, true);
            }
        }
    }
}
