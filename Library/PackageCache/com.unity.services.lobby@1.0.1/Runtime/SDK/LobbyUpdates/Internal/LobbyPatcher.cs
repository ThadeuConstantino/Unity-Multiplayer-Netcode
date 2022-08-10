#if UGS_BETA_LOBBY_EVENTS && UGS_LOBBY_EVENTS
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Scripting;

internal static class LobbyPatcher
{
    private const int MaxPlayerCount = 100;

    internal class LobbyPatch
    {
        public string op;
        public string path;
        public object value;

        [Preserve]
        public LobbyPatch()
        {
            // We must preserve the constructor or it might get pulled out during build (such as on iOS devices).
            // This is because this class is only accessed via JsonConvert.DeserializeObject<>
        }
    }

    internal class LobbyPatches
    {
        public int Version;
        public List<LobbyPatch> Patches;

        [Preserve]
        public LobbyPatches()
        {
            // We must preserve the constructor or it might get pulled out during build (such as on iOS devices).
            // This is because this class is only accessed via JsonConvert.DeserializeObject<>
        }
    }

    internal static void ApplyPatchesToLobby(ILobbyChanges changes, Lobby lobbyToChange)
    {
        if (changes.LobbyDeleted)
        {
            Debug.LogWarning("Attempting to apply changes to lobby, but the lobby has been deleted. Check if a lobby has been deleted by checking .LobbyDeleted");
            return;
        }

        // The order in which we apply changes should be consistent.
        // It's useful to keep this function as the single source of truth for the order changes are applied.
        if (changes.Name.Changed)
        {
            lobbyToChange.Name = changes.Name.Value;
        }
        if (changes.IsPrivate.Changed)
        {
            lobbyToChange.IsPrivate = changes.IsPrivate.Value;
        }
        if (changes.IsLocked.Changed)
        {
            lobbyToChange.IsLocked = changes.IsLocked.Value;
        }
        if (changes.AvailableSlots.Changed)
        {
            lobbyToChange.AvailableSlots = changes.AvailableSlots.Value;
        }
        if (changes.MaxPlayers.Changed)
        {
            lobbyToChange.MaxPlayers = changes.MaxPlayers.Value;
        }
        if (changes.Data.Removed)
        {
            if (lobbyToChange.Data != null) {
                lobbyToChange.Data.Clear();
            }
        }
        else if (changes.Data.Changed)
        {
            if (lobbyToChange.Data == null)
            {
                lobbyToChange.Data = new Dictionary<string, DataObject>();
            }
            foreach (var change in changes.Data.Value)
            {
                if (change.Value.Removed)
                {
                    lobbyToChange.Data.Remove(change.Key);
                }
                else
                {
                    lobbyToChange.Data[change.Key] = change.Value.Value;
                }
            }
        }
        // It is important that we remove players before adding new players.
        // Removing, then adding, is expected by the service.
        if (changes.PlayerLeft.Changed)
        {
            // We need to sort so we remove the highest index first
            // This is to avoid removing the wrong player. For example:
            // We have players A, B, C, D, E, F
            // We are told to remove players at index 2 and 3, C and D.
            // In ascending order, we have a bug:
            // A, B,  , D, E, F - we remove player at index 2
            // A, B, D, E, F    - everyone above player C is pushed down
            // A, B, D,  , F    - we remove player at index 3
            //                    this is a bug! we just removed player E, not D!
            // To avoid this, we sort into descending order:
            // A, B, C,  , E, F - we remove player at index 3
            // A, B, C, E, F    - everyone above player D is pushed down
            // A, B,  , E, F    - we remove player at index 2
            // A, B, E, F       - everyone above player C is pushed down
            // we successfully removed player C and D!
            var playerIndicesToRemove = changes.PlayerLeft.Value;
            playerIndicesToRemove.Sort((first, second) => second.CompareTo(first));
            foreach (var playerLeftIndex in playerIndicesToRemove)
            {
                lobbyToChange.Players.RemoveAt(playerLeftIndex); // TODO: Check if the index is valid
            }
        }
        if (changes.PlayerJoined.Changed)
        {
            if (lobbyToChange.Players == null)
            {
                lobbyToChange.Players = new List<Player>(changes.PlayerJoined.Value.Count);
            }
            foreach (var playerJoined in changes.PlayerJoined.Value)
            {
                lobbyToChange.Players.Insert(playerJoined.PlayerIndex, playerJoined.Player);
            }
        }
        if (changes.PlayerData.Changed)
        {
            foreach (var playerDataChanged in changes.PlayerData.Value)
            {
                var dataChanged = playerDataChanged.Value;
                var index = playerDataChanged.Value.PlayerIndex;
                var playerToChange = lobbyToChange.Players[index];
                if (dataChanged.ConnectionInfoChanged.Changed)
                {
                    playerToChange.ConnectionInfo = playerDataChanged.Value.ConnectionInfoChanged.Value;
                }
                if (dataChanged.LastUpdatedChanged.Changed)
                {
                    playerToChange.LastUpdated = dataChanged.LastUpdatedChanged.Value;
                }
                if (dataChanged.ChangedData.Removed)
                {
                    if (playerToChange.Data != null) {
                        playerToChange.Data.Clear();
                    }
                }
                else if (dataChanged.ChangedData.Changed)
                {
                    if (playerToChange.Data == null)
                    {
                        playerToChange.Data = new Dictionary<string, PlayerDataObject>();
                    }
                    foreach (var keyValuePair in dataChanged.ChangedData.Value)
                    {
                        if (keyValuePair.Value.Removed)
                        {
                            playerToChange.Data.Remove(keyValuePair.Key);
                        }
                        else
                        {
                            playerToChange.Data[keyValuePair.Key] = keyValuePair.Value.Value;
                        }
                    }
                }
            }
        }
        // The HostId is related to the players, so applying this after editing players is preferred.
        if (changes.HostId.Changed)
        {
            lobbyToChange.HostId = changes.HostId.Value;
        }
        if (changes.LastUpdated.Changed)
        {
            lobbyToChange.LastUpdated = changes.LastUpdated.Value;
        }
    }

