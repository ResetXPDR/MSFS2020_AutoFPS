using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection;
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
        private int groundSpeed = 0;
        private float vs;
 
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

            Model.altAboveGnd = (int)SimConnect.ReadSimVar("PLANE ALT ABOVE GROUND", "feet");
            if (Model.altAboveGnd == 0 && !Model.OnGround)
                Model.altAboveGnd = (int)SimConnect.ReadSimVar("PLANE ALT ABOVE GROUND MINUS CG", "feet");

            groundSpeed = (int)SimConnect.ReadSimVar("GROUND VELOCITY", "knots");
            GetMSFSState();
        }

        public void RunTick()
        {
            UpdateVariables();

            float TLODStep;
            bool TLODMinGndLanding = true;
            bool DecCloudQ = true;
            float FPSTolerance = 5.0f;
            float MinTLOD;
            float MaxTLOD;
            float newTLOD;
            float CloudRecoveryTLOD = Model.DefaultTLOD - 1;
            float OLODAtBase = Model.DefaultOLOD; 
            float AltOLODBase = 2000;
            float OLODAtTop = Math.Max(Model.DefaultOLOD * 0.2f, 10.0f);
            float AltOLODTop = 10000;
            float TLODMinAltBand;
            float AltTLODBase = 1000;
            float AvgDescentRate = 2000;
            const float FPSPriorityBaseAlt = 1000;
            if (Model.UseExpertOptions)
            {
                FPSTolerance = (float)Model.FPSTolerance;
                TLODMinGndLanding = Model.TLODMinGndLanding;
                MinTLOD = Model.MinTLOD;
                MaxTLOD = Model.MaxTLOD;
                DecCloudQ = Model.DecCloudQ;
                CloudRecoveryTLOD = Model.CloudRecoveryTLOD;
                if (Model.TLODMinGndLanding)
                {
                    AltTLODBase = Model.AltTLODBase;
                    AvgDescentRate = Model.AvgDescentRate;
                }
            }
            else
            {
                if (Model.VrModeActive)
                {
                    MinTLOD = Math.Max(Model.DefaultTLOD_VR * 0.5f, 10.0f);
                    MaxTLOD = Model.DefaultTLOD_VR * 2.0f;
                }
                else
                {
                    MinTLOD = Math.Max(Model.DefaultTLOD * 0.5f, 10.0f);
                    MaxTLOD = Model.DefaultTLOD * 2.0f;
                }
            }
            if (Model.CustomAutoOLOD && Model.UseExpertOptions)
            {
                OLODAtBase = Model.OLODAtBase;
                AltOLODBase = Model.AltOLODBase;
                OLODAtTop = Model.OLODAtTop;
                AltOLODTop = Model.AltOLODTop;
            }
            TLODStep = Math.Max(2.0f, Model.FPSTolerance);
            TLODMinAltBand = AvgDescentRate / 60 * ((MaxTLOD - MinTLOD) / TLODStep);
 
            if (!TLODMinGndLanding ||  Model.altAboveGnd >= AltTLODBase + TLODMinAltBand) Model.IsAppPriorityFPS = true;
            else Model.IsAppPriorityFPS = false;
            if (!(!Model.ActiveWindowMSFS && (Model.UseExpertOptions && Model.PauseMSFSFocusLost)) && Model.FPSSettleCounter == 0)
            {
                float deltaFPS = GetAverageFPS() - Model.TargetFPS;
                if (Math.Abs(deltaFPS) >= Model.TargetFPS * FPSTolerance / 100 || !Model.IsAppPriorityFPS)
                {
                    newTLOD = Model.tlod + Math.Sign(deltaFPS) * (Model.OnGround && groundSpeed > 1 ? 2 : TLODStep * (Math.Abs(deltaFPS) >= Model.TargetFPS * 2 * FPSTolerance / 100 && (groundSpeed < 1 || !Model.OnGround) ? 2 : 1) * (Model.altAboveGnd < FPSPriorityBaseAlt && !Model.OnGround ? (float)Model.altAboveGnd / FPSPriorityBaseAlt : 1));
                    if (!Model.IsAppPriorityFPS)
                    {
                        if (Model.altAboveGnd < AltTLODBase) newTLOD = MinTLOD;
                        else newTLOD = Math.Min(MinTLOD + (MaxTLOD - MinTLOD) * (Model.altAboveGnd - AltTLODBase) / TLODMinAltBand, newTLOD);
                    }
                    newTLOD = (float)Math.Round(Math.Min(MaxTLOD, Math.Max(MinTLOD, newTLOD)));
                    if (Math.Abs(Model.tlod - newTLOD) >= 1)
                    {
                        Model.MemoryAccess.SetTLOD(newTLOD);
                        Model.tlod_step = true;
                    }
                    else Model.tlod_step = false;
                    if (DecCloudQ && !Model.DecCloudQActive && newTLOD == MinTLOD && (!TLODMinGndLanding || (TLODMinGndLanding && deltaFPS <= -Model.TargetFPS * FPSTolerance / 100)))
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
                    }
                    if (DecCloudQ && Model.DecCloudQActive && ((TLODMinGndLanding && deltaFPS >= Model.TargetFPS * 0.15f) || (newTLOD >= CloudRecoveryTLOD && (!TLODMinGndLanding || (TLODMinGndLanding && deltaFPS >= Model.TargetFPS * FPSTolerance / 100)))))
                    {
                        if (Model.VrModeActive) Model.MemoryAccess.SetCloudQ_VR(Model.DefaultCloudQ_VR);
                        else Model.MemoryAccess.SetCloudQ(Model.DefaultCloudQ);
                        Model.DecCloudQActive = false;
                    }
                }
                else Model.tlod_step = false;

                if (Model.CustomAutoOLOD && Model.UseExpertOptions)
                {
                    float newOLOD;
                    if (Model.altAboveGnd < AltOLODBase) newOLOD = OLODAtBase;
                    else if (Model.altAboveGnd > AltOLODTop) newOLOD = OLODAtTop;
                    else
                    {
                        newOLOD = OLODAtBase + (OLODAtTop - OLODAtBase) * (Model.altAboveGnd - AltOLODBase) / (AltOLODTop - AltOLODBase);
                        if (Math.Abs(newOLOD - Model.olod) > FPSTolerance) newOLOD = newOLOD + Math.Sign(newOLOD - Model.olod) * FPSTolerance;
                    }
                    if (newOLOD != Model.olod && Math.Abs(newOLOD - Model.olod) >= 1)
                    {
                        Model.MemoryAccess.SetOLOD(newOLOD);
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
            else if (--Model.FPSSettleCounter < 0) Model.FPSSettleCounter = 0;
        }
        public int VerticalAverage()
        {
            return verticalStats.Sum();
        }
        public float VSAverage()
        {
            return verticalStatsVS.Average();
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
            if (Model.ActiveWindowMSFS != Model.MemoryAccess.IsActiveWindowMSFS() && Model.UseExpertOptions && Model.PauseMSFSFocusLost) Model.FPSSettleCounter = ServiceModel.FPSSettleSeconds;
            Model.ActiveWindowMSFS = Model.MemoryAccess.IsActiveWindowMSFS();
            string ActiveGraphicsMode = Model.ActiveGraphicsMode;
            if (Model.VrModeActive)
            {
                Model.ActiveGraphicsMode = "VR";
                Model.TargetFPS = Model.TargetFPS_VR;
            }
            else if (Model.FgModeEnabled)
            {
                Model.ActiveGraphicsMode = "FG";
                Model.TargetFPS = Model.TargetFPS_FG;
            }
            else
            {
                Model.ActiveGraphicsMode = "PC";
                Model.TargetFPS = Model.TargetFPS_PC;
            }
            if (Model.ActiveGraphicsMode != ActiveGraphicsMode)
            {
                Model.FPSSettleCounter = ServiceModel.FPSSettleSeconds;
                Model.ActiveGraphicsModeChanged = true;
            }
        }
    }
}
