// Adapted from ProcessMemory MultilevelPointer.cs
// See included license note LICENSE.ProcessMemory

using System;

namespace MemCore
{
    public unsafe class MemPointer
    {
        private MemHandler memHandler;
        public IntPtr BaseAddress { get => (IntPtr)_baseAddress; }
        public IntPtr Address { get => (IntPtr)_address;}

        private readonly int* _baseAddress;
        private int _address;
        private int[]? offsets;
        public bool IsNullPointer => _address == 0;

        public MemPointer(MemHandler memHandler, IntPtr baseAddress) : this(memHandler, (int*)baseAddress.ToPointer()) { }
        public MemPointer(MemHandler memHandler, IntPtr baseAddress, params int[] offsets) : this(memHandler, (int*)baseAddress.ToPointer(), offsets) { }

        public MemPointer(MemHandler memHandler, int* baseAddress)
        {
            this._address = 0;
            this.memHandler = memHandler;
            this._baseAddress = baseAddress;
            this.offsets = null;
            UpdatePointers();
        }

        public MemPointer(MemHandler memHandler, int* baseAddress, params int[] offsets)
        {
            this._address = 0;
            this.memHandler = memHandler;
            this._baseAddress = baseAddress;
            this.offsets = offsets;
            UpdatePointers();
        }

        public unsafe void UpdatePointers()
        {
            fixed (int* p = &_address)
                memHandler.TryGetIntAt(_baseAddress, p);
            
            if (_address == 0)
                return;

            if (offsets != null)
            {
                foreach (int offset in offsets)
                {
                    fixed (int* p = &_address)
                        memHandler.TryGetIntAt((int*)(_address + offset), p);
                    
                    if (_address == 0)
                        return;
                }
            }
        }

        // Generic
        public bool TryDeref<T>(int offset, ref T result) where T : unmanaged
            => !IsNullPointer && this.memHandler.TryGetAt(IntPtr.Add(Address, offset), ref result);
        public bool TryDeref<T>(int offset, T* result) where T : unmanaged
            => (!IsNullPointer && result != (T*)0) ? this.memHandler.TryGetAt<T>((int*)(_address + offset), ref *result) : false;

        public bool TryWrite<T>(int offset, ref T value) where T : unmanaged
            => !IsNullPointer && this.memHandler.TrySetAt(IntPtr.Add(Address, offset), ref value);
        public bool TryWrite<T>(int offset, T* value) where T : unmanaged
            => (!IsNullPointer && value != (T*)0) ? this.memHandler.TrySetAt((int*)(_address + offset), ref *value) : false;

        // SByte
        public bool TryDerefSByte(int offset, ref sbyte result)
            => !IsNullPointer && this.memHandler.TryGetSByteAt(IntPtr.Add(Address, offset), ref result);
        public bool TryDerefSByte(int offset, sbyte* result)
            => (!IsNullPointer && result != (sbyte*)0) ? this.memHandler.TryGetSByteAt((int*)(_address + offset), result) : false;

        public bool TryWriteSByte(int offset, ref sbyte value)
            => !IsNullPointer && this.memHandler.TrySetSByteAt(IntPtr.Add(Address, offset), ref value);
        public bool TryWriteSByte(int offset, sbyte* value)
            => (!IsNullPointer && value != (sbyte*)0) ? this.memHandler.TrySetSByteAt((int*)(_address + offset), ref *value) : false;

        // Byte
        public bool TryDerefByte(int offset, ref byte result)
            => !IsNullPointer && this.memHandler.TryGetByteAt(IntPtr.Add(Address, offset), ref result);
        public bool TryDerefByte(int offset, byte* result)
            => (!IsNullPointer && result != (byte*)0) ? this.memHandler.TryGetByteAt((int*)(_address + offset), result) : false;

        public bool TryWriteByte(int offset, ref byte value)
            => !IsNullPointer && this.memHandler.TrySetByteAt(IntPtr.Add(Address, offset), ref value);
        public bool TryWriteByte(int offset, byte* value)
            => (!IsNullPointer && value != (byte*)0) ? this.memHandler.TrySetByteAt((int*)(_address + offset), ref *value) : false;

