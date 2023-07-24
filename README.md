# Better Allowlist

This is a simple allowlist for TShock. It allows you to specify a list of users by IP, UUID, or Character Name, or combination of all 3.

## Command Reference

### Add

Adds a player's IP, UUID, Name, or all 3 to the allowlist.

```
/allowlist add [ip/uuid/name/all] <player-name>
```

### Remove

Remove a player from the list by name.

```
/allowlist remove <player-name>
```

### Reload 

Reloads the allowlist from the file.

```
/allowlist reload
```

### List

Lists all players on the allowlist.

```
/allowlist list
```

### Enable / Disable

Enables or disables the allowlist.

```
/allowlist [enable/disable]
```


## Example config

### allowlist-config.json
```json
{
  "enabled": true,
  "allowList": [
    {
      // IP only
      "ip": ""
    },
    {
      // UUID only
      "uuid": ""
    },
    {
      // Name only
      "name": ""
    },
    {
      // Requires all 3 to match to be allowed
      "ip": "127.0.0.1",
      "uuid": "SOMEVERYLONGSTRINGOFCHARACTERSANDNUMBERSHERE",
      "name": "loganintech"
    }
  ],
  "removalReason": "You are not on the allowlist."
}
```