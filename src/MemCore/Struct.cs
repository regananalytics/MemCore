using System.Diagnostics;

namespace MemCore
{
    public class StateStructConfig : StateConfig
    {
        public int? Pack { get; set; }
        public int? Size { get; set; }
        public Dictionary<string, FieldConfig> FieldConfigs { get; set; } = new Dictionary<string, FieldConfig>();

        public StateStructConfig (string name, int baseOffset=0x0, int[]? levels=null, int valueOffset=0x0, int? pack=null, int? size=null)
        : base(name, null, baseOffset, levels, valueOffset)
        {
            Name = name;
            BaseOffset = baseOffset;
            Levels = levels;
            ValueOffset = valueOffset;
            Pack = pack;
            Size = size;
        }
        
        public void AddFieldConfig(string name, FieldConfig value) => FieldConfigs.Add(name, value);
    }
    
    public class FieldConfig : StateConfig
    {
        public FieldConfig (string name, string typeStr, int valueOffset=0x0, object? defaultValue=null)
        : base(name, typeStr, 0x0, null, valueOffset, defaultValue) {}
    }

    public class StateStruct : StateStructConfig
    {
        private Process Process { get; set; }
        private MemPointer Pointer { get; set; }
        private Dictionary<string, MemPointer> Fields { get; set; } = new Dictionary<string, MemPointer>();

        public StateStruct (StateStructConfig config, Process process) 
        : base(config.Name, config.BaseOffset, config.Levels, config.ValueOffset, config.Pack, config.Size)
        {
            Process = process;
            Pointer = new MemPointer(Name, null, BaseOffset, Levels);
            Pointer.AttachProcess(Process);
            FieldConfigs = config.FieldConfigs;
            foreach (var fieldConfig in config.FieldConfigs)
            {
                var type = (fieldConfig.Value.TypeStr != null) ? Type.GetType(fieldConfig.Value.TypeStr) : null;
                if (type == null)
                    throw new System.Exception("Invalid type string");
                var fieldPointer = new MemPointer(
                    fieldConfig.Value.Name, 
                    type, 
                    config.BaseOffset, 
                    Levels,
                    fieldConfig.Value.ValueOffset, 
                    fieldConfig.Value.Default
                );
                fieldPointer.AttachProcess(Process);
                Fields.Add(fieldConfig.Key, fieldPointer);
            }
        }

        public void Update() => Pointer.Update();

        public Dictionary<string, object?> Deref()
        {
            var values = new Dictionary<string, object?>();
            foreach (var field in Fields)
                values.Add(field.Key, field.Value.Deref(ValueOffset + field.Value.ValueOffset));
            return values;
        }
    }
}