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
        private float tlod = 0;
        private int cloudQ = 0;
        private int cloudQ_VR = 0;
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
            if (vs >= 4.0f)
                verticalStats[verticalIndex] = 1;
            else if (vs <= -4.0f)
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

            if ((Model.UseExpertOptions && !Model.TLODMinGndLanding) || altAboveGnd >= 2000 || (VerticalAverage() >= 3 && !Model.OnGround)) Model.IsAppPriorityFPS = true;
            else Model.IsAppPriorityFPS = false;
            if (!(!Model.ActiveWindowMSFS && (!Model.UseExpertOptions || Model.PauseMSFSFocusLost)) && Model.FPSSettleCounter == 0)
            {
                bool TLODMinGndLanding;
                bool DecCloudQ;
                float FPSTolerance;
                float TLODStep;
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
                TLODStep = Math.Max(2.0f, FPSTolerance);
                float deltaFPS = GetAverageFPS() - Model.TargetFPS;
                if (Math.Abs(deltaFPS) >= Model.TargetFPS * FPSTolerance / 100 || (TLODMinGndLanding && altAboveGnd < 2000))
                {
                    if (TLODMinGndLanding)
                    {
                        if ((VerticalAverage() <= -3 || Model.OnGround) && altAboveGnd < 2000) newTLOD = tlod + (altAboveGnd - 1000 > 0 ? Math.Max((tlod - MinTLOD) / (altAboveGnd - 1000) * VSAverage(), -20) : (Model.OnGround ? MinTLOD - tlod : -20));
                        else newTLOD = tlod + (!Model.OnGround ? Math.Sign(deltaFPS) * TLODStep * (Math.Abs(deltaFPS) >= Model.TargetFPS * 2 * FPSTolerance / 100 ? 2 : 1) * (altAboveGnd < 1000 && !Model.OnGround && VerticalAverage() >= 3 ? (float)altAboveGnd / 1000 : (altAboveGnd > 1000 ? 1 : -1)) * (altAboveGnd < 100 ? 0 : 1) : 0);
                    }
                    else
                        newTLOD = tlod + Math.Sign(deltaFPS) * (Model.OnGround && groundSpeed > 1 ? 2 : TLODStep * (Math.Abs(deltaFPS) >= Model.TargetFPS * 2 * FPSTolerance / 100 && (groundSpeed < 1 || !Model.OnGround) ? 2 : 1) * (altAboveGnd < 1000 && !Model.OnGround ? (float)altAboveGnd / 1000 : 1));
                    newTLOD = (float)Math.Round(Math.Min(MaxTLOD, Math.Max(MinTLOD, newTLOD)));
                    if (Math.Abs(tlod - newTLOD) >= 1)
                    {
                        Model.MemoryAccess.SetTLOD(newTLOD);
                        Model.tlod_step = true;
                    }
                    else Model.tlod_step = false;
                    if (DecCloudQ && newTLOD == MinTLOD && ((!TLODMinGndLanding && Model.tlod_step) || (TLODMinGndLanding && deltaFPS <= -Model.TargetFPS * FPSTolerance / 100)))
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
                    if (DecCloudQ && Model.DecCloudQActive && newTLOD >= CloudRecoveryTLOD && ((!TLODMinGndLanding && Model.tlod_step) || (TLODMinGndLanding && deltaFPS >= Model.TargetFPS * FPSTolerance / 100)))
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
            tlod = Model.tlod = Model.MemoryAccess.GetTLOD_PC();
            cloudQ = Model.cloudQ = Model.MemoryAccess.GetCloudQ_PC();
            cloudQ_VR = Model.cloudQ_VR = Model.MemoryAccess.GetCloudQ_VR();
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
