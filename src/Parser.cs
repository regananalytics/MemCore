using System.Diagnostics;
using YamlDotNet.Serialization;

namespace MemCore
{
    public class ConfigParser
    {
        public Config Parse(string configFile)
        {
            var deserializer = new DeserializerBuilder().Build();
            var config = deserializer.Deserialize<Config>(new StringReader(configFile));
            return config;
        }
    }

    public class Address
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public int Offset { get; set; } = 0x0;
        public int Size { get; set; } = 0x3;

        public Address(string name, string? description = null, int offset = 0x0, int size = 0x3)
        {
            Name = name;
            Description = description;
            Offset = offset;
            Size = size;
        }

        public unsafe ProcessAddress AttachProcess(Process process)
        {
            return new ProcessAddress(this, process);
        }
    }

    public class ProcessAddress: Address
    {
        public Process Process { get; set; }
        public MemHandler MemHandler { get; set; }
        public IntPtr Address {get => (IntPtr)_address;}

        private int _address;

        public ProcessAddress(Address address, Process process) : base(address.Name, address.Description, address.Offset, address.Size)
        {
            Process = process;
            MemHandler = new MemHandler(Process.Id, false);

            var baseAddress = NativeWrappers.GetProcessBaseAddress(Process.Id, PInvoke.ListModules.LIST_MODULES_64BIT);
            if (baseAddress == 0)
                baseAddress = Process.MainModule.BaseAddress;

            _address = (int)IntPtr.Add(baseAddress, Offset);
        }

        public IDisposable MemoryProtection()
        {
            return new MemoryProtectionScope(MemHandler.ProcessHandle, Address, Size);
        }

        public IDisposable TemporaryNop()
        {
            return new TemporaryNopScope(MemHandler.ProcessHandle, Address, Size);
        }
    }

    public class Pointer
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public int BaseAddress { get; set; } = 0x0;
        public int[]? Levels { get; set; }
        public int Offset { get; set; } = 0x0;
        public Type? Type { get; set; }
        public object? Default { get; set; }

        public Pointer(string name, string? description = null, int? baseAddress = 0x0, int[]? levels = null, int? offset = 0x0, Type? type = null, object? defaultValue = null)
        {
            Name = name;
            Description = description;
            if (baseAddress != null)
                BaseAddress = (int)baseAddress;
            Levels = levels;
            if (offset != null)
                Offset = (int)offset;
            Type = type;
            Default = defaultValue;
        }

        public Pointer(string? name, string? description = null, string? baseAddress = "0x0", int[]? levels = null, int? offset = 0x0, string? type = null, object? defaultValue = null)
        {
            if (name == null)
                throw new ArgumentException("Name cannot be null");
            else if (name == "")
                throw new ArgumentException("Name cannot be empty");
            Name = name;
            Description = description;
            BaseAddress = Convert.ToInt32(baseAddress, 16);
            Levels = levels;
            if (offset != null)
                Offset = (int)offset;
            if (type != null)
                Type = (type != null) ? Type.GetType(TypeDictionary[type.ToLower()]) : null;
            else
                Type = null;
        }

        public unsafe ProcessPointer AttachProcess(Process process)
        {
            return new ProcessPointer(this, process);
        }

        public static Dictionary<string, string> TypeDictionary = new Dictionary<string, string>
        {
            { "byte", "System.Byte" },
            { "short", "System.Int16" },
            { "int", "System.Int32" },
            { "long", "System.Int64" },
            { "float", "System.Single" },
            { "double", "System.Double" },
            { "decimal", "System.Decimal" }
        };
    }

    public class ProcessPointer : Pointer
    {
        public Process Process { get; set; }
        public MemHandler MemHandler { get; set; }
        public MemPointer MemPointer { get; set; }

        public ProcessPointer(Pointer pointer, Process process) : base(pointer.Name, pointer.Description, pointer.BaseAddress, pointer.Levels, pointer.Offset, pointer.Type, pointer.Default)
        {
            Process = process;
            MemHandler = new MemHandler(Process.Id, false);

            var baseAddress = NativeWrappers.GetProcessBaseAddress(Process.Id, PInvoke.ListModules.LIST_MODULES_64BIT);
            if (baseAddress == 0)
                baseAddress = Process.MainModule.BaseAddress;

            if (Levels != null)
            {
                // long[] levelOffsets = Levels.Select(x => (long)x).ToArray();
                int[] levelOffsets = Levels.Select(x => x).ToArray();
                MemPointer = new MemPointer(MemHandler, IntPtr.Add(baseAddress, BaseAddress), levelOffsets);
            }
            else
            {
                MemPointer = new MemPointer(MemHandler, IntPtr.Add(baseAddress, BaseAddress));
            }
        }

        public void Update()
        {
            MemPointer?.UpdatePointers();
        }

        public object? Deref(int? offset = null)
        {
            int offsetNotNull = offset ?? Offset;

            // Return default if the pointer is null
            if (MemPointer == null || MemPointer.IsNullPointer)
                return Default;

            // Ensure Type is set, use Default's type if available
            Type ??= Default?.GetType() ?? throw new ArgumentException($"Type is null and no default value is set for pointer '{Name}'");

            // Map the types to deref functions
            switch (Type)
            {
                case Type t when t == typeof(sbyte):
                {
                    sbyte value = 0;
                    if (MemPointer.TryDerefSByte(offsetNotNull, ref value))
                        return value;
                    return Default;
                }
                case Type t when t == typeof(byte):
                {
                    byte value = 0;
                    if (MemPointer.TryDerefByte(offsetNotNull, ref value))
                        return value;
                    return Default;
                }
                case Type t when t == typeof(short):
                {
                    short value = 0;
                    if (MemPointer.TryDerefShort(offsetNotNull, ref value))
                        return value;
                    return Default;
                }
                case Type t when t == typeof(ushort):
                {
                    ushort value = 0;
                    if (MemPointer.TryDerefUShort(offsetNotNull, ref value))
                        return value;
                    return Default;
                }
                case Type t when t == typeof(int):
                {
                    int value = 0;
                    if (MemPointer.TryDerefInt(offsetNotNull, ref value))
                        return value;
                    return Default;
                }
                case Type t when t == typeof(uint):
                {
                    uint value = 0;
                    if (MemPointer.TryDerefUInt(offsetNotNull, ref value))
                        return value;
                    return Default;
                }
                case Type t when t == typeof(long):
                {
                    long value = 0;
                    if (MemPointer.TryDerefLong(offsetNotNull, ref value))
                        return value;
                    return Default;
                }
                case Type t when t == typeof(ulong):
                {
                    ulong value = 0;
                    if (MemPointer.TryDerefULong(offsetNotNull, ref value))
                        return value;
                    return Default;
                }
                case Type t when t == typeof(float):
                {
                    float value = 0;
                    if (MemPointer.TryDerefFloat(offsetNotNull, ref value))
                        return value;
                    return Default;
                }
                case Type t when t == typeof(double):
                {
                    double value = 0;
                    if (MemPointer.TryDerefDouble(offsetNotNull, ref value))
                        return value;
                    return Default;
                }
                default:
                    throw new ArgumentException($"Invalid type '{Type.Name}'");
            }
        }

        public bool SetValue(float value)
        {
            // Return default if the pointer is null
            if (MemPointer == null || MemPointer.IsNullPointer)
                return false;

            // Ensure Type is set, use Default's type if available
            Type ??= Default?.GetType() ?? throw new ArgumentException($"Type is null and no default value is set for pointer '{Name}'");

            // var _value = Convert.ChangeType(value, Type);
            return MemPointer.TryWriteFloat(Offset, ref value);
        }
    }
}