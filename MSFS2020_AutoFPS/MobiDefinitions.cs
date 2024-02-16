using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MSFS2020_AutoFPS
{
    public enum MOBIFLIGHT_CLIENT_DATA_ID
    {
        MOBIFLIGHT_LVARS,
        MOBIFLIGHT_CMD,
        MOBIFLIGHT_RESPONSE
    }

    public enum PILOTSDECK_CLIENT_DATA_ID
    {
        MOBIFLIGHT_LVARS = 1989,
        MOBIFLIGHT_CMD,
        MOBIFLIGHT_RESPONSE
    }

    public enum SIMCONNECT_REQUEST_ID
    {
        Dummy = 0
    }

    public enum SIMCONNECT_DEFINE_ID
    {
        Dummy = 0
    }

    public enum SIMCONNECT_NOTIFICATION_GROUP_ID
    {
        SIMCONNECT_GROUP_PRIORITY_DEFAULT,
        SIMCONNECT_GROUP_PRIORITY_HIGHEST
    }

    public enum SIM_EVENTS
    {
        SIM,
        PAUSE,
        FRAME
    };

    //public enum NOTFIY_GROUP
    //{
    //    GROUP0
    //};

    public class SimVar
    {
        public UInt32 ID { get; set; }
        public String Name { get; set; }
        public float Data { get; set; }

        public SimVar(uint iD, float data = 0.0f)
        {
            ID = iD;
            Data = data;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ClientDataValue
    {
        public float data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ClientDataString
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)MobiSimConnect.MOBIFLIGHT_MESSAGE_SIZE)]
        public byte[] data;

        public ClientDataString(string strData)
        {
            byte[] txtBytes = Encoding.ASCII.GetBytes(strData);
            var ret = new byte[1024];
            Array.Copy(txtBytes, ret, txtBytes.Length);
            data = ret;
        }
    }

    public struct ResponseString
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)MobiSimConnect.MOBIFLIGHT_MESSAGE_SIZE)]
        public String Data;
    }
}
