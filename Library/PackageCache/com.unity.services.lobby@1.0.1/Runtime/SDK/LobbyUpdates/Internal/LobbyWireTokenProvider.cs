#if UGS_BETA_LOBBY_EVENTS && UGS_LOBBY_EVENTS
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using Unity.Services.Wire.Internal;
using UnityEngine;

namespace Unity.Services.Lobbies.Internal
{
    internal class LobbyWireTokenProvider : IChannelTokenProvider
    {
        string lobbyId;
        WrappedLobbyService lobbyService;

        internal LobbyWireTokenProvider(string lobbyId, WrappedLobbyService lobbyService)
        {
            if (lobbyId == null)
            {
                Debug.LogError($"{nameof(LobbyWireTokenProvider)} is invalid as its {nameof(lobbyId)} is null!");
            }
            if (lobbyService == null)
            {
                Debug.LogError($"{nameof(LobbyWireTokenProvider)} is invalid as its {nameof(lobbyService)} is null!");
            }
            this.lobbyId = lobbyId;
            this.lobbyService = lobbyService;
        }

        public async Task<ChannelToken> GetTokenAsync()
        {
            var tokens = await lobbyService.RequestTokensAsync(lobbyId, TokenRequest.TokenTypeOptions.WireJoin);
            if (tokens.TryGetValue("wireJoin", out var tokenData))
            {
                return new ChannelToken() { ChannelName = tokenData.Uri, Token = tokenData.TokenValue };
            }
            return new ChannelToken();
        }
    }
}
#endif
