using ProcessMemory;
using System.Diagnostics;

namespace MemCore
{
    public class MemPointer
    {
        public string Name { get; set; }
        public Type? Type { get; set; }
        public int BaseOffset { get; set; } = 0x0;
        public int[]? LevelOffsets { get; set; }
        public int ValueOffset { get; set; } = 0x0;
        public object? Default { get; set; }

        private Process? Process { get; set; }
        private ProcessMemoryHandler? MemoryHandler { get; set; }
        private MultilevelPointer? Pointer { get; set; }

        public MemPointer (string name, Type? type = null, int baseOffset = 0x0, int[]? levelOffsets = null, int valueOffset = 0x0, object? defaultValue=null)
        {
            Name = name;
            Type = type;
            BaseOffset = baseOffset;
            LevelOffsets = levelOffsets;
            ValueOffset = valueOffset;
            Default = defaultValue;
        }

        public unsafe void AttachProcess(Process process)
        {
            Process = process;
            MemoryHandler = new ProcessMemoryHandler(Process.Id);

            var baseAddress = NativeWrappers.GetProcessBaseAddress(Process.Id, PInvoke.ListModules.LIST_MODULES_64BIT);

            if (LevelOffsets != null)
            {
                var levelOffsets = LevelOffsets.Select(x => (long)x).ToArray();
                Pointer = new MultilevelPointer(MemoryHandler, IntPtr.Add(baseAddress, BaseOffset), levelOffsets);
            }
            else
            {
                Pointer = new MultilevelPointer(MemoryHandler, IntPtr.Add(baseAddress, BaseOffset));
            }
        }

        public void Update()
        {
            Pointer?.UpdatePointers();
        }
        
        public object? Deref(int? valueOffset)
        {
            int offset;
            if (valueOffset != null)
                offset = (int) valueOffset;
            else
                offset = ValueOffset;

            if (Type == null)
                throw new ArgumentException("Type is null");

            if (Pointer == null || Pointer.IsNullPointer)
                return Default;
            if (Type == typeof(Byte))
                return Pointer.DerefByte(offset);
            else if (Type == typeof(int))
                return Pointer.DerefInt(offset);
            else if (Type == typeof(long))
                return Pointer.DerefLong(offset);
            else if (Type == typeof(float))
                return Pointer.DerefFloat(offset);
            else if (Type == typeof(double))
                return Pointer.DerefDouble(offset);
            else if (Type == typeof(string))
                return Pointer.DerefUnicodeString(offset, 100);
            throw new ArgumentException($"Invalid type '{Type.Name}'");
        }

    }

}
