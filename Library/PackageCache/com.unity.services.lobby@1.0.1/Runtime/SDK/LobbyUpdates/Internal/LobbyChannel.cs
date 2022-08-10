#if UGS_BETA_LOBBY_EVENTS && UGS_LOBBY_EVENTS
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Wire.Internal;
using UnityEngine;

namespace Unity.Services.Lobbies.Internal
{
    internal class LobbyChannel : ILobbyEvents
    {
        private readonly IChannel channelSubscription;

        public LobbyEventCallbacks Callbacks { get; }

        internal LobbyChannel(IChannel channel, LobbyEventCallbacks callbacks)
        {
            channelSubscription = channel;
            Callbacks = callbacks;
            channelSubscription.MessageReceived += (payload) => { OnLobbySubscriptionMessage(payload, callbacks); };
            channelSubscription.KickReceived += () => { OnLobbySubscriptionKick(callbacks); };
            channelSubscription.NewStateReceived += (state) => { OnLobbySubscriptionNewState(state, callbacks); };
        }

        public async Task SubscribeAsync()
        {
            try
            {
                await channelSubscription.SubscribeAsync();
            }
            catch (RequestFailedException ex)
            {
                switch (ex.ErrorCode)
                {
                    case 23002: /* WireErrorCode.CommandFailed */ throw new LobbyServiceException(LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy, $"The connection was lost or dropped while attempting to subscribe.", ex);
                    case 23003: /* WireErrorCode.ConnectionFailed */ throw new LobbyServiceException(LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy, $"The connection was lost or dropped while attempting to subscribe.", ex);
                    case 23008: /* WireErrorCode.AlreadySubscribed */  throw new LobbyServiceException(LobbyExceptionReason.AlreadySubscribedToLobby, $"You are already subscribed to this lobby, you do not need to subscribe again.", ex);
                    default: throw new LobbyServiceException(LobbyExceptionReason.LobbyEventServiceConnectionError, $"There was an error when trying to connect to the lobby service for events. Ensure a valid Lobby ID was sent. Error Code[{ex.ErrorCode}].", ex);
                }
            }
        }

        public async Task UnsubscribeAsync()
        {
            try {
                await channelSubscription.UnsubscribeAsync();
            }
            catch (RequestFailedException ex)
            {
                switch (ex.ErrorCode)
                {
                    case 23002: /* WireErrorCode.CommandFailed */ throw new LobbyServiceException(LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy, $"The connection was lost or dropped while attempting to unsubscribe.", ex);
                    case 23009: /* WireErrorCode.AlreadyUnsubscribed */ throw new LobbyServiceException(LobbyExceptionReason.AlreadyUnsubscribedFromLobby, $"You are already unsubscribed from this lobby, you do not need to unsubscribe again.", ex);
                    default: throw new LobbyServiceException(LobbyExceptionReason.LobbyEventServiceConnectionError, $"There was an error when trying to connect to the lobby service for events. Ensure a valid Lobby ID was sent. Error Code[{ex.ErrorCode}].", ex);
                }
            }
        }

        private void OnLobbySubscriptionMessage(string payload, LobbyEventCallbacks callbacks)
        {
            try
            {
                var changes = LobbyPatcher.GetLobbyChanges(payload);
                callbacks.InvokeLobbyChanged(changes);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void OnLobbySubscriptionKick(LobbyEventCallbacks callbacks)
        {
            callbacks.InvokeKickedFromLobby();
        }

        private void OnLobbySubscriptionNewState(SubscriptionState state, LobbyEventCallbacks callbacks)
        {
            switch (state) {
                case SubscriptionState.Unsubscribed: callbacks.InvokeLobbyEventConnectionStateChanged(LobbyEventConnectionState.Unsubscribed); break;
                case SubscriptionState.Subscribing: callbacks.InvokeLobbyEventConnectionStateChanged(LobbyEventConnectionState.Subscribing); break;
                case SubscriptionState.Synced: callbacks.InvokeLobbyEventConnectionStateChanged(LobbyEventConnectionState.Subscribed); break;
                case SubscriptionState.Unsynced: callbacks.InvokeLobbyEventConnectionStateChanged(LobbyEventConnectionState.Unsynced); break;
                case SubscriptionState.Error: callbacks.InvokeLobbyEventConnectionStateChanged(LobbyEventConnectionState.Error); break;
                default: callbacks.InvokeLobbyEventConnectionStateChanged(LobbyEventConnectionState.Unknown); break;
            }
        }

    }
}
#endif
