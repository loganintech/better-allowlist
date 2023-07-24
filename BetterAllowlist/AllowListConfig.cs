using Newtonsoft.Json;

namespace BetterAllowlist;

class AllowListConfig
{
    [JsonProperty("enabled")] public bool enabled { get; set; }
    [JsonProperty("allowList")] public List<AllowListEntry> allowList { get; set; } = new();
    [JsonProperty("removalReason")] public string removalReason { get; set; } = "You are not on the allowlist.";

    public AllowListConfig()
    {
    }
    
    public void Add(AllowListEntry entry)
    {
        allowList.Add(entry);
    }

    public int RemoveAllMatches(string name)
    {
        return allowList.RemoveAll(val => val.name == name);
    }

    public string StringifyEntries()
    {
        return String.Join("\n", this.allowList.Select(entry => entry.ToString()));
    }
}