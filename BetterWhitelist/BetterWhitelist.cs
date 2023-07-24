using System.Text.Json;
using System.Text.Json.Serialization;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BetterWhitelist;

[ApiVersion(2, 1)]
public class BetterWhitelist : TerrariaPlugin
{
    private static readonly IEnumerable<string> AllowedFilterTypes = new HashSet<string> { "name", "ip", "uuid" };
    private static readonly IEnumerable<string> AllowedActions = new HashSet<string> { "add", "remove" };

    private static string TShockConfigPath
    {
        get { return Path.Combine(TShock.SavePath, "better-whitelist.json"); }
    }

    public override string Author => "loganintech";
    public override string Description => "Implement a whitelist based on UUID, Character Name, or IP";
    public override string Name => "Better Whitelist";
    public override Version Version => new Version(0, 0, 1, 1);
    private readonly string _properSyntax = "Proper syntax:\n/allowlist [add/remove] [ip/name/uuid] <value>\n/allowlist reload";

    private AllowListConfig _config;

    public BetterWhitelist(Main game) : base(game)
    {
        _config = new AllowListConfig();
    }

    private void Log(string message)
    {
        Console.WriteLine($"{Name}: {message}");
    }

    public override void Initialize()
    {
        ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
        Commands.ChatCommands.Add(new Command(Permissions.whitelist, CommandHandler, "allowlist"));

        // Load file from Config path. If it doesn't exist, create it. 
        if (!LoadConfig())
        {
            
            WriteConfig();
        }
    }

    public void CommandHandler(CommandArgs args)
    {
        TSPlayer player = args.Player;

        
        if (args.Parameters.Count == 1)
        {
            switch (args.Parameters[0])
            {
                case "reload":
                    LoadConfig();
                    break;
                case "list":
                    List(player);
                    break;
            }

            return;
        }

        if (args.Parameters.Count != 3)
        {
            player.SendErrorMessage($"Invalid syntax! {_properSyntax}");
            return;
        }

        string action = args.Parameters[0];
        string type = args.Parameters[1];
        string value = args.Parameters[2];

        if (!AllowedActions.Contains(action))
        {
            player.SendErrorMessage($"Invalid syntax: {action} is not a valid action! {_properSyntax}");
            return;
        }

        if (!AllowedFilterTypes.Contains(type))
        {
            player.SendErrorMessage($"Invalid syntax: {type} is not a valid type! {_properSyntax}");
            return;
        }

        ManageEntry(player, action, type, value);
    }

    // Reload from disk
    // allowlist reload
    // LoadConfig returns false if the file does not exist, and true if it does exist, even if it fails to deserialize.
    public bool LoadConfig()
    {
        if (!File.Exists(TShockConfigPath))
        {
            _config = new AllowListConfig();
            return false;
        }

        string fileContent = File.ReadAllText(TShockConfigPath);
        AllowListConfig? configData;
        try
        {
            configData = JsonSerializer.Deserialize<AllowListConfig>(fileContent);
        } catch (Exception e)
        {
            Log($"Failed to deserialize config file: {e.Message}");
            _config = new AllowListConfig();
            return false;
        }
        if (configData == null)
        {
            _config = new AllowListConfig();
            return true;
        }

        _config = configData;
        return true;
    }

    public void List(TSPlayer player)
    {
        var toPrint = "";
        foreach (var entry in _config.allowList)
        {
            toPrint += $"{entry.type} {entry.value}\n";
        }
        player.SendSuccessMessage(toPrint);
    }

    public void WriteConfig()
    {
        var opts = new JsonSerializerOptions
        {
            WriteIndented = true,
        };
        var jsonConfig = JsonSerializer.Serialize(_config, opts);
        File.WriteAllText(TShockConfigPath, jsonConfig);
    }

    // Allowlist [add/remove] [ip/name/uuid] <value>
    public void ManageEntry(TSPlayer player, string action, string type, string value)
    {
        AllowListEntryType typ = AllowListEntryType.Name;
        switch (type)
        {
            case "ip":
                typ = AllowListEntryType.IP;
                break;
            case "name":
                typ = AllowListEntryType.Name;
                break;
            case "uuid":
                typ = AllowListEntryType.UUID;
                break;
        }

        AllowListEntry entry = new AllowListEntry(typ, value);

        if (action == "add")
        {
            _config.allowList.Add(entry);
            WriteConfig();
            player.SendSuccessMessage($"Added entry to allowlist.");
            return;
        }

        if (action == "remove")
        {
            var removed = _config.allowList.RemoveAll(val => val.Matches(entry));
            if (removed == 0)
            {
                player.SendErrorMessage("Could not remove that value, no matches were found.");
                return;
            }

            WriteConfig();
            player.SendSuccessMessage($"Removed {removed} matching entries from the allowlist.");
            return;
        }

        player.SendErrorMessage("Invalid action.");
    }


    private void OnJoin(JoinEventArgs args)
    {
        TSPlayer player = TShock.Players[args.Who];
        if (player == null)
        {
            //?? 
            Log($"On join event happened but the player is null: {args.Who}");
            return;
        }

        var bypassPermissions = new string[]
        {
            Permissions.whitelist,
            Permissions.ban,
            Permissions.kick,
            Permissions.immunetokick,
        };
        
        // If the player has a bypass permission, just allow them
        if (bypassPermissions.Any(perm => player.HasPermission(perm)))
        {
            return;
        }

        var allowed = _config.allowList.Any(entry => entry.Matches(player));
        if (_config.enabled && !allowed)
        {
            player.Disconnect(_config.removalReason);
        }
    }
}

class AllowListConfig
{
    [JsonPropertyName("enabled")]
    public bool enabled { get; set; }
    [JsonPropertyName("allowList")]

    public List<AllowListEntry> allowList { get; set; } = new();
    [JsonPropertyName("removalReason")]
    public string removalReason { get; set; } = "You are not on the allowlist.";
}

enum AllowListEntryType
{
    IP = "ip",
    Name = "name",
    UUID = "uuid"
}

class AllowListEntry
{
    [JsonPropertyName("type")]
    public AllowListEntryType type { get; }
    [JsonPropertyName("value")]
    public string value { get; }

    public AllowListEntry(AllowListEntryType type, string value)
    {
        this.type = type;
        this.value = value;
    }

    public bool Matches(TSPlayer player)
    {
        switch (type)
        {
            case AllowListEntryType.IP:
                return player.IP == value;
            case AllowListEntryType.Name:
                return player.Name == value;
            case AllowListEntryType.UUID:
                return player.UUID == value;
        }

        return false;
    }

    public bool Matches(AllowListEntry entry)
    {
        return this.type == entry.type && this.value == entry.value;
    }
}