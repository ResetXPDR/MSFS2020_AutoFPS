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
        private int altAboveGnd = 0;
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

            altAboveGnd = (int)SimConnect.ReadSimVar("PLANE ALT ABOVE GROUND", "feet");
            if (altAboveGnd == 0 && !Model.OnGround)
                altAboveGnd = (int)SimConnect.ReadSimVar("PLANE ALT ABOVE GROUND MINUS CG", "feet");

            groundSpeed = (int)SimConnect.ReadSimVar("GROUND VELOCITY", "knots");
            GetMSFSState();
        }

        public void RunTick()
        {
            UpdateVariables();

            float TLODStep;
            bool TLODMinGndLanding;
            bool DecCloudQ;
            float FPSTolerance;
            float MinTLOD;
            float MaxTLOD;
            float newTLOD;
            float CloudRecoveryTLOD;
            if (Model.UseExpertOptions)
            {
                FPSTolerance = (float)Model.FPSTolerance;
                TLODMinGndLanding = Model.TLODMinGndLanding;
                MinTLOD = Model.MinTLOD;
                MaxTLOD = Model.MaxTLOD;
                DecCloudQ = Model.DecCloudQ;
                CloudRecoveryTLOD = Model.CloudRecoveryTLOD;
            }
            else
            {
                FPSTolerance = 5.0f;
                TLODMinGndLanding = true;
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
                DecCloudQ = true;
                CloudRecoveryTLOD = Model.DefaultTLOD - 1;
            }
            TLODStep = Math.Max(2.0f, Model.FPSTolerance);

            if (Model.UseExpertOptions && Model.TLODMinGndLanding)
            {
                Model.TLODMinTriggerAlt = 1000 + Math.Abs(VSAverage()) * (Model.tlod - MinTLOD) / TLODStep;
                if (altAboveGnd > Model.TLODMinTriggerAlt + 1000) Model.TLODMinDescentPhasePrimed = true;
                if (altAboveGnd <= Model.TLODMinTriggerAlt && Model.TLODMinDescentPhasePrimed) Model.TLODMinDescentPhaseActive = true;
                if (altAboveGnd <= 500)
                {
                    Model.TLODMinDescentPhasePrimed = false;
                    Model.TLODMinDescentPhaseActive = false;
                }
            }
 
            if ((Model.UseExpertOptions && !Model.TLODMinGndLanding) || altAboveGnd >= Model.TLODMinTriggerAlt || (VerticalAverage() >= 3 && !Model.OnGround)) Model.IsAppPriorityFPS = true;
            else Model.IsAppPriorityFPS = false;
            if (!(!Model.ActiveWindowMSFS && (!Model.UseExpertOptions || Model.PauseMSFSFocusLost)) && Model.FPSSettleCounter == 0)
            {
                float deltaFPS = GetAverageFPS() - Model.TargetFPS;
                if (Math.Abs(deltaFPS) >= Model.TargetFPS * FPSTolerance / 100 || (TLODMinGndLanding && altAboveGnd < Model.TLODMinTriggerAlt))
                {
                    if (TLODMinGndLanding)
                    {
                        if ((VerticalAverage() <= -3 || Model.OnGround) && altAboveGnd <= Model.TLODMinTriggerAlt) newTLOD = Model.tlod + (altAboveGnd - 1000 > 0 ? Math.Max((Model.tlod - MinTLOD) / (altAboveGnd - 1000) * VSAverage(), -20) : (Model.OnGround ? MinTLOD - Model.tlod : -20));
                        else if (Model.TLODMinDescentPhaseActive && altAboveGnd <= Model.TLODMinTriggerAlt) newTLOD = Model.tlod;
                        else newTLOD = Model.tlod + (!Model.OnGround ? Math.Sign(deltaFPS) * TLODStep * (Math.Abs(deltaFPS) >= Model.TargetFPS * 2 * FPSTolerance / 100 ? 2 : 1) * (altAboveGnd < 1000 && !Model.OnGround && VerticalAverage() >= 3 ? (float)altAboveGnd / 1000 : (altAboveGnd > 1000 ? 1 : -1)) * (altAboveGnd < 100 ? 0 : 1) : 0);
                    }
                    else
                        newTLOD = Model.tlod + Math.Sign(deltaFPS) * (Model.OnGround && groundSpeed > 1 ? 2 : TLODStep * (Math.Abs(deltaFPS) >= Model.TargetFPS * 2 * FPSTolerance / 100 && (groundSpeed < 1 || !Model.OnGround) ? 2 : 1) * (altAboveGnd < 1000 && !Model.OnGround ? (float)altAboveGnd / 1000 : 1));
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
                else
                {
                    Model.tlod_step = false;
                    Model.olod_step = false;
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
            if (Model.FgModeActive)
                return (float)Math.Round(IPCManager.SimConnect.GetAverageFPS() * 2.0f);
            else
                return (float)Math.Round(IPCManager.SimConnect.GetAverageFPS());
        }
        private void GetMSFSState()
        {
            Model.tlod = Model.MemoryAccess.GetTLOD_PC();
            Model.cloudQ = Model.MemoryAccess.GetCloudQ_PC();
            Model.cloudQ_VR = Model.MemoryAccess.GetCloudQ_VR();
            Model.VrModeActive = Model.MemoryAccess.IsVrModeActive();
            Model.FgModeActive = Model.MemoryAccess.IsFgModeActive();
            if (Model.ActiveWindowMSFS != Model.MemoryAccess.IsActiveWindowMSFS() && (!Model.UseExpertOptions || Model.PauseMSFSFocusLost)) Model.FPSSettleCounter = ServiceModel.FPSSettleSeconds;
            Model.ActiveWindowMSFS = Model.MemoryAccess.IsActiveWindowMSFS();
            string ActiveGraphicsMode = Model.ActiveGraphicsMode;
            if (Model.VrModeActive)
            {
                Model.ActiveGraphicsMode = "VR";
                Model.TargetFPS = Model.TargetFPS_VR;
            }
            else if (Model.FgModeActive)
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
