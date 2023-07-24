using Newtonsoft.Json;
using TShockAPI;

namespace BetterAllowlist;

class AllowListEntry
{
    [JsonProperty("ip")] public string? ip { get; set; }
    [JsonProperty("name")] public string? name { get; set; }
    [JsonProperty("uuid")] public string? uuid { get; set; }

    public AllowListEntry()
    {
    }
    
    public AllowListEntry(string? ip, string? name, string? uuid)
    {
        this.ip = ip;
        this.name = name;
        this.uuid = uuid;
    }

    public AllowListEntry(string type, string value)
    {
        string? ip = null;
        string? name = null;
        string? uuid = null;
        switch (type)
        {
            case "ip":
                foreach (var player in TShock.Players)
                {
                    if (player.Name == value)
                    {
                        ip = player.IP;
                        break;
                    }
                }
                break;
            case "name":
                name = value;
                break;
            case "uuid":
                foreach (var player in TShock.Players)
                {
                    if (player.Name == value)
                    {
                        uuid = player.UUID;
                        break;
                    }
                }
                break;
            case "all":
                foreach (var player in TShock.Players)
                {
                    if (player.Name == value)
                    {
                        name = player.Name;
                        ip = player.IP;
                        uuid = player.UUID;
                        break;
                    }
                }

                break;
        }
        
        this.ip = ip;
        this.name = name;
        this.uuid = uuid;
    }

    public bool Matches(TSPlayer player)
    {
        var matches = true;
        matches = matches && (this.ip == null || this.ip == player.IP);
        matches = matches && (this.name == null || this.name == player.Name);
        matches = matches && (this.uuid == null || this.uuid == player.UUID);
        return matches;
    }

    public bool Matches(AllowListEntry entry)
    {
        var matches = true;
        matches = matches && (this.ip == null || this.ip == entry.ip);
        matches = matches && (this.name == null || this.name == entry.name);
        matches = matches && (this.uuid == null || this.uuid == entry.uuid);
        return matches;
    }
    
    public override string ToString()
    {
        return $"AllowListEntry(ip={ip}, name={name}, uuid={uuid})";
    }
    
    
}