    internal static LobbyPatcherChanges GetLobbyChanges(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("Unable to apply patches to lobby as the provided JSON was null!");
        }
        var lobbyPatches = JsonConvert.DeserializeObject<LobbyPatches>(json);
        if (lobbyPatches == null)
        {
            Debug.LogError("Unable to deserialize JSON to LobbyPatches!");
        }
        return GetLobbyPatches(lobbyPatches);
    }

    internal static LobbyPatcherChanges GetLobbyPatches(LobbyPatches lobbyPatches)
    {
        if (lobbyPatches.Patches == null || lobbyPatches.Patches.Count < 1)
        {
            Debug.LogWarning("Attempting to apply patches to lobby, but there were no patches to apply.");
            return new LobbyPatcherChanges();
        }
        var changes = new LobbyPatcherChanges();
        foreach (var patchToApply in lobbyPatches.Patches)
        {
            ParseLobbyPatch(patchToApply, changes);
        }
        return changes;
    }

    private static void ParseLobbyPatch(LobbyPatch patch, LobbyPatcherChanges changes)
    {
        switch (patch.op)
        {
            case "add": ParseAddPatch(patch, changes); break;
            case "replace": ParseReplacePatch(patch, changes); break;
            case "remove": ParseRemovePatch(patch, changes); break;
            default: Debug.LogError($"patch.op({patch.op}) is not implemented by the {nameof(LobbyPatcher)}"); break;
        }
    }

    private static void ParseAddPatch(LobbyPatch patch, LobbyPatcherChanges changes)
    {

        if (patch.path.StartsWith("/data/"))
        {
            ParseLobbyDataAddOrReplacePatch(patch, changes);
        }
        else if (patch.path.StartsWith("/players/"))
        {
            ParsePlayerAddPatch(patch, changes);
        }
        else
        {
            switch (patch.path)
            {

                case "/name": { changes.NameChange((string)patch.value); break; }
                case "/isPrivate": { changes.IsPrivateChange((bool)patch.value); break; }
                case "/isLocked": { changes.IsLockedChange((bool)patch.value); break; }
                case "/availableSlots": { changes.AvailableSlotsChange((int)(Int64)patch.value); break; }
                case "/maxPlayers": { changes.MaxPlayersChange((int)(Int64)patch.value); break; }
                case "/data": { ParseAddLobbyData((JObject)patch.value, changes); break; }
                case "/hostId": { changes.HostChange((string)patch.value); break; }
                case "/lastUpdated": { changes.LastUpdatedChange((DateTime)patch.value); break; }
                default: Debug.LogError($"Not implemented add patch with path[{patch.path}]"); break;
            }
        }
    }

    private static void ParseReplacePatch(LobbyPatch patch, LobbyPatcherChanges changes)
    {
        if (patch.path.StartsWith("/data/"))
        {
            ParseLobbyDataAddOrReplacePatch(patch, changes);
        }
        else if (patch.path.StartsWith("/players/"))
        {
            ParsePlayerReplacePatch(patch, changes);
        }
        else
        {
            switch (patch.path)
            {
                case "/name": { changes.NameChange((string)patch.value); break; }
                case "/isPrivate": { changes.IsPrivateChange((bool)patch.value); break; }
                case "/isLocked": { changes.IsLockedChange((bool)patch.value); break; }
                case "/availableSlots": { changes.AvailableSlotsChange((int)(Int64)patch.value); break; }
                case "/maxPlayers": { changes.MaxPlayersChange((int)(Int64)patch.value); break; }
                case "/hostId": { changes.HostChange((string)patch.value); break; }
                case "/lastUpdated": { changes.LastUpdatedChange((DateTime)patch.value); break; }
                default: Debug.LogError($"Not implemented replace patch with path[{patch.path}]"); break;
            }
        }
    }

    private static void ParseRemovePatch(LobbyPatch patch, LobbyPatcherChanges changes)
    {
        if (patch.path.StartsWith("/data/"))
        {
            ParseLobbyDataRemovePatch(patch, changes);
        }
        else if (patch.path.StartsWith("/players/"))
        {
            ParsePlayerRemovePatch(patch, changes);
        }
        else
        {
            switch (patch.path)
            {
                case "/name": { changes.NameChange(null); break; }
                case "/isPrivate": { changes.IsPrivateChange(false); break; }
                case "/isLocked": { changes.IsLockedChange(false); break; }
                case "/availableSlots": { changes.AvailableSlotsChange(0); break; }
                case "/maxPlayers": { changes.MaxPlayersChange(MaxPlayerCount); break; }
                case "/data": { ParseLobbyDataRemovePatch(patch, changes); break;}
                case "/hostId": { changes.HostChange(null); break; }
                case "/": { changes.LobbyDeletedChange(); break; }
                default: Debug.LogError($"Not implemented remove patch with path[{patch.path}]"); break;
            }
        }
    }

    private static void ParseAddLobbyData(JObject data, LobbyPatcherChanges changes)
    {
        foreach (var dataToAdd in data)
        {
            changes.DataChange(dataToAdd.Key, dataToAdd.Value.ToObject<DataObject>());
        }
    }

    private static void ParseLobbyDataAddOrReplacePatch(LobbyPatch patch, LobbyPatcherChanges changes)
    {
        var startLength = "/data/".Length;
        var pathEnd = patch.path.IndexOf('/', startLength);
        var key = pathEnd < 0 ? patch.path.Substring(startLength) : patch.path.Substring(pathEnd);
        var jObject = (JObject)patch.value;
        changes.DataChange(key, jObject.ToObject<DataObject>());
    }

    private static void ParseLobbyDataRemovePatch(LobbyPatch patch, LobbyPatcherChanges changes)
    {
        if (patch.path == "/data")
        {
            changes.DataRemoveChange();
        }
        else
        {
            var key = patch.path.Substring("/data/".Length);
            changes.DataRemoveChange(key);
        }
    }

    private static string GetPlayerPathAndIndex(LobbyPatch patch, out int playerIndex)
    {
        var sections = patch.path.Split('/');
        // if path is of the form "/players/0/data"
        //                        ^   ^     ^  ^
        // Sections when split:   0   1     2  extra
        const int startPath = 1;
        const int index = 2;
        const int extraPath = 3;
        if (!int.TryParse(sections[index], out playerIndex))
        {
            var builder = new StringBuilder();
            foreach (var section in sections)
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }
                builder.Append($"\"{section}\"");
            }
            throw new InvalidOperationException($"Unable to parse section[{sections[index]}] from sections[{builder}]");
        }
        else
        {
            if (sections.Length <= extraPath)
            {
                return $"/{sections[startPath]}";
            }
            var remainingPath = patch.path.Substring(sections[startPath].Length + sections[index].Length + 2);
            return remainingPath;
        }
    }

    private static void ParsePlayerAddPatch(LobbyPatch patch, LobbyPatcherChanges changes)
    {
        var path = GetPlayerPathAndIndex(patch, out var index);
        if (path.StartsWith("/data"))
        {
            ParseAddOrReplacePlayerData(index, patch, path, changes);
        }
        else
        {
            switch (path)
            {
                case "/players": { ParseAddPlayer(patch, index, changes); break; }
                case "/connectionInfo": { changes.PlayerConnectionInfoChange(index, (string)patch.value); break; }
                default: Debug.LogError($"Not implemented add player patch with path[{path}] from player patch[{patch.path}]"); break;
            }
        }
    }

    private static void ParsePlayerReplacePatch(LobbyPatch patch, LobbyPatcherChanges changes)
    {
        var path = GetPlayerPathAndIndex(patch, out var index);
        if (path.StartsWith("/data"))
        {
            ParseAddOrReplacePlayerData(index, patch, path, changes);
        }
        else
        {
            switch (path)
            {
                case "/connectionInfo": { changes.PlayerConnectionInfoChange(index, (string)patch.value); break; }
                case "/lastUpdated": { changes.PlayerLastUpdatedChange(index, (DateTime)patch.value); break; }
                default: Debug.LogError($"Not implemented replace player patch with path[{path}]"); break;
            }
        }
    }

    private static void ParsePlayerRemovePatch(LobbyPatch patch, LobbyPatcherChanges changes)
    {
        var path = GetPlayerPathAndIndex(patch, out var index);
        if (path.StartsWith("/data"))
        {
            ParseRemovePlayerData(index, patch, path, changes);
        }
        else
        {
            switch (path)
            {
                case "/players": { changes.PlayerLeftChange(index); break; }
                case "/connectionInfo": { changes.PlayerConnectionInfoChange(index, null); break; }
                default: Debug.LogError($"Not implemented remove player patch with path[{path}]"); break;
            }
        }
    }

    private static void ParseAddPlayer(LobbyPatch patch, int index, LobbyPatcherChanges changes)
    {
        var playerObject = (JObject)patch.value;
        var player = playerObject.ToObject<Player>();
        changes.PlayerJoinedChange(index, player);
    }

    private static void ParseAddOrReplacePlayerData(int index, LobbyPatch patch, string path, LobbyPatcherChanges changes)
    {
        if (path == "/data")
        {
            var data = (JObject) patch.value;
            foreach (var dataToAdd in data)
            {
                changes.PlayerDataChange(index, dataToAdd.Key, dataToAdd.Value.ToObject<PlayerDataObject>());
            }
        }
        else
        {
            var sections = patch.path.Split('/');
            var key = sections[4];
            var obj = (JObject)patch.value;
            changes.PlayerDataChange(index, key, obj.ToObject<PlayerDataObject>());
        }
    }

    private static void ParseRemovePlayerData(int index, LobbyPatch patch, string path, LobbyPatcherChanges changes)
    {
        if (path == "/data")
        {
            changes.PlayerDataRemoveChange(index);
        }
        else
        {
            var key = path.Substring("/data/".Length);
            changes.PlayerDataRemoveChange(index, key);
        }
    }
}
#endif
