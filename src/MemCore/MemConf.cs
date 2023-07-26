using System.Diagnostics;
using YamlDotNet.Serialization;

namespace MemCore
{
    public class MemConfigParser
    {
        public Dictionary<string, MemPointer> Pointers { get; set; } = new Dictionary<string, MemPointer>();
        public string? ConfigString { get; set; }
        public string? GameVersion { get; set; }

        private Dictionary<string, GameVersion> GameVersions { get; set; } = new Dictionary<string, GameVersion>();
        private Dictionary<string, StateConfig> StateConfigs { get; set; } = new Dictionary<string, StateConfig>();
        private Dictionary<string, StateStructConfig> StructConfigs { get; set; } = new Dictionary<string, StateStructConfig>();

        public Dictionary<string, State> States { get; set; } = new Dictionary<string, State>();
        public Dictionary<string, StateStruct> Structs { get; set; } = new Dictionary<string, StateStruct>();

        public void Parse(string? confFile)
        {
            if (confFile != null)
                ConfigString = File.ReadAllText(confFile);
            else if (ConfigString == null)
                throw new Exception("No config string or file provided");

            var deserializer = new DeserializerBuilder().Build();
            var configDict = (Dictionary<object, object>?) deserializer.Deserialize(new StringReader(ConfigString));

            if (configDict == null)
                return;

            // Iterate over the deserialized object and dispatch to the appropriate parsers
            foreach (var item in configDict)
            {
                var key = item.Key.ToString();
                if (key == null)
                    continue;
                key = key.ToUpper();
                
                if (key == "GAME_VERSIONS")
                    GameVersions = ParseGameVersions(ToObjDict(item.Value));
                else if (key == "STATES")
                    StateConfigs = ParseStateConfigs(ToObjDict(item.Value));
                else if (key == "STRUCTS")
                    StructConfigs = ParseStructConfigs(ToObjDict(item.Value));
            }
        }

        private static Dictionary<string, GameVersion> ParseGameVersions(Dictionary<object, object> versions)
        {
            var gameVersions = new Dictionary<string, GameVersion>();
            foreach (var item in versions)
            {
                var name = (string)item.Key;
                var val = ToObjDict(item.Value);
                var desc = (string)val["Description"];
                var hash = ToObjList(val["Hash"]).Select(b => Convert.ToByte((string)b, 16)).ToArray(); // There must be a better way...
                var pointers = ToObjDict(val["Pointers"]);
                var addressesDict = new Dictionary<string, int>();
                foreach (var addr in pointers)
                {
                    var aKey = (string)addr.Key;
                    var aVal = Convert.ToInt32((string)addr.Value, 16);
                    addressesDict.Add(aKey, aVal);
                }
                gameVersions.Add(name, new GameVersion(name, desc, hash, addressesDict));
            }
            return gameVersions;
        } 

        private static Dictionary<string, StateConfig> ParseStateConfigs(Dictionary<object, object> states)
        {
            var States = new Dictionary<string, StateConfig>();
            foreach (var item in states)
            {
                var name  = (string)item.Key;
                var val = (Dictionary<object, object>)item.Value;
                var baseOffset = (string)val["BaseOffset"];
                var type = (string)val["Type"];
                var levels_ = ToObjList(val["Levels"]);
                var levels = (levels_ != null) ? levels_.Select(b => Convert.ToInt32((string)b, 16)).ToArray() : null;
                var valueOffset = Convert.ToInt32((string)val["ValueOffset"], 16);
                var default_ = val.ContainsKey("Default") ? val["Default"] : null;
                States.Add(name, new StateConfig(name, type, 0x0, levels, valueOffset, default_));
            }
            return States;
        }

        private static Dictionary<string, StateStructConfig> ParseStructConfigs(Dictionary<object, object> structs)
        {
            var structConfigs = new Dictionary<string, StateStructConfig>();
            foreach (var item in structs)
            {
                var name = (string)item.Key;
                var val = ToObjDict(item.Value);

                int? pack = null;
                if (val.ContainsKey("Pack"))
                    pack = (val["Pack"] != null) ? Convert.ToInt32((string)val["Pack"]) : null;

                int? size = null;
                if (val.ContainsKey("Size"))
                    pack = (val["Size"] != null) ? Convert.ToInt32((string)val["Size"], 16) : null;
                
                int[]? levels = null;
                if (val.ContainsKey("Levels"))
                    levels = (ToObjList(val["Levels"]) != null) ? ToObjList(val["Levels"]).Select(b => Convert.ToInt32((string)b, 16)).ToArray() : null;

                var structConfig = new StateStructConfig(name, 0x0, levels, 0x0, pack, size);

                var fieldConfigs = ToObjDict(val["Fields"]);
                foreach (var field in fieldConfigs)
                {
                    var fname = (string)field.Key;
                    var fval = ToObjDict(field.Value);
                    structConfig.AddFieldConfig(fname, 
                        new FieldConfig(
                            fname,
                            TypeDictionary[(string)fval["Type"]],
                            (fval["FieldOffset"] != null) ? Convert.ToInt32((string)fval["FieldOffset"], 16) : 0x0,
                            (fval["Default"] != null) ? fval["Default"] : null
                        )
                    );
                }
                structConfigs.Add(name, structConfig);
            }
            return structConfigs;
        }

        public void Build(string? gameVersion, Process process)
        {
            // Handle nulls
            if (ConfigString == null)
                throw new Exception("Config not parsed yet");
            if (gameVersion != null)
                GameVersion = gameVersion;
            else if (GameVersion == null)
                throw new Exception("No game version provided"); 

            // Get the game version
            var thisVersion = GameVersions[GameVersion];

            // Iterate over States to build pointers
            foreach (var state in StateConfigs)
            {
                // Determine if the type is a data type or a struct
                if (state.Value.TypeStr == null)
                    continue;
                if (StructConfigs.ContainsKey(state.Value.TypeStr))
                {
                    // Update the BaseOffset from the GameVersion
                    var structConfig = StructConfigs[state.Value.TypeStr];
                    structConfig.BaseOffset = thisVersion.Addresses[state.Key];
                    structConfig.Levels = state.Value.Levels;
                    structConfig.ValueOffset = state.Value.ValueOffset;
                    // Build the State from the StateConfig by attaching the process
                    Structs.Add(state.Key, new StateStruct(structConfig, process));
                }
                else
                {
                    // Update the BaseOffset from the GameVersion
                    state.Value.BaseOffset = thisVersion.Addresses[state.Key];
                    // Build the State from the StateConfig by attaching the process
                    States.Add(state.Key, new State(state.Value, process));
                }
            }
        }        

        private static Dictionary<object, object> ToObjDict(object obj) => (Dictionary<object, object>)obj;
        private static List<object> ToObjList(object obj) => (List<object>)obj;

        internal static Dictionary<string, string> TypeDictionary = new Dictionary<string, string>
        {
            { "byte", "System.Byte" },
            { "int", "System.Int32" },
            { "long", "System.Int64" },
            { "float", "System.Single" },
            { "double", "System.Double" },
            { "decimal", "System.Decimal" }
        };
    }
}
