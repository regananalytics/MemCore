// Adapted from ProcessMemory ProcessMemHandler.cs
// See included license note LICENSE.ProcessMemory

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static MemCore.PInvoke;

namespace MemCore
{
    public unsafe class MemHandler : IDisposable
    {
        private const int STILL_ACTIVE = 259;
        public readonly IntPtr ProcessHandle = IntPtr.Zero;
        
        public MemHandler(int pid, bool readOnly = true)
        {
            var accessFlags = readOnly 
                ? ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VirtualMemoryRead 
                : ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VirtualMemoryRead | ProcessAccessFlags.VirtualMemoryOperation | ProcessAccessFlags.VirtualMemoryWrite;
                
            ProcessHandle = OpenProcess(accessFlags, false, pid);
        }

        public bool ProcessRunning
        {
            get 
            { 
                int exitCode = 0;
                bool success = GetExitCodeProcess(ProcessHandle, ref exitCode);
                return success && exitCode == STILL_ACTIVE;
            }
        }

        public int ProcessExitCode
        {
            get
            {
                int exitCode = 0;
                GetExitCodeProcess(ProcessHandle, ref exitCode);
                return exitCode;
            }
        }

        private static readonly int memBasicInfoSize = sizeof(MEMORY_BASIC_INFORMATION32);

        private string GetMemProtectFlags(IntPtr offset)
        {
            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                MEMORY_BASIC_INFORMATION32 memBasicInfo = new MEMORY_BASIC_INFORMATION32();
                VirtualQueryEx(ProcessHandle, offset, out memBasicInfo, memBasicInfoSize);

                sb.AppendLine("[MEMORY_BASIC_INFORMATION32]")
                    .AppendLine($"BaseAddress: {memBasicInfo.BaseAddress}\r\n")
                    .AppendLine($"AllocationBase: {memBasicInfo.AllocationBase}\r\n")
                    .AppendLine($"AllocationProtect: {memBasicInfo.AllocationProtect}\r\n")
                    .AppendLine($"RegionSize: {memBasicInfo.RegionSize}\r\n")
                    .AppendLine($"State: {memBasicInfo.State}\r\n")
                    .AppendLine($"Protect: {memBasicInfo.Protect}\r\n")
                    .AppendLine($"Type: {memBasicInfo.Type}\r\n");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"[GetMemoryProtectFlags EXCEPTION: {ex.ToString()}]";
            }
        }

        // Generic
        public bool TryGetAt<T>(IntPtr address, ref T value) where T : unmanaged
        {
            fixed (T* pointer = &value)
                return ReadProcessMemory(ProcessHandle, address, pointer, sizeof(T), out IntPtr bytesRead);
        }
        public bool TryGetAt<T>(int* address, ref T value) where T : unmanaged
        {
            fixed (T* pointer = &value)
                return ReadProcessMemory(ProcessHandle, address, pointer, sizeof(T), out IntPtr bytesRead);
        }

        public bool TrySetAt<T>(IntPtr address, ref T value) where T : unmanaged
        {
            fixed (T* pointer = &value)
                return WriteProcessMemory(ProcessHandle, address, pointer, sizeof(T), out IntPtr bytesRead);
        }
        public bool TrySetAt<T>(int* address, ref T value) where T : unmanaged
        {
            fixed (T* pointer = &value)
                return WriteProcessMemory(ProcessHandle, address, pointer, sizeof(T), out IntPtr bytesRead);
        }

        // SByte
        public bool TryGetSByteAt(IntPtr address, ref sbyte result) => TryGetAt(address, ref result);
        public bool TryGetSByteAt(IntPtr address, sbyte* result) => TryGetAt(address, ref *result);
        public bool TryGetSByteAt(int* address, sbyte* result) => TryGetAt(address, ref *result);
        
        public bool TrySetSByteAt(IntPtr address, ref sbyte value) => TrySetAt(address, ref value);
        public bool TrySetSByteAt(int* address, ref sbyte value) => TrySetAt(address, ref value);

        // Byte
        public bool TryGetByteAt(IntPtr address, ref byte result) => TryGetAt(address, ref result);
        public bool TryGetByteAt(IntPtr address, byte* result) => TryGetAt(address, ref *result);
        public bool TryGetByteAt(int* address, byte* result) => TryGetAt(address, ref *result);
        
        public bool TrySetByteAt(IntPtr address, ref byte value) => TrySetAt(address, ref value);
        public bool TrySetByteAt(int* address, ref byte value) => TrySetAt(address, ref value);


