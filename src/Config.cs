using System.Diagnostics;
using YamlDotNet.Serialization;

namespace MemCore
{
    public class Config
    {
        public string GameName { get; set; } = "";
        public string? GameID { get; set; }
        public string? GameExe { get; set; }
        public Dictionary<string, GameVersion> GameVersions { get; set; } = new Dictionary<string, GameVersion>();
        public Dictionary<string, OpAddress> OpAddresses { get; set; } = new Dictionary<string, OpAddress>();
        public Dictionary<string, StatePointer> StatePointers { get; set; } = new Dictionary<string, StatePointer>();
        public Dictionary<string, ReplicaPointer> ReplicaPointers { get; set; } = new Dictionary<string, ReplicaPointer>();
        public Dictionary<string, StateStruct> StateStructs { get; set; } = new Dictionary<string, StateStruct>();
    }

    public class GameVersion
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public List<byte>? Hash { get; set; }
        public Dictionary<string, StatePointer> Pointers { get; set; } = new Dictionary<string, StatePointer>();
    }

    public class OpAddress
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public int Offset { get; set; }
        public int Size { get; set;}
    }

    public class StatePointer
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? Address { get; set; }
        public int[]? Levels { get; set; }
        public int? Offset { get; set; }
        public string? Type { get; set; }
        public object? Default { get; set; }
    }

    public class ReplicaPointer : StatePointer
    {
        public int? Replicas { get; set; }
        public int? Padding { get; set; }
    }

    public class StateStruct
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public int? Address { get; set; }
        public List<StatePointer> Fields { get; set; } = new List<StatePointer>();
    }

}