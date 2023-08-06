using ProcessMemory;
using System.Diagnostics;
using YamlDotNet.Serialization;

namespace MemCore {
  public class ConfigParser {
    public Config Parse(string configFile) {
      var deserializer = new DeserializerBuilder().Build();
      var config = deserializer.Deserialize<Config>(new StringReader(configFile));
      return config;
    }

    // public void Build(Config config) {
    //     raise new NotImplementedException();
    // }
  }

  public class Pointer {
    public string Name { get; set; }
    public string? Description { get; set; }
    public int BaseAddress { get; set; } = 0x0;
    public int[]? Levels { get; set; }
    public int Offset { get; set; } = 0x0;
    public Type? Type { get; set; }
    public object? Default { get; set; }

    public Pointer (string name, string? description = null, int? baseAddress = 0x0, int[]? levels = null, int? offset = 0x0, Type? type = null, object? defaultValue = null) {
      Name = name;
      Description = description;
      if (baseAddress != null)
        BaseAddress = (int) baseAddress;
      Levels = levels;
      if (offset != null)
        Offset = (int) offset;
      Type = type;
      Default = defaultValue;
    }

    public Pointer (string? name, string? description = null, string? baseAddress = "0x0", int[]? levels = null, int? offset = 0x0, string? type = null, object? defaultValue = null) {
      if (name == null)
        throw new ArgumentException("Name cannot be null");
      else if (name == "")
        throw new ArgumentException("Name cannot be empty");
      Name = name;
      Description = description;
      BaseAddress = Convert.ToInt32(baseAddress, 16);
      Levels = levels;
      if (offset != null)
        Offset = (int) offset;
      if (type != null)
        Type = (type != null) ? Type.GetType(TypeDictionary[type.ToLower()]) : null;
      else
        Type = null;
    }

    public unsafe ProcessPointer AttachProcess(Process process) {
      return new ProcessPointer(this, process);
    }

    public static Dictionary<string, string> TypeDictionary = new Dictionary<string, string>
    {
      { "byte", "System.Byte" },
      { "int", "System.Int32" },
      { "long", "System.Int64" },
      { "float", "System.Single" },
      { "double", "System.Double" },
      { "decimal", "System.Decimal" }
    };
  }

  public class ProcessPointer : Pointer {
    public Process Process { get; set; }  
    public ProcessMemoryHandler MemoryHandler { get; set; }
    public MultilevelPointer MLPointer { get; set; }

    public ProcessPointer (Pointer pointer, Process process) : base(pointer.Name, pointer.Description, pointer.BaseAddress, pointer.Levels, pointer.Offset, pointer.Type, pointer.Default) {
      Process = process;
      MemoryHandler = new ProcessMemoryHandler(Process.Id);

      var baseAddress = NativeWrappers.GetProcessBaseAddress(Process.Id, PInvoke.ListModules.LIST_MODULES_64BIT);

      if (Levels != null) {
        var levelOffsets = Levels.Select(x => (long)x).ToArray();
        MLPointer = new MultilevelPointer(MemoryHandler, IntPtr.Add(baseAddress, BaseAddress), levelOffsets);
      } else {
        MLPointer = new MultilevelPointer(MemoryHandler, IntPtr.Add(baseAddress, BaseAddress));
      }
    }

    public void Update() {
      MLPointer?.UpdatePointers();
    }

    public object? Deref(int? offset = null) {
      int offset_not_null;
      if (offset != null)
        offset_not_null = (int) offset;
      else
        offset_not_null = Offset;

      // Return default if the pointer is null
      if (MLPointer == null || MLPointer.IsNullPointer)
        return Default;

      // Dereference the pointer
      if (Type == null) // We don't have a type...
        if (Default != null) // If we have a default we can use that type
          Type = Default.GetType();
        else
          throw new ArgumentException($"Type is null and no default value is set for pointer '{Name}'");
      else if (Type == typeof(Byte))
        return MLPointer.DerefByte(offset_not_null);
      else if (Type == typeof(int))
        return MLPointer.DerefInt(offset_not_null);
      else if (Type == typeof(long))
        return MLPointer.DerefLong(offset_not_null);
      else if (Type == typeof(float))
        return MLPointer.DerefFloat(offset_not_null);
      else if (Type == typeof(double))
        return MLPointer.DerefDouble(offset_not_null);
      else if (Type == typeof(string))
        return MLPointer.DerefUnicodeString(offset_not_null, 100);
      throw new ArgumentException($"Invalid type '{Type.Name}'");
    }
  }
}