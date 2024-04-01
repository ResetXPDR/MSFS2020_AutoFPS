/*
    Here is a short overview of all fields accessible via the shared memory.
    You can get an updated list if you call ListAllData(true) and ListAllSensors(true).
 
    # DATA FIELDS #

    [0]FillratePixel: 13.2
    [1]SubvendorID: 1002
    [2]GPURevision: 
    [3]MemType: GDDR3
    [4]MemBusWidth: 256
    [5]VendorID: 1002
    [6]MultiGPUName: ATI CrossFire
    [7]BusInterface: PCI-E x16 @ x16
    [8]BIOSVersion: VER010.075.000.002.027526
    [9]ClockShader: 
    [10]MultiGPU0: Enabled (2 GPUs) (unsure on Vista64)
    [11]ShaderModel: 4.1
    [12]ClockMem: 900
    [13]DriverVersion: atiumdag 7.14.10.0590  / Vista64
    [14]Vendor: ATI
    [15]DeviceID: 950F
    [16]ProcessSize: 55
    [17]FillrateTexel: 13.2
    [18]Subvendor: ATI
    [19]NumShadersUnified: 320
    [20]NumROPs: 16
    [21]DieSize: 190
    [22]ClockGPUDefault: 823
    [23]MemSize: 512
    [24]NumShadersVertex: 
    [25]DirectXSupport: 10.1
    [26]CardName: ATI Radeon HD 3870 X2
    [27]ClockShaderDefault: 
    [28]ClockMemDefault: 900
    [29]MemBandwidth: 57.6
    [30]ClockGPU: 823
    [31]SubsysID: 2042
    [32]GPUName: R680

    # SENSOR FIELDS #

    [0]GPU Core Clock: 823 MHz
    [1]GPU Memory Clock: 900 MHz
    [2]GPU Temperature: 56 °C
    [3]Fan Speed: 40 %%
    [4]GPU Load: 0 %%
    [5]VDDC Current: 4,51612903225806 A
    [6]VDDC Slave #1 Temperature: 60 °C
    [7]VDDC Slave #2 Temperature: 60 °C
    [8]VDDC: 1,3125 V
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace MSFS2020_AutoFPS
{
    class GpuzWrapper
    {
        [DllImport(@"GpuzShMem.x64.dll", SetLastError = true)]
        public static extern int InitGpuzShMem();

        [DllImport(@"GpuzShMem.x64.dll", SetLastError = true)]
        public static extern int RemGpuzShMem();

        [DllImport(@"GpuzShMem.x64.dll", SetLastError = true)]
        public static extern IntPtr GetSensorName(int index);

        [DllImport(@"GpuzShMem.x64.dll", SetLastError = true)]
        public static extern double GetSensorValue(int index);

        [DllImport(@"GpuzShMem.x64.dll", SetLastError = true)]
        public static extern IntPtr GetSensorUnit(int index);

        [DllImport(@"GpuzShMem.x64.dll", SetLastError = true)]
        public static extern IntPtr GetDataKey(int index);

        [DllImport(@"GpuzShMem.x64.dll", SetLastError = true)]
        public static extern IntPtr GetDataValue(int index);

        /// <summary>
        /// Opens the shared memory interface for reading. Don't forget to close it if you don't need it anymore!
        /// </summary>
        /// <exception cref="Exception">If the shared memory could not be opened.</exception>
        public bool Open()
        {
            if (InitGpuzShMem() != 0)
            {
                return false;
            }
            return true;
        }

        
        /// <summary>
        /// Closes the shared memory interface.
        /// </summary>
        public void Close()
        {
            RemGpuzShMem();
        }

        
        /// <summary>
        /// Gets the name of the specified sensor field (eg. "GPU Core Clock", "Fan Speed", ...).
        /// </summary>
        /// <param name="index">Index of sensor field needed.</param>
        /// <returns>Name of the sensor field.</returns>
        public string SensorName(int index)
        {
            return Marshal.PtrToStringUni(GetSensorName(index));
        }

        
        /// <summary>
        /// Gets the value of the specified sensor field (eg. 900.0, 56.0, ...).
        /// </summary>
        /// <param name="index">Index of sensor field needed.</param>
        /// <returns>Value of the sensor field.</returns>
        public double SensorValue(int index)
        {
            return GetSensorValue(index);
        }

        
        /// <summary>
        /// Gets the unit of the specified sensor field (e.g. "MHz", "°C", ...).
        /// </summary>
        /// <param name="index">Index of sensor field needed.</param>
        /// <returns>Unit of the sensor field.</returns>
        public string SensorUnit(int index)
        {
            return Marshal.PtrToStringUni(GetSensorUnit(index));
        }

        
        /// <summary>
        /// Gets the key (=name) of the specified data field (eg. "FillratePixel", "Vendor", ...).
        /// </summary>
        /// <param name="index">Index of data field needed.</param>
        /// <returns>Key of the data field.</returns>
        public string DataKey(int index)
        {
            return Marshal.PtrToStringUni(GetDataKey(index));
        }

        
        /// <summary>
        /// Gets the value of the specified data field (eg. "13.2", "ATI", ...).
        /// </summary>
        /// <param name="index">Index of data field needed.</param>
        /// <returns>Value of the data field.</returns>
        public string DataValue(int index)
        {
            return Marshal.PtrToStringUni(GetDataValue(index));
        }

        /// <summary>
        /// Returns a list of all sensor fields available.
        /// </summary>
        /// <returns>A formated string of all sensor names, values and units, each triple a line.</returns>
        public string ListAllSensors()
        {
            String s, res = String.Empty;

            for (int i = 0; (s = SensorName(i)) != String.Empty; i++)
                res += "[" + i + "]" + s + ": " + SensorValue(i) + " " + SensorUnit(i) + "\n";

            return res;
        }

        /// <summary>
        /// Returns a list of all data fields available.
        /// </summary>
        /// <returns>A formated string of all data keys and values, each pair a line.</returns>
        public string ListAllData()
        {
            String s, res = String.Empty;

            for (int i = 0; (s = DataKey(i)) != String.Empty; i++)
                res += "[" + i + "]" + s + ": " + DataValue(i) + "\n";

            return res;
        }
    }
}