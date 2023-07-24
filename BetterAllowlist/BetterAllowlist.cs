using Newtonsoft.Json;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace BetterAllowlist;

[ApiVersion(2, 1)]
public class BetterAllowlist : TerrariaPlugin
{
    private AllowListConfig _config;

    private static readonly IEnumerable<string>
        AllowedFilterTypes = new HashSet<string> { "name", "ip", "uuid", "all" };

    private static readonly IEnumerable<string> AllowedActions = new HashSet<string>
        { "add", "remove", "list", "enable", "disable", "reload" };

    public override string Author => "loganintech";
    public override string Description => "Implement a whitelist based on UUID, Character Name, or IP";
    public override string Name => "Better Allowlist";
    public override Version Version => new Version(0, 0, 1, 1);

    private readonly string _properSyntax =
        "Proper syntax:\n" +
        "/allowlist add [ip/name/uuid/all] <player-name> - Add or remove players type entry based on player name.\n" +
        "This means that /allowlist add ip Logan would add Logan's IP adddress.\n" +
        "For custom entry of these values, edit the config file directly.\n" +
        "/allowlist remove <player-name> - Remove player from allowlist.\n" +
        "/allowlist reload\n" +
        "/allowlist list\n" +
        "/allowlist enable\n" +
        "/allowlist disable";

    private static string TShockConfigPath
    {
        get { return Path.Combine(TShock.SavePath, "better-allowlist.json"); }
    }

    private static JsonSerializerSettings serializeOpts = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Include,
        Formatting = Formatting.Indented,
    };

    private readonly string[] bypassPermissions =
    {
        Permissions.whitelist,
        Permissions.ban,
        Permissions.kick,
        Permissions.immunetokick,
    };

    public BetterAllowlist(Main game) : base(game)
    {
        _config = new AllowListConfig();
    }

    private void Log(string message)
    {
        Console.WriteLine($"[{Name}]: {message}");
    }

    public override void Initialize()
    {
        ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
        Commands.ChatCommands.Add(new Command(Permissions.whitelist, CommandHandler, "allowlist"));
        if (!Load())
        {
            Write();
        }
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
        }

        Commands.ChatCommands.RemoveAll(cmd => cmd.HasAlias("allowlist"));
        base.Dispose(disposing);
    }

    public void CommandHandler(CommandArgs args)
    {
        TSPlayer player = args.Player;

        if (args.Parameters.Count == 0)
        {
            player.SendErrorMessage($"Invalid syntax, requires an action: {_properSyntax}");
            return;
        }

        string action = args.Parameters[0];
        if (!AllowedActions.Contains(action))
        {
            player.SendErrorMessage($"Invalid syntax: {action} is not a valid action! {_properSyntax}");
            return;
        }

        if (args.Parameters.Count == 1)
        {
            switch (args.Parameters[0])
            {
                case "reload":
                    if (!Load())
                    {
                        player.SendErrorMessage("Could not reload config. Check server log for details.");
                        return;
                    }

                    player.SendSuccessMessage("Reloaded config.");
                    break;
                case "list":
                    List(player);
                    break;
                case "enable":
                    _config.enabled = true;
                    Write();
                    player.SendSuccessMessage("Allowlist enabled.");
                    break;
                case "disable":
                    _config.enabled = false;
                    Write();
                    player.SendSuccessMessage("Allowlist disabled.");
                    break;
            }

            return;
        }

        ManageEntry(player, args.Parameters);
    }

    public void List(TSPlayer player)
    {
        if (_config.allowList.Count == 0)
        {
            player.SendInfoMessage("No entries in allowlist.");
            return;
        }

        player.SendSuccessMessage(_config.StringifyEntries());
    }

    // Allowlist [add/remove] [ip/name/uuid] <value>
    public void ManageEntry(TSPlayer player, List<string> args)
    {
        if (args.Count < 2)
        {
            player.SendErrorMessage($"Invalid syntax! {_properSyntax}");
            return;
        }

        string action = args[0];
        if (action == "remove")
        {
            RemoveEntry(player, args[1]);
            return;
        }

        string type = args[1];
        string targetPlayerName = args[2];

        if (!AllowedFilterTypes.Contains(type))
        {
            player.SendErrorMessage($"Invalid syntax: {type} is not a valid type! {_properSyntax}");
            return;
        }

        AllowListEntry entry = new AllowListEntry(type, targetPlayerName);
        _config.Add(entry);
        Write();
        player.SendSuccessMessage($"Added entry to allowlist.");
    }

    public void RemoveEntry(TSPlayer player, string targetPlayer)
    {
        var removed = _config.RemoveAllMatches(targetPlayer);
        if (removed == 0)
        {
            player.SendErrorMessage("Could not remove that value, no matches were found.");
            return;
        }

        Write();
        player.SendSuccessMessage($"Removed {removed} matching entries from the allowlist.");
    }


    private void OnJoin(JoinEventArgs args)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_config == null)
        {
            Log($"On join event happened but the config is null: {args.Who}");
            return;
        }

        if (!_config.enabled)
        {
            Log($"On join event happened but config is disabled: {args.Who}");
            return;
        }

        TSPlayer player = TShock.Players[args.Who];
        if (player == null)
        {
            Log($"On join event happened but the player is null: {args.Who}");
            return;
        }

        // If the player has a bypass permission, just allow them
        if (bypassPermissions.Any(perm => player.HasPermission(perm)))
        {
            Log($"On join event happened but the player can bypass the check: {player.Name}");
            return;
        }

        var allowed = _config.allowList.Any(entry => entry.Matches(player));
        if (!allowed)
        {
            Log($"Player is not on whitelist: {player.Name}");
            player.Disconnect(_config.removalReason);
        }
    }

    public void Write()
    {
        try
        {
            var jsonConfig = JsonConvert.SerializeObject(_config, serializeOpts);
            File.WriteAllText(TShockConfigPath, jsonConfig);
        }
        catch (Exception e)
        {
            Log($"Write: Exception happened writing config {e.Message}");
        }
    }

    // Reload from disk
    // allowlist reload
    // LoadConfig returns false if the file does not exist, and true if it does exist, even if it fails to deserialize.
    public bool Load()
    {
        if (!File.Exists(TShockConfigPath))
        {
            Log($"Load: File does not exist {TShockConfigPath}");
            _config = new AllowListConfig();
            return false;
        }

        string fileContent = File.ReadAllText(TShockConfigPath);
        AllowListConfig? configData;
        try
        {
            configData = JsonConvert.DeserializeObject<AllowListConfig>(fileContent, serializeOpts);
        }
        catch (Exception e)
        {
            Log($"Load: Exception happened reading config {e.Message}");
            return true;
        }

        if (configData == null)
        {
            Log($"Load: Config was loaded but not set properly.");
            return true;
        }

        Log("Load: Config loaded successfully.");
        _config = configData;
        return true;
    }
}