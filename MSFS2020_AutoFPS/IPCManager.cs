using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace MSFS2020_AutoFPS
{
    public static class IPCManager
    {
        public static readonly int waitDuration = 30000;

        public static MobiSimConnect SimConnect { get; set; } = null;

        public static bool WaitForSimulator(ServiceModel model)
        {
            bool simRunning = IsSimRunning();
            bool logEntryMade = false;

            if (!simRunning && model.WaitForConnect)
            {
                do
                {
                    if (!logEntryMade)
                    {
                        Logger.Log(LogLevel.Information, "IPCManager:WaitForSimulator", $"Simulator not started - waiting {waitDuration / 1000}s between Retries");
                        logEntryMade = true;
                    }
                    Thread.Sleep(waitDuration);
                }
                while (!IsSimRunning() && !model.CancellationRequested);

                Thread.Sleep(waitDuration);
                return true;
            }
            else if (simRunning)
            {
                Logger.Log(LogLevel.Information, "IPCManager:WaitForSimulator", $"Simulator started");
                return true;
            }
            else
            {
                Logger.Log(LogLevel.Error, "IPCManager:WaitForSimulator", $"Simulator not started - aborting");
                return false;
            }
        }

        public static bool IsProcessRunning(string name)
        {
            Process proc = Process.GetProcessesByName(name).FirstOrDefault();
            return proc != null && proc.ProcessName == name;
        }

        public static bool IsSimRunning()
        {
            return IsProcessRunning("FlightSimulator");
        }
        
        public static bool WaitForConnection(ServiceModel model)
        {
            if (!IsSimRunning())
                return false;

            SimConnect = new MobiSimConnect();
            bool mobiRequested = SimConnect.Connect();
            bool logEntryMade = false;

            if (!SimConnect.IsConnected)
            {
                do
                {
                    if (!logEntryMade)
                    {
                        Logger.Log(LogLevel.Information, "IPCManager:WaitForConnection", $"Connection not established - waiting {waitDuration / 1000}s between Retries");
                        logEntryMade = true;
                    }
                    Thread.Sleep(waitDuration / 2);
                    if (!mobiRequested)
                        mobiRequested = SimConnect.Connect();
                }
                while (!SimConnect.IsConnected && IsSimRunning() && !model.CancellationRequested);

                return SimConnect.IsConnected;
            }
            else
            {
                Logger.Log(LogLevel.Information, "IPCManager:WaitForConnection", $"SimConnect is opened");
                return true;
            }
        }
                
        public static bool WaitForSessionReady(ServiceModel model)
        {
            int waitDuration = 5000;
            SimConnect.SubscribeSimVar("CAMERA STATE", "Enum");
            SimConnect.SubscribeSimVar("PLANE IN PARKING STATE", "Bool");
            Thread.Sleep(250);
            bool isReady = IsCamReady();
            bool logEntryMade = false;
            while (IsSimRunning() && !isReady && !model.CancellationRequested)
            {
                if (!logEntryMade)
                {
                    Logger.Log(LogLevel.Information, "IPCManager:WaitForSessionReady", $"Session not ready - waiting {waitDuration / 1000}s between Retries");
                    logEntryMade = true;
                }
                Thread.Sleep(waitDuration);
                isReady = IsCamReady();
            }

            if (!isReady)
            {
                Logger.Log(LogLevel.Information, "IPCManager:WaitForSessionReady", $"SimConnect or Simulator not available - aborting");
                return false;
            }

            return true;
        }

        public static bool IsCamReady()
        {
            float value = SimConnect.ReadSimVar("CAMERA STATE", "Enum");
            bool parkState = SimConnect.ReadSimVar("PLANE IN PARKING STATE", "Bool") == 1;

            return value >= 2 && value <= 5 && !parkState;
        }

        public static void CloseSafe()
        {
            try
            {
                if (SimConnect != null)
                {
                    SimConnect.Disconnect();
                    SimConnect = null;
                }
            }
            catch { }
        }
    }
}
