using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
        private float tlod = 0;
        private int cloudQ = 0;
        private int cloudQ_VR = 0;
        private int fpsModeTicks = 0;
        private int fpsModeDelayTicks = 0;
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
            tlod = Model.MemoryAccess.GetTLOD_PC();
            cloudQ = Model.MemoryAccess.GetCloudQ_PC();
            cloudQ_VR = Model.MemoryAccess.GetCloudQ_VR();
            if (cloudQ > Model.DefaultCloudQ) Model.DefaultCloudQ = cloudQ;
            if (cloudQ_VR > Model.DefaultCloudQ_VR) Model.DefaultCloudQ_VR = cloudQ_VR;
            Model.CurrentPairTLOD = 0;
            Model.CurrentPairOLOD = 0;
            Model.fpsMode = false;
            Model.tlod_step = false;
            Model.olod_step = false;
    }

    private void UpdateVariables()
        {
            vs = SimConnect.ReadSimVar("VERTICAL SPEED", "feet per second");
            Model.OnGround = SimConnect.ReadSimVar("SIM ON GROUND", "Bool") == 1.0f;
            verticalStatsVS[verticalIndex] = vs;
            if (vs >= 8.0f)
                verticalStats[verticalIndex] = 1;
            else if (vs <= -8.0f)
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
            tlod = Model.MemoryAccess.GetTLOD_PC();
            cloudQ = Model.MemoryAccess.GetCloudQ_PC();
            cloudQ_VR = Model.MemoryAccess.GetCloudQ_VR();
        }


        public void RunTick()
        {
            UpdateVariables();

            int FPSTolerance;
            bool TLODMinGndLanding;
            bool DecCloudQ;
            float MinTLOD;
            float MaxTLOD;
            float newTLOD;
            float CloudRecoveryTLOD;
            if (Model.UseExpertOptions)
            {
                FPSTolerance = Model.FPSTolerance;
                TLODMinGndLanding = Model.TLODMinGndLanding;
                MinTLOD = Model.MinTLOD;
                MaxTLOD = Model.MaxTLOD;
                DecCloudQ = Model.DecCloudQ;
                CloudRecoveryTLOD = Model.CloudRecoveryTLOD;
            }
            else
            {
                FPSTolerance = 5;
                TLODMinGndLanding = false;
                if (Model.MemoryAccess.IsVrModeActive())
                {
                    MinTLOD = Math.Max(Model.DefaultTLOD_VR * 0.5f, 10);
                    MaxTLOD = Model.DefaultTLOD_VR * 2.0f;
                }
                else
                {
                    MinTLOD = Math.Max(Model.DefaultTLOD * 0.5f, 10);
                    MaxTLOD = Model.DefaultTLOD * 2.0f;
                }
                DecCloudQ = true;
                CloudRecoveryTLOD = Model.DefaultTLOD - 1;
            }
            if ((!Model.UseExpertOptions || Model.TLODMinGndLanding) && altAboveGnd < 2000 && (VSAverage() <= 0 || Model.OnGround)) Model.IsAppPriorityFPS = false;
            else Model.IsAppPriorityFPS = true;
            float deltaFPS = GetAverageFPS() - Model.TargetFPS;
            if (Math.Abs(deltaFPS) >= Model.TargetFPS * FPSTolerance / 100 || (TLODMinGndLanding && altAboveGnd < 2000))
            {
                if (TLODMinGndLanding)
                {
                    if ((VSAverage() <= 0 || Model.OnGround) && altAboveGnd < 2000) newTLOD = tlod + (altAboveGnd - 1000 > 0 ? (tlod - MinTLOD) / (altAboveGnd - 1000) * VSAverage(): MinTLOD - tlod);
                    else newTLOD = tlod + (!Model.OnGround ? Math.Sign(deltaFPS) * FPSTolerance * (Math.Abs(deltaFPS) >= Model.TargetFPS * 2 * FPSTolerance / 100 ? 2 : 1) * (altAboveGnd < 1000 && !Model.OnGround ? (float)altAboveGnd / 1000 : 1) : 0);
                }
                else
                    newTLOD = tlod + Math.Sign(deltaFPS) * (Model.OnGround && groundSpeed > 1 ? 1 : FPSTolerance * (Math.Abs(deltaFPS) >= Model.TargetFPS * 2 * FPSTolerance / 100 && (groundSpeed < 1 || !Model.OnGround) ? 2 : 1) * (altAboveGnd < 1000 && !Model.OnGround ? (float)altAboveGnd / 1000 : 1));
                newTLOD = (float)Math.Round(Math.Min(MaxTLOD, Math.Max(MinTLOD, newTLOD)));
                if (Math.Abs(tlod - newTLOD) >= 1)
                {
                    Model.MemoryAccess.SetTLOD(newTLOD);
                    Model.tlod_step = true;
                }
                else Model.tlod_step = false;
                if (DecCloudQ && newTLOD == MinTLOD && ((!TLODMinGndLanding && Model.tlod_step) || (TLODMinGndLanding && deltaFPS <= -Model.TargetFPS * FPSTolerance / 100)))
                {
                    if (Model.MemoryAccess.IsVrModeActive() && Model.DefaultCloudQ_VR >= 1)
                    {
                        Model.MemoryAccess.SetCloudQ_VR(Model.DefaultCloudQ_VR - 1);
                        Model.DecCloudQActive = true;
                    }
                    if (!Model.MemoryAccess.IsVrModeActive() && Model.DefaultCloudQ >= 1)
                    {
                        Model.MemoryAccess.SetCloudQ(Model.DefaultCloudQ - 1);
                        Model.DecCloudQActive = true;
                    }
                }
                if (DecCloudQ && Model.DecCloudQActive && ((!TLODMinGndLanding && newTLOD >= CloudRecoveryTLOD && Model.tlod_step) || (TLODMinGndLanding && deltaFPS >= Model.TargetFPS * FPSTolerance / 100)))
                {
                    if (Model.MemoryAccess.IsVrModeActive()) Model.MemoryAccess.SetCloudQ_VR(Model.DefaultCloudQ_VR);
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
            if (Model.MemoryAccess.IsFgModeActive())
                return (float)Math.Round(IPCManager.SimConnect.GetAverageFPS() * 2.0f);
            else
                return (float)Math.Round(IPCManager.SimConnect.GetAverageFPS());
        }
    }
}