        // Byte Array
        public bool TryGetByteArrayAt(IntPtr offset, int size, IntPtr result)
            => ReadProcessMemory(ProcessHandle, offset, result, size, out IntPtr _);
        public bool TryGetByteArrayAt(int* offset, int size, IntPtr result) 
            => TryGetByteArrayAt((IntPtr)offset, size, result);
        public bool TryGetByteArrayAt(IntPtr offset, int size, void* result)
            => ReadProcessMemory(ProcessHandle, offset, result, size, out IntPtr _);
        public bool TryGetByteArrayAt(int* offset, int size, void* result)
            => TryGetByteArrayAt((IntPtr)offset, size, result);

        public bool TrySetByteArrayAt(IntPtr offset, int size, IntPtr result)
            => WriteProcessMemory(ProcessHandle, offset, result, size, out IntPtr _);
        public bool TrySetByteArrayAt(int* offset, int size, IntPtr result)
            => TrySetByteArrayAt((IntPtr)offset, size, result);
        public bool TrySetByteArrayAt(IntPtr offset, int size, void* result)
            => WriteProcessMemory(ProcessHandle, offset, result, size, out IntPtr _);
        public bool TrySetByteArrayAt(int* offset, int size, void* result)
            => TrySetByteArrayAt((IntPtr)offset, size, result);

        // Short
        public bool TryGetShortAt(IntPtr address, ref short result) => TryGetAt(address, ref result);
        public bool TryGetShortAt(IntPtr address, short* result) => TryGetAt(address, ref *result);
        public bool TryGetShortAt(int* address, short* result) => TryGetAt(address, ref *result);
        
        public bool TrySetShortAt(IntPtr address, ref short value) => TrySetAt(address, ref value);
        public bool TrySetShortAt(int* address, ref short value) => TrySetAt(address, ref value);

        // UShort
        public bool TryGetUShortAt(IntPtr address, ref ushort result) => TryGetAt(address, ref result);
        public bool TryGetUShortAt(IntPtr address, ushort* result) => TryGetAt(address, ref *result);
        public bool TryGetUShortAt(int* address, ushort* result) => TryGetAt(address, ref *result);
        
        public bool TrySetUShortAt(IntPtr address, ref ushort value) => TrySetAt(address, ref value);
        public bool TrySetUShortAt(int* address, ref ushort value) => TrySetAt(address, ref value);

        // Int
        public bool TryGetIntAt(IntPtr address, ref int result) => TryGetAt(address, ref result);
        public bool TryGetIntAt(IntPtr address, int* result) => TryGetAt(address, ref *result);
        public bool TryGetIntAt(int* address, int* result) => TryGetAt(address, ref *result);
        
        public bool TrySetIntAt(IntPtr address, ref int value) => TrySetAt(address, ref value);
        public bool TrySetIntAt(int* address, ref int value) => TrySetAt(address, ref value);

        // UInt
        public bool TryGetUIntAt(IntPtr address, ref uint result) => TryGetAt(address, ref result);
        public bool TryGetUIntAt(IntPtr address, uint* result) => TryGetAt(address, ref *result);
        public bool TryGetUIntAt(int* address, uint* result) => TryGetAt(address, ref *result);
        
        public bool TrySetUIntAt(IntPtr address, ref uint value) => TrySetAt(address, ref value);
        public bool TrySetUIntAt(int* address, ref uint value) => TrySetAt(address, ref value);

        // Long
        public bool TryGetLongAt(IntPtr address, ref long result) => TryGetAt(address, ref result);
        public bool TryGetLongAt(IntPtr address, long* result) => TryGetAt(address, ref *result);
        public bool TryGetLongAt(int* address, long* result) => TryGetAt(address, ref *result);
        
        public bool TrySetLongAt(IntPtr address, ref long value) => TrySetAt(address, ref value);
        public bool TrySetLongAt(int* address, ref long value) => TrySetAt(address, ref value);

        // ULong
        public bool TryGetULongAt(IntPtr address, ref ulong result) => TryGetAt(address, ref result);
        public bool TryGetULongAt(IntPtr address, ulong* result) => TryGetAt(address, ref *result);
        public bool TryGetULongAt(int* address, ulong* result) => TryGetAt(address, ref *result);
        
        public bool TrySetULongAt(IntPtr address, ref ulong value) => TrySetAt(address, ref value);
        public bool TrySetULongAt(int* address, ref ulong value) => TrySetAt(address, ref value);

