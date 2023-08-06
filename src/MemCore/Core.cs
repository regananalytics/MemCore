using System.Diagnostics;
using ProcessMemory;

namespace MemCore
{
  public class MemoryCore
  {
    public string GameConfFile { get; set; }
    public Config Config { get; set; }
    public Process? Process { get; set; }
    public Dictionary<string, Pointer> BasePointers { get; set; } = new Dictionary<string, Pointer>();
    public Dictionary<string, Pointer> StatePointers { get; set; } = new Dictionary<string, Pointer>();
    public Dictionary<string, ProcessPointer> ProcessPointers { get; set; } = new Dictionary<string, ProcessPointer>();

    public MemoryCore(string gameName)
    {
      // Load Config
      GameConfFile = @".\config\" + gameName + ".yaml";
      string gameConf = File.ReadAllText(GameConfFile);

      // Parse Config
      var confParser = new ConfigParser();
      Config = confParser.Parse(gameConf);

      // Open Process
      var process = Process.GetProcessesByName(Config.GameExe)?.FirstOrDefault();
      if (process == null)
        throw new System.Exception("Process not found");

      // Determine Game Version
      GameVersion gameVersion;
      if (Config.GameVersions.Count == 1)
        gameVersion = Config.GameVersions.First().Value;
      else
        throw new System.Exception("Multiple Game Versions not supported yet");

      // Build BasePointers
      foreach (var _sp in gameVersion.Pointers) {
        var sp = _sp.Value;
        BasePointers.Add(sp.Name, BuildPointer(sp));
      }

      // Build State Pointers
      foreach (var _sp in Config.StatePointers) {
        var sp = _sp.Value;

        // Determine Base Pointer
        if (sp.Address == null)
            throw new System.Exception("State Pointer Address cannot be null");

        if (!sp.Address.StartsWith("0x")) {
            // Relative Address
            var bp = BasePointers[sp.Address];
            if (bp.Levels != null) 
                if (sp.Levels != null)
                    throw new System.Exception(
                        "Relative Address with BasePointer and StatePointer Levels not supported yet"
                      );
                sp.Levels = bp.Levels;
            sp.Address = "0x" + (bp.BaseAddress + bp.Offset).ToString("X");
        }

        // Determine Struct type
        if (sp.Type != null && !Pointer.TypeDictionary.ContainsKey(sp.Type.ToLower()))
          if (!Config.StateStructs.ContainsKey(sp.Type))
            throw new System.Exception("StatePointer Type" + sp.Type + "not found");
          else
            sp.Type = Config.StateStructs[sp.Type].Name;
        StatePointers.Add(sp.Name, BuildPointer(sp));
      }

      // Build Structs
      // Build Core
      // Attach Process to Pointers
      foreach (var _sp in StatePointers) {
        var sp = _sp.Value;
        ProcessPointers.Add(sp.Name, sp.AttachProcess(process));
      }
    }

    public Dictionary<string, object?> GetState() {
      var dict = new Dictionary<string, object?>();
      foreach (var pp in ProcessPointers) {
        pp.Value.Update();
        dict.Add(pp.Key, pp.Value.Deref());
      }
      return dict;
    }

    internal Pointer BuildPointer(StatePointer _p) {
      return new Pointer(
          _p.Name, _p.Description, _p.Address, _p.Levels, _p.Offset, _p.Type, _p.Default
      );
    }
  }
}