using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace Unity.Services.Lobbies
{
    /// <summary>
    /// Optional parameter class for Lobby creation requests.
    /// </summary>
    public class CreateLobbyOptions 
    {
        /// <summary>
        /// Indicates whether or not the lobby is publicly visible and will show up in query results.
        /// If the lobby is not publicly visible, the creator can share the LobbyCode with other users who can use it to join this lobby.
        /// If left as null, a default value will be used.
        /// </summary>
        public bool? IsPrivate { get; set; }

        /// <summary>
        /// Information about a specific player creating the lobby.
        /// </summary>
        public Player Player { get; set; }

        /// <summary>
        /// Custom game-specific properties that apply to the lobby (e.g. mapName or gameType).
        /// </summary>
        public Dictionary<string, DataObject> Data { get; set; }
    }
}