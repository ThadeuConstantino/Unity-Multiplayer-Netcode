using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;

namespace Unity.Services.Lobbies
{
    /// <summary>
    /// Interface used for editing the configuration of the lobby service SDK.
    /// Primary usage is for testing purposes.
    /// </summary>
    public interface ILobbyServiceSDKConfiguration
    {
        /// <summary>
        /// Sets the base path in configuration.
        /// </summary>
        /// <param name="basePath">The base path to set in configuration.</param>
        void SetBasePath(string basePath);

#if UGS_BETA_LOBBY_EVENTS && UGS_LOBBY_EVENTS
        /// <summary>
        /// If the Wire package is available, but you do not want the Lobby package to use Wire for some reason.
        /// </summary>
        void DisableLobbyUpdates();
#endif
    }
}