        // ByteArray
        public bool TryDerefByteArray(int offset, int size, IntPtr result)
            => !IsNullPointer && result != IntPtr.Zero && this.memHandler.TryGetByteArrayAt(IntPtr.Add(Address, offset), size, result);
        public bool TryDerefByteArray(int offset, int size, byte* result)
            => (!IsNullPointer && result != (byte*)0) ? this.memHandler.TryGetByteArrayAt((int*)(_address + offset), size, result) : false;

        public bool TryWriteByteArray(int offset, int size, IntPtr value)
            => !IsNullPointer && value != IntPtr.Zero && this.memHandler.TrySetByteArrayAt(IntPtr.Add(Address, offset), size, value);
        public bool TryWriteByteArray(int offset, int size, byte* value)
            => (!IsNullPointer && value != (byte*)0) ? this.memHandler.TrySetByteArrayAt((int*)(_address + offset), size, value) : false;

        // Short
        public bool TryDerefShort(int offset, ref short result)
            => !IsNullPointer && this.memHandler.TryGetShortAt(IntPtr.Add(Address, offset), ref result);
        public bool TryDerefShort(int offset, short* result)
            => (!IsNullPointer && result != (short*)0) ? this.memHandler.TryGetShortAt((int*)(_address + offset), result) : false;

        public bool TryWriteShort(int offset, ref short value)
            => !IsNullPointer && this.memHandler.TrySetShortAt(IntPtr.Add(Address, offset), ref value);
        public bool TryWriteShort(int offset, short* value)
            => (!IsNullPointer && value != (short*)0) ? this.memHandler.TrySetShortAt((int*)(_address + offset), ref *value) : false;

        // UShort
        public bool TryDerefUShort(int offset, ref ushort result)
            => !IsNullPointer && this.memHandler.TryGetUShortAt(IntPtr.Add(Address, offset), ref result);
        public bool TryDerefUShort(int offset, ushort* result)
            => (!IsNullPointer && result != (ushort*)0) ? this.memHandler.TryGetUShortAt((int*)(_address + offset), result) : false;

        public bool TryWriteUShort(int offset, ref ushort value)
            => !IsNullPointer && this.memHandler.TrySetUShortAt(IntPtr.Add(Address, offset), ref value);
        public bool TryWriteUShort(int offset, ushort* value)
            => (!IsNullPointer && value != (ushort*)0) ? this.memHandler.TrySetUShortAt((int*)(_address + offset), ref *value) : false;

        // Int
        public bool TryDerefInt(int offset, ref int result)
            => !IsNullPointer && this.memHandler.TryGetIntAt(IntPtr.Add(Address, offset), ref result);
        public bool TryDerefInt(int offset, int* result)
            => (!IsNullPointer && result != (int*)0) ? this.memHandler.TryGetIntAt((int*)(_address + offset), result) : false;

        public bool TryWriteInt(int offset, ref int value)
            => !IsNullPointer && this.memHandler.TrySetIntAt(IntPtr.Add(Address, offset), ref value);
        public bool TryWriteInt(int offset, int* value)
            => (!IsNullPointer && value != (int*)0) ? this.memHandler.TrySetIntAt((int*)(_address + offset), ref *value) : false;

        // UInt
        public bool TryDerefUInt(int offset, ref uint result)
            => !IsNullPointer && this.memHandler.TryGetUIntAt(IntPtr.Add(Address, offset), ref result);
        public bool TryDerefUInt(int offset, uint* result)
            => (!IsNullPointer && result != (uint*)0) ? this.memHandler.TryGetUIntAt((int*)(_address + offset), result) : false;

        public bool TryWriteUInt(int offset, ref uint value)
            => !IsNullPointer && this.memHandler.TrySetUIntAt(IntPtr.Add(Address, offset), ref value);
        public bool TryWriteUInt(int offset, uint* value)
            => (!IsNullPointer && value != (uint*)0) ? this.memHandler.TrySetUIntAt((int*)(_address + offset), ref *value) : false;

