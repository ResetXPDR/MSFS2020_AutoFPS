using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MSFS2020_AutoFPS
{
    public static class MemoryInterface
    {
        public const int PROCESS_VM_OPERATION = 0x0008;
        public const int PROCESS_VM_READ = 0x0010;
        public const int PROCESS_VM_WRITE = 0x0020;

        private static Process proc;
        private static IntPtr procHandle;

        public static long GetModuleAddress(string Name)
        {
            try
            {
                if (proc != null)
                {
                    foreach (ProcessModule ProcMod in proc.Modules)
                    {
                        if (Name == ProcMod.ModuleName)
                            return (long)ProcMod.BaseAddress;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryInterface:GetModuleAddress", $"Exception {ex}: {ex.Message}");
            }

            return -1;
        }

        public static bool Attach(string name)
        {
            try
            {
                if (Process.GetProcessesByName(name).Length > 0)
                {
                    proc = Process.GetProcessesByName(name)[0];
                    procHandle = NativeMethods.OpenProcess(PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE, false, proc.Id);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryInterface:Attach", $"Exception {ex}: {ex.Message}");
            }

            return false;
        }

        public static void WriteMemory<T>(long Address, object Value)
        {
            try
            {
                var buffer = StructureToByteArray(Value);
                NativeMethods.NtWriteVirtualMemory(checked((int)procHandle), Address, buffer, buffer.Length, out _);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryInterface:WriteMemory", $"Exception {ex}: {ex.Message}");
            }
        }

        public static T ReadMemory<T>(long address) where T : struct
        {
            try
            {
                var ByteSize = Marshal.SizeOf(typeof(T));

                var buffer = new byte[ByteSize];

                NativeMethods.NtReadVirtualMemory(checked((int)procHandle), address, buffer, buffer.Length, out _);

                return ByteArrayToStructure<T>(buffer);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "MemoryInterface:ReadMemory", $"Exception {ex}: {ex.Message}");
            }

            return default;
        }

        private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        private static byte[] StructureToByteArray(object obj)
        {
            var length = Marshal.SizeOf(obj);

            var array = new byte[length];

            var pointer = Marshal.AllocHGlobal(length);

            Marshal.StructureToPtr(obj, pointer, true);
            Marshal.Copy(pointer, array, 0, length);
            Marshal.FreeHGlobal(pointer);

            return array;
        }
    }

    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("ntdll.dll")]
        internal static extern IntPtr NtWriteVirtualMemory(int ProcessHandle, long BaseAddress, byte[] Buffer, int NumberOfBytesToWrite, out int NumberOfBytesWritten);
        [DllImport("ntdll.dll")]
        internal static extern bool NtReadVirtualMemory(int ProcessHandle, long BaseAddress, byte[] Buffer, int NumberOfBytesToRead, out int NumberOfBytesRead);
    }
}