        // Float
        public bool TryGetFloatAt(IntPtr address, ref float result) => TryGetAt(address, ref result);
        public bool TryGetFloatAt(IntPtr address, float* result) => TryGetAt(address, ref *result);
        public bool TryGetFloatAt(int* address, float* result) => TryGetAt(address, ref *result);
        
        public bool TrySetFloatAt(IntPtr address, ref float value) => TrySetAt(address, ref value);
        public bool TrySetFloatAt(int* address, ref float value) => TrySetAt(address, ref value);

        // Double
        public bool TryGetDoubleAt(IntPtr address, ref double result) => TryGetAt(address, ref result);
        public bool TryGetDoubleAt(IntPtr address, double* result) => TryGetAt(address, ref *result);
        public bool TryGetDoubleAt(int* address, double* result) => TryGetAt(address, ref *result);
        
        public bool TrySetDoubleAt(IntPtr address, ref double value) => TrySetAt(address, ref value);
        public bool TrySetDoubleAt(int* address, ref double value) => TrySetAt(address, ref value);

        // Unicode
        // public bool TryGetUnicodeStringAt(IntPtr address, int size, ref long result)
        // {
        //     Span<byte> stringSpan = new byte[size];
        //     fixed(byte* bp = stringSpan)
        //         if (TryGetByteArrayAt(address, size, bp))
        //         {
        //             result = stringSpan.FromUnicodeBytes();
        //             return true;
        //         }
        //     return false;
        // }
        // public bool TryGetUnicodeStringAt(int* address, int size, ref string result) 
        //     => TryGetUnicodeStringAt((IntPtr)address, size, ref result);

        // ASCII
        // public bool TryGetASCIIStringAt(IntPtr address, ref long result)
        // {
        //     Span<byte> stringSpan = new byte[size];
        //     fixed(byte* bp = stringSpan)
        //         if (TryGetByteArrayAt(address, size, bp))
        //         {
        //             result = stringSpan.FromASCIIBytes();
        //             return true;
        //         }
        //     return false;
        // }
        // public bool TryGetASCIIStringAt(int* address, int size, ref string result) 
        //     => TryGetASCIIStringAt((IntPtr)address, size, ref result);

        // IDisposable
        #region IDisposable Support
        protected virtual void Dispose(bool disposing) => CloseHandle(ProcessHandle);
        public void Dispose() => Dispose(true);
        #endregion
    }

    public class MemoryProtectionScope : IDisposable
    {
        private readonly IntPtr _address;
        private readonly int _size;
        private readonly IntPtr _processHandle;
        private uint _oldProtect;

        public MemoryProtectionScope(IntPtr processHandle, IntPtr address, int size)
        {
            _address = address;
            _size = size;
            _processHandle = processHandle;

            // Change memory protection to PAGE_EXECUTE_READWRITE
            if (!VirtualProtectEx(_processHandle, _address, _size, (uint)AllocationProtect.PAGE_EXECUTE_READWRITE, out _oldProtect))
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to change memory protection at {_address}. Error code: {errorCode}");
            }
        }

        public void Dispose()
        {
            // Replace original memory protection when disposed
            VirtualProtectEx(_processHandle, _address, _size, _oldProtect, out _);
        }
    }

    public class TemporaryNopScope: IDisposable
    {
        private readonly IntPtr _address;
        private readonly int _size;
        private readonly byte[] _originalOps;
        private readonly IntPtr _processHandle;
        
        public TemporaryNopScope(IntPtr processHandle, IntPtr address, int size)
        {
            _address = address;
            _size = size;
            _processHandle = processHandle;

            // Save original instructions
            _originalOps = new byte[_size];
            ReadProcessMemory(_processHandle, _address, _originalOps, _size, out _);

            // Fill with NOPs
            byte[] nopOps = new byte[_size];
            for (int i = 0; i < _size; i++)
            {
                nopOps[i] = 0x90; // NOP instruction
            }

            // Use MemoryProtectionScope to temporarily allow writes
            using (new MemoryProtectionScope(_processHandle, _address, _size))
            {
                WriteProcessMemory(_processHandle, _address, nopOps, _size, out _);
            }
        }

        public void Dispose()
        {
            using (new MemoryProtectionScope(_processHandle, _address, _size))
            {
                WriteProcessMemory(_processHandle, _address, _originalOps, _size, out _);
            }
        }
    }
}