        // Long
        public bool TryDerefLong(int offset, ref long result)
            => !IsNullPointer && this.memHandler.TryGetLongAt(IntPtr.Add(Address, offset), ref result);
        public bool TryDerefLong(int offset, long* result)
            => (!IsNullPointer && result != (long*)0) ? this.memHandler.TryGetLongAt((int*)(_address + offset), result) : false;

        public bool TryWriteLong(int offset, ref long value)
            => !IsNullPointer && this.memHandler.TrySetLongAt(IntPtr.Add(Address, offset), ref value);
        public bool TryWriteLong(int offset, long* value)
            => (!IsNullPointer && value != (long*)0) ? this.memHandler.TrySetLongAt((int*)(_address + offset), ref *value) : false;

        // ULong
        public bool TryDerefULong(int offset, ref ulong result)
            => !IsNullPointer && this.memHandler.TryGetULongAt(IntPtr.Add(Address, offset), ref result);
        public bool TryDerefULong(int offset, ulong* result)
            => (!IsNullPointer && result != (ulong*)0) ? this.memHandler.TryGetULongAt((int*)(_address + offset), result) : false;

        public bool TryWriteULong(int offset, ref ulong value)
            => !IsNullPointer && this.memHandler.TrySetULongAt(IntPtr.Add(Address, offset), ref value);
        public bool TryWriteULong(int offset, ulong* value)
            => (!IsNullPointer && value != (ulong*)0) ? this.memHandler.TrySetULongAt((int*)(_address + offset), ref *value) : false;

        // Float
        public bool TryDerefFloat(int offset, ref float result)
            => !IsNullPointer && this.memHandler.TryGetFloatAt(IntPtr.Add(Address, offset), ref result);
        public bool TryDerefFloat(int offset, float* result)
            => (!IsNullPointer && result != (float*)0) ? this.memHandler.TryGetFloatAt((int*)(_address + offset), result) : false;

        public bool TryWriteFloat(int offset, ref float value)
            => !IsNullPointer && this.memHandler.TrySetFloatAt(IntPtr.Add(Address, offset), ref value);
        public bool TryWriteFloat(int offset, float* value)
            => (!IsNullPointer && value != (float*)0) ? this.memHandler.TrySetFloatAt((int*)(_address + offset), ref *value) : false;

        // Double
        public bool TryDerefDouble(int offset, ref double result)
            => !IsNullPointer && this.memHandler.TryGetDoubleAt(IntPtr.Add(Address, offset), ref result);
        public bool TryDerefDouble(int offset, double* result)
            => (!IsNullPointer && result != (double*)0) ? this.memHandler.TryGetDoubleAt((int*)(_address + offset), result) : false;

        public bool TryWriteDouble(int offset, ref double value)
            => !IsNullPointer && this.memHandler.TrySetDoubleAt(IntPtr.Add(Address, offset), ref value);
        public bool TryWriteDouble(int offset, double* value)
            => (!IsNullPointer && value != (double*)0) ? this.memHandler.TrySetDoubleAt((int*)(_address + offset), ref *value) : false;

        // public bool TryDerefUnicodeString(int offset, int size, ref string result)
        //     => !IsNullPointer && this.memHandler.TryGetUnicodeStringAt(IntPtr.Add(Address, offset), size, ref result);
        // public bool TryDerefUnicodeString(int offset, int size, string* result)
        //     => (!IsNullPointer && result != (string*)0) ? this.memHandler.TryGetUnicodeStringAt((int*)(_address + offset), size, result) : false;

        // public bool TryDerefASCIIString(int offset, int size, ref string result)
        //     => !IsNullPointer && this.memHandler.TryGetASCIIStringAt(IntPtr.Add(Address, offset), size, ref result);
        // public bool TryDerefASCIIString(int offset, int size, string* result)
        //     => (!IsNullPointer && result != (string*)0) ? this.memHandler.TryGetASCIIStringAt((int*)(_address + offset), size, result) : false;
    }
}