namespace MemCore
{
    public class GameVersion
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public byte[] Hash { get; set; }
        public Dictionary<string, int> Addresses { get; set; }

        public GameVersion (string name, string description, byte[] hash, Dictionary<string, int> addresses)
        {
            Name = name;
            Description = description;
            Hash = hash;
            Addresses = addresses;
        }
    }
}