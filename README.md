# Better Allowlist

The Better Allowlist is a plugin for TShock that allows you to manage a list of allowed players based on their IP, UUID, or Character Name, or any combination of these criteria.

## Command Reference

- **Add**: Adds a player's IP, UUID, Name, or all 3 to the allowlist.
  ```
  /allowlist add [ip/uuid/name/all] <player-name>
  ```

- **Remove**: Remove a player from the allowlist using their name.
  ```
  /allowlist remove <player-name>
  ```

- **Reload**: Reload the allowlist from the file.
  ```
  /allowlist reload
  ```

- **List**: View all players currently on the allowlist.
  ```
  /allowlist list
  ```

- **Enable / Disable**: Enable or disable the allowlist as needed.
  ```
  /allowlist [enable/disable]
  ```

## Example Configuration

Below is an example of the configuration file, `allowlist-config.json`:

```json
{
  "enabled": true,
  "allowList": [
    {
      // Only IP
      "ip": ""
    },
    {
      // Only UUID
      "uuid": ""
    },
    {
      // Only Name
      "name": ""
    },
    {
      // All 3 criteria required for access
      "ip": "127.0.0.1",
      "uuid": "SOMEVERYLONGSTRINGOFCHARACTERSANDNUMBERSHERE",
      "name": "loganintech"
    }
  ],
  "removalReason": "You are not on the allowlist."
}
```

With the Better Allowlist plugin, managing access to your TShock server becomes more convenient and flexible. By utilizing IPs, UUIDs, and Character Names, or any combination of these, you can efficiently control who can join your server.