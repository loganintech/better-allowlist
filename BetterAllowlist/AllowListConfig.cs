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

    public int RemoveAllMatches(AllowListEntry entry)
    {
        var removed = allowList.RemoveAll(val => val.Matches(entry));
        if (removed == 0)
        {
            return 0;
        }

        return removed;
    }

    public string StringifyEntries()
    {
        return String.Join("\n", this.allowList.Select(entry => entry.ToString()));
    }
}