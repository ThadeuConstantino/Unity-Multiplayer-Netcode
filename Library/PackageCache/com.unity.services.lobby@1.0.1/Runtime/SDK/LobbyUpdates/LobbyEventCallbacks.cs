#if UGS_BETA_LOBBY_EVENTS && UGS_LOBBY_EVENTS
using System;
using Unity.Services.Lobbies;

/// <summary>
/// A class for you to provide the callbacks you want to be called from the lobby event subscription.
/// </summary>
public class LobbyEventCallbacks
{
    /// <summary>
    /// Event called when a change has occurred to a lobby on the server.
    /// </summary>
    public event Action<ILobbyChanges> LobbyChanged;

    /// <summary>
    /// Event called when a kick has been received from the lobby event subscription.
    /// </summary>
    public event Action KickedFromLobby;

    /// <summary>
    /// Event called when the connection state of the lobby event subscription changes.
    /// </summary>
    public event Action<LobbyEventConnectionState> LobbyEventConnectionStateChanged;

    internal void InvokeLobbyChanged(ILobbyChanges changes)
    {
        LobbyChanged?.Invoke(changes);
    }

    internal void InvokeKickedFromLobby()
    {
        KickedFromLobby?.Invoke();
    }

    internal void InvokeLobbyEventConnectionStateChanged(LobbyEventConnectionState state)
    {
        LobbyEventConnectionStateChanged?.Invoke(state);
    }
}
#endif
