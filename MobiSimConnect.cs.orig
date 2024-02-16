using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace DynamicLOD_ResetEdition
{
    public class MobiSimConnect : IDisposable
    {
        public const string MOBIFLIGHT_CLIENT_DATA_NAME_COMMAND = "MobiFlight.Command";
        public const string MOBIFLIGHT_CLIENT_DATA_NAME_RESPONSE = "MobiFlight.Response";
        public const uint MOBIFLIGHT_MESSAGE_SIZE = 1024;

        public const uint WM_PILOTSDECK_SIMCONNECT = 0x1989;
        public const string CLIENT_NAME = "DynamicLOD_ResetEdition";
        public const string PILOTSDECK_CLIENT_DATA_NAME_SIMVAR = $"{CLIENT_NAME}.LVars";
        public const string PILOTSDECK_CLIENT_DATA_NAME_COMMAND = $"{CLIENT_NAME}.Command";
        public const string PILOTSDECK_CLIENT_DATA_NAME_RESPONSE = $"{CLIENT_NAME}.Response";

        protected SimConnect simConnect = null;
        protected IntPtr simConnectHandle = IntPtr.Zero;
        protected Thread simConnectThread = null;
        private static bool cancelThread = false;

        protected bool isSimConnected = false;
        protected bool isMobiConnected = false;
        protected bool isReceiveRunning = false;
        public bool IsConnected { get { return isSimConnected && isMobiConnected; } }
        public bool IsReady { get { return IsConnected && isReceiveRunning; } }

        public bool SimIsPaused { get; private set; }
        public bool SimIsRunning { get; private set; }
        private const int fpsLen = 60;
        private float[] fpsStatistic;
        private int fpsIndex;

        protected uint nextID = 1;
        protected const int reorderTreshold = 150;
        protected Dictionary<string, uint> addressToIndex = new();
        protected Dictionary<uint, float> simVars = new();

        public MobiSimConnect()
        {
            fpsIndex = -1;
            fpsStatistic = new float[fpsLen];
        }

        public float GetAverageFPS()
        {
            if (fpsIndex == -1)
                return 0;
            else
            {
                return fpsStatistic.Sum() / fpsLen;
            }
        }

        public bool Connect()
        {
            try
            {
                if (isSimConnected)
                    return true;
                
                simConnect = new SimConnect(CLIENT_NAME, simConnectHandle, WM_PILOTSDECK_SIMCONNECT, null, 0);
                simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnOpen);
                simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnQuit);
                simConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_OnException);
                
                cancelThread = false;
                simConnectThread = new(new ThreadStart(SimConnect_ReceiveThread))
                {
                    IsBackground = true
                };
                simConnectHandle = new IntPtr(simConnectThread.ManagedThreadId);
                simConnectThread.Start();

                Logger.Log(LogLevel.Information, "MobiSimConnect:Connect", $"SimConnect Connection open");
                return true;
            }
            catch (Exception ex)
            {
                simConnectThread = null;
                simConnectHandle = IntPtr.Zero;
                cancelThread = true;
                simConnect = null;

                Logger.Log(LogLevel.Error, "MobiSimConnect:Connect", $"Exception while opening SimConnect! (Exception: {ex.GetType()} {ex.Message})");
            }

            return false;
        }

        protected void SimConnect_OnOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            try
            {
                isSimConnected = true;
                simConnect.OnRecvClientData += new SimConnect.RecvClientDataEventHandler(SimConnect_OnClientData);
                simConnect.OnRecvEvent += new SimConnect.RecvEventEventHandler(SimConnect_OnReceiveEvent);
                simConnect.OnRecvEventFrame += new SimConnect.RecvEventFrameEventHandler(Simconnect_OnRecvEventFrame);
                CreateDataAreaDefaultChannel();
                CreateEventSubscription();
                Logger.Log(LogLevel.Information, "MobiSimConnect:SimConnect_OnOpen", $"SimConnect OnOpen received");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MobiSimConnect:SimConnect_OnOpen", $"Exception during SimConnect OnOpen! (Exception: {ex.GetType()} {ex.Message})");
            }
        }

        protected void SimConnect_ReceiveThread()
        {
            ulong ticks = 0;
            int delay = 100;
            int repeat = 5000 / delay;
            int errors = 0;
            isReceiveRunning = true;
            while (!cancelThread && simConnect != null && isReceiveRunning)
            {
                try
                {
                    simConnect.ReceiveMessage();

                    if (isSimConnected && !isMobiConnected && ticks % (ulong)repeat == 0)
                    {
                        Logger.Log(LogLevel.Debug, "MobiSimConnect:SimConnect_ReceiveThread", $"Sending Ping to MobiFlight WASM Module");
                        SendMobiWasmCmd("MF.DummyCmd");
                        SendMobiWasmCmd("MF.Ping");
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    if (errors > 6)
                    {
                        isReceiveRunning = false;
                        Logger.Log(LogLevel.Error, "MobiSimConnect:SimConnect_ReceiveThread", $"Maximum Errors reached, closing Receive Thread! (Exception: {ex.GetType()})");
                        return;
                    }
                }
                Thread.Sleep(delay);
                ticks++;
            }
            isReceiveRunning = false;
            return;
        }

        protected void CreateEventSubscription()
        {
            //simConnect.MapClientEventToSimEvent(SIM_EVENTS.SIM, "SIM");
            //simConnect.AddClientEventToNotificationGroup(NOTFIY_GROUP.GROUP0, SIM_EVENTS.SIM, false);
            //simConnect.MapClientEventToSimEvent(SIM_EVENTS.PAUSE, "PAUSE");
            //simConnect.AddClientEventToNotificationGroup(NOTFIY_GROUP.GROUP0, SIM_EVENTS.PAUSE, false);
            //simConnect.MapClientEventToSimEvent(SIM_EVENTS.FRAME, "FRAME");
            //simConnect.AddClientEventToNotificationGroup(NOTFIY_GROUP.GROUP0, SIM_EVENTS.FRAME, false);
            simConnect.SubscribeToSystemEvent(SIM_EVENTS.SIM, "sim");
            simConnect.SubscribeToSystemEvent(SIM_EVENTS.PAUSE, "pause");
            simConnect.SubscribeToSystemEvent(SIM_EVENTS.FRAME, "frame");
        }

        protected void CreateDataAreaDefaultChannel()
        {
            simConnect.MapClientDataNameToID(MOBIFLIGHT_CLIENT_DATA_NAME_COMMAND, MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_CMD);

            simConnect.MapClientDataNameToID(MOBIFLIGHT_CLIENT_DATA_NAME_RESPONSE, MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE);

            simConnect.AddToClientDataDefinition((SIMCONNECT_DEFINE_ID)0, 0, MOBIFLIGHT_MESSAGE_SIZE, 0, 0);
            simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ResponseString>((SIMCONNECT_DEFINE_ID)0);
            simConnect.RequestClientData(MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE,
                (SIMCONNECT_REQUEST_ID)0,
                (SIMCONNECT_DEFINE_ID)0,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0);
        }

        protected void CreateDataAreaClientChannel()
        {
            simConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_COMMAND, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD);

            simConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_RESPONSE, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE);

            simConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_SIMVAR, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_LVARS);

            simConnect.AddToClientDataDefinition((SIMCONNECT_DEFINE_ID)0, 0, MOBIFLIGHT_MESSAGE_SIZE, 0, 0);
            simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ResponseString>((SIMCONNECT_DEFINE_ID)0);
            simConnect.RequestClientData(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE,
                (SIMCONNECT_REQUEST_ID)0,
                (SIMCONNECT_DEFINE_ID)0,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0);
        }

        protected void SimConnect_OnClientData(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA data)
        {
            try
            {
                if (data.dwRequestID == 0)
                {
                    var request = (ResponseString)data.dwData[0];
                    if (request.Data == "MF.Pong")
                    {
                        if (!isMobiConnected)
                        {
                            Logger.Log(LogLevel.Information, "MobiSimConnect:SimConnect_OnClientData", $"MobiFlight WASM Ping acknowledged - opening Client Connection");
                            SendMobiWasmCmd($"MF.Clients.Add.{CLIENT_NAME}");
                        }
                    }
                    if (request.Data == $"MF.Clients.Add.{CLIENT_NAME}.Finished")
                    {
                        CreateDataAreaClientChannel();
                        isMobiConnected = true;
                        SendClientWasmCmd("MF.SimVars.Clear");
                        SendClientWasmCmd($"MF.Config.MAX_VARS_PER_FRAME.Set.{ServiceModel.MfLvarsPerFrame}");
                        Logger.Log(LogLevel.Information, "MobiSimConnect:SimConnect_OnClientData", $"MobiFlight WASM Client Connection opened");
                    }
                }
                else
                {
                    var simData = (ClientDataValue)data.dwData[0];
                    if (simVars.ContainsKey(data.dwRequestID))
                    {
                        simVars[data.dwRequestID] = simData.data;
                    }
                    else
                        Logger.Log(LogLevel.Warning, "MobiSimConnect:SimConnect_OnClientData", $"The received ID '{data.dwRequestID}' is not subscribed! (Data: {data})");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MobiSimConnect:SimConnect_OnClientData", $"Exception during SimConnect OnClientData! (Exception: {ex.GetType()}) (Data: {data})");
            }
        }

        protected void SimConnect_OnQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Disconnect();
        }

        public void Disconnect()
        {
            try
            {
                if (isMobiConnected)
                    SendClientWasmCmd("MF.SimVars.Clear");

                cancelThread = true;
                if (simConnectThread != null)
                {
                    simConnectThread.Interrupt();
                    simConnectThread.Join(500);
                    simConnectThread = null;
                }

                if (simConnect != null)
                {
                    simConnect.Dispose();
                    simConnect = null;
                    simConnectHandle = IntPtr.Zero;
                }

                isSimConnected = false;
                isMobiConnected = false;

                nextID = 1;
                simVars.Clear();
                addressToIndex.Clear();
                Logger.Log(LogLevel.Information, "MobiSimConnect:Disconnect", $"SimConnect Connection closed");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MobiSimConnect:Disconnect", $"Exception during disconnecting from SimConnect! (Exception: {ex.GetType()} {ex.Message})");
            }
        }

        private void Simconnect_OnRecvEventFrame(SimConnect sender, SIMCONNECT_RECV_EVENT_FRAME recEvent)
        {
            if (SimIsRunning && !SimIsPaused && recEvent != null)
            {
                if (fpsIndex == -1)
                {
                    fpsIndex = 1;
                    for (int i = 0; i < fpsStatistic.Length; i++)
                    {
                        fpsStatistic[i] = recEvent.fFrameRate;
                    }
                }
                else
                {
                    fpsStatistic[fpsIndex] = recEvent.fFrameRate;
                    fpsIndex++;
                    if (fpsIndex >= fpsStatistic.Length)
                        fpsIndex = 0;
                }
            }
        }

        private void SimConnect_OnReceiveEvent(SimConnect sender, SIMCONNECT_RECV_EVENT recEvent)
        {
            if (recEvent != null)
            {
                if (recEvent.uEventID == (uint)SIM_EVENTS.PAUSE)
                {
                    if (recEvent.dwData == 1)
                        SimIsPaused = true;
                    else
                        SimIsPaused = false;
                }
                else if (recEvent.uEventID == (uint)SIM_EVENTS.SIM)
                {
                    if (recEvent.dwData == 1)
                        SimIsRunning = true;
                    else
                        SimIsRunning = false;
                }
            }
        }

        public void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }

        private void SendClientWasmCmd(string command)
        {
            SendWasmCmd(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD, (MOBIFLIGHT_CLIENT_DATA_ID)0, command);
        }

        private void SendClientWasmDummyCmd()
        {
            SendWasmCmd(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD, (MOBIFLIGHT_CLIENT_DATA_ID)0, "MF.DummyCmd");
        }

        private void SendMobiWasmCmd(string command)
        {
            SendWasmCmd(MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_CMD, (MOBIFLIGHT_CLIENT_DATA_ID)0, command);
        }

        private void SendWasmCmd(Enum cmdChannelId, Enum cmdId, string command)
        {
            simConnect.SetClientData(cmdChannelId, cmdId, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, new ClientDataString(command));
        }

        protected void SimConnect_OnException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            if (data.dwException != 3 && data.dwException != 29)
                Logger.Log(LogLevel.Error, "MobiSimConnect:SimConnect_OnException", $"Exception received: (Exception: {data.dwException})");
        }

        public void SubscribeLvar(string address)
        {
            SubscribeVariable($"(L:{address})");
        }

        public void SubscribeSimVar(string name, string unit)
        {
            SubscribeVariable($"(A:{name}, {unit})");
        }

        protected void SubscribeVariable(string address)
        {
            try
            {
                if (!addressToIndex.ContainsKey(address))
                {
                    RegisterVariable(nextID, address);
                    simVars.Add(nextID, 0.0f);
                    addressToIndex.Add(address, nextID);

                    nextID++;
                }
                else
                    Logger.Log(LogLevel.Warning, "MobiSimConnect:SubscribeAddress", $"The Address '{address}' is already subscribed");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MobiSimConnect:SubscribeAddress", $"Exception while subscribing SimVar '{address}'! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
        }

        protected void RegisterVariable(uint ID, string address)
        {
            simConnect.AddToClientDataDefinition(
                (SIMCONNECT_DEFINE_ID)ID,
                (ID - 1) * sizeof(float),
                sizeof(float),
                0,
                0);

            simConnect?.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ClientDataValue>((SIMCONNECT_DEFINE_ID)ID);

            simConnect?.RequestClientData(
                PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_LVARS,
                (SIMCONNECT_REQUEST_ID)ID,
                (SIMCONNECT_DEFINE_ID)ID,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0
            );

            SendClientWasmCmd($"MF.SimVars.Add.{address}");
        }

        public void UnsubscribeAll()
        {
            try
            {
                SendClientWasmCmd("MF.SimVars.Clear");
                nextID = 1;
                simVars.Clear();
                addressToIndex.Clear();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MobiSimConnect:UnsubscribeAll", $"Exception while unsubscribing SimVars! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
        }

        public float ReadLvar(string address)
        {
            if (addressToIndex.TryGetValue($"(L:{address})", out uint index) && simVars.TryGetValue(index, out float value))
                return value;
            else
                return 0;
        }

        public float ReadSimVar(string name, string unit)
        {
            string address = $"(A:{name}, {unit})";
            if (addressToIndex.TryGetValue(address, out uint index) && simVars.TryGetValue(index, out float value))
                return value;
            else
                return 0;
        }

        public void WriteLvar(string address, float value)
        {
            SendClientWasmCmd($"MF.SimVars.Set.{string.Format(new CultureInfo("en-US").NumberFormat, "{0:G}", value)} (>L:{address})");
            SendClientWasmDummyCmd();
        }

        public void ExecuteCode(string code)
        {
            SendClientWasmCmd($"MF.SimVars.Set.{code}");
            SendClientWasmDummyCmd();
        }
    }
}
