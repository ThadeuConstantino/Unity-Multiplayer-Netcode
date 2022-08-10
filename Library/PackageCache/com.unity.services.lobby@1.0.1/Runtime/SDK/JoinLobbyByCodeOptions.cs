
using Unity.Services.Lobbies.Models;

namespace Unity.Services.Lobbies
{
    /// <summary>
    /// Optional parameter class for Lobby Join By Code requests
    /// </summary>
    public class JoinLobbyByCodeOptions
    {
        /// <summary>
        /// Information about a specific player joining the lobby.
        /// </summary>
        public Player Player { get; set; }
    }
}