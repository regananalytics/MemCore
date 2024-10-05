using System.Diagnostics;
using Newtonsoft.Json;

namespace MemCore
{
    public class MemoryCore
    {
        public string GameConfFile { get; set; }
        public Config Config { get; set; }
        public Process? Process { get; set; }
        public Dictionary<string, Pointer> BasePointers { get; set; } = new Dictionary<string, Pointer>();
        public Dictionary<string, Pointer> StatePointers { get; set; } = new Dictionary<string, Pointer>();
        public Dictionary<string, List<Pointer>> ReplicaPointers { get; set; } = new Dictionary<string, List<Pointer>>();
        public Dictionary<string, ProcessAddress> ProcessAddresses { get; set; } = new Dictionary<string, ProcessAddress>();
        public Dictionary<string, ProcessPointer> ProcessPointers { get; set; } = new Dictionary<string, ProcessPointer>();

        public MemoryCore(string gameConfDir, bool dryRun = false)
        {
            // Load Config
            if (gameConfDir.EndsWith(".yaml"))
                GameConfFile = gameConfDir;
            else
                GameConfFile = gameConfDir + "mem.yaml";
            string gameConf = File.ReadAllText(GameConfFile);

            // Parse Config
            var confParser = new ConfigParser();
            Config = confParser.Parse(gameConf);

            // Open Process
            var process = Process.GetProcessesByName(Config.GameExe)?.FirstOrDefault();
            if (!dryRun && process == null)
                throw new InvalidOperationException("Process not found");

            // Determine Game Version
            GameVersion gameVersion;
            if (Config.GameVersions.Count == 1)
                gameVersion = Config.GameVersions.First().Value;
            else
                throw new System.Exception("Multiple Game Versions not supported yet");

            // Build BasePointers
            foreach (var _sp in gameVersion.Pointers)
            {
                var sp = _sp.Value;
                BasePointers.Add(sp.Name, BuildPointer(sp));
            }

            // Build Addresses
            foreach (var _ad in Config.OpAddresses)
            {
                var ad = _ad.Value;
                var address = new Address(ad.Name, ad.Description, ad.Offset, ad.Size);
                ProcessAddresses.Add(ad.Name, address.AttachProcess(process));
            }


            // Build StatePointers
            foreach (var _sp in Config.StatePointers)
            {
                var sp = _sp.Value;

                // Determine Base Pointer
                if (sp.Address == null)
                    throw new System.Exception("State Pointer Address cannot be null");

                if (!sp.Address.StartsWith("0x"))
                {
                    // Relative Address
                    var bp = BasePointers[sp.Address];
                    if (sp.Levels != null)
                        if (bp.Levels != null)
                            throw new System.Exception(
                                "Relative Address with BasePointer and StatePointer Levels not supported yet"
                              );
                    else if (bp.Levels != null)
                        sp.Levels = bp.Levels; // Use BP Levels
                    sp.Address = "0x" + (bp.BaseAddress + bp.Offset).ToString("X");
                }

                // Determine Struct type
                if (sp.Type != null && !Pointer.TypeDictionary.ContainsKey(sp.Type.ToLower()))
                {
                    if (!Config.StateStructs.ContainsKey(sp.Type))
                        throw new System.Exception("StatePointer Type" + sp.Type + "not found");

                    // Build the StateStruct Pointers
                    var ss = Config.StateStructs[sp.Type];
                    foreach (var sf in ss.Fields)
                    {
                        // Build Field Pointer
                        sf.Name = sp.Name + "." + sf.Name;
                        sf.Levels = sp.Levels;
                        sf.Address = sp.Address;
                        sf.Offset = sp.Offset + sf.Offset;
                        
                        StatePointers.Add(sf.Name, BuildPointer(sf));
                    }

                }
                else
                {
                    StatePointers.Add(sp.Name, BuildPointer(sp));
                }
            }

            // Build Replica Pointers
            foreach (var _rp in Config.ReplicaPointers)
            {
                var rp = _rp.Value;

                // Determine Base Pointer
                if (rp.Address == null)
                    throw new System.Exception("State Pointer Address cannot be null");

                if (!rp.Address.StartsWith("0x"))
                {
                    // Relative Address
                    var bp = BasePointers[rp.Address];
                    if (bp.Levels != null)
                        if (rp.Levels != null)
                            throw new System.Exception(
                                "Relative Address with BasePointer and StatePointer Levels not supported yet"
                              );
                    rp.Levels = bp.Levels;
                    rp.Address = "0x" + (bp.BaseAddress + bp.Offset).ToString("X");
                }

                // Determine Struct type
                if (rp.Type != null && !Pointer.TypeDictionary.ContainsKey(rp.Type.ToLower()))
                {
                    if (!Config.StateStructs.ContainsKey(rp.Type))
                        throw new System.Exception("StatePointer Type" + rp.Type + "not found");

                    // Build each replica
                    for (int i = 0; i < rp.Replicas; i++)
                    {
                        int pad = i * (rp.Padding ?? 0);

                        // Build the StateStruct Pointers
                        var ss = Config.StateStructs[rp.Type];
                        foreach (var _sf in ss.Fields)
                        {
                            // Build Replica Field Pointer
                            var rpi = new StatePointer
                            {
                                Name = rp.Name + "[" + i + "]" + "." + _sf.Name,
                                Address = rp.Address,
                                Levels = rp.Levels,
                                Offset = rp.Offset + _sf.Offset + pad,
                                Type = _sf.Type,
                                Default = _sf.Default
                            };
                            StatePointers.Add(rpi.Name, BuildPointer(rpi));
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < rp.Replicas; i++)
                    {
                        int pad = i * (rp.Padding ?? 0);
                        var rpi = new StatePointer
                        {
                            Name = rp.Name + "[" + i + "]",
                            Address = rp.Address,
                            Levels = rp.Levels,
                            Offset = rp.Offset + pad,
                            Type = rp.Type,
                            Default = rp.Default
                        };
                        StatePointers.Add(rp.Name, BuildPointer(rpi));
                    }
                }
            }

            // Build Core
            // Attach Process to Pointers
            foreach (var _sp in StatePointers)
            {
                var sp = _sp.Value;
                ProcessPointer? proc;
                if (!dryRun && process != null)
                    proc = sp.AttachProcess(process);
                else
                    proc = null;
                ProcessPointers.Add(sp.Name, proc!);
            }
        }

        public Dictionary<string, object?> GetState()
        {
            var dict = new Dictionary<string, object?>();
            foreach (var pp in ProcessPointers)
            {
                pp.Value.Update();
                dict.Add(pp.Key, pp.Value.Deref());
            }
            return dict;
        }

        internal Address BuildAddress(OpAddress _a)
        {
            return new Address(
                _a.Name, _a.Description, _a.Offset, _a.Size
            );
        }

        internal Pointer BuildPointer(StatePointer _p)
        {
            return new Pointer(
                _p.Name, _p.Description, _p.Address, _p.Levels, _p.Offset, _p.Type, _p.Default
            );
        }

        public void OutputConfig()
        {
            Console.WriteLine(JsonConvert.SerializeObject(Config, Formatting.Indented));
        }

        public string OutputState()
        {
            return JsonConvert.SerializeObject(GetState(), Formatting.Indented);
        }
    }
}
