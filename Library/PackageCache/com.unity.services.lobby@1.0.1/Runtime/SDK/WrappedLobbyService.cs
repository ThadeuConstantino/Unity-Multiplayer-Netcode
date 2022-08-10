using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies.Http;
using Unity.Services.Lobbies.Lobby;
using Debug = Unity.Services.Lobbies.Logger;
#if UGS_BETA_LOBBY_EVENTS && UGS_LOBBY_EVENTS
using Unity.Services.Wire.Internal;
#endif

namespace Unity.Services.Lobbies.Internal
{
    /// <summary>
    /// The Lobby Service enables clients to create/host, join, delete lobbies using the bespoke underlying relay protocol.
    /// </summary>

#pragma warning disable CS0618 // Ignoring warning as we want to implement ILobbyServiceSDK for backwards compatibility.
    internal class WrappedLobbyService : ILobbyService, ILobbyServiceSDK, ILobbyServiceSDKConfiguration, ILobbyServiceInternal
#pragma warning restore CS0618
    {
        internal ILobbyServiceSdk m_LobbyService;

        //Minimum value of a lobby error (used to elevate standard errors if unhandled)
        internal const int LOBBY_ERROR_MIN_RANGE = 16000;

        internal WrappedLobbyService(ILobbyServiceSdk lobbyService)
        {
            m_LobbyService = lobbyService;
        }

#if UGS_BETA_LOBBY_EVENTS && UGS_LOBBY_EVENTS
        /// <inheritdoc/>
        public void DisableLobbyUpdates()
        {
            m_LobbyService.Wire = null;
        }
#endif

        /// <inheritdoc/>
        public async Task<Models.Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, CreateLobbyOptions options = default)
        {
            if (string.IsNullOrWhiteSpace(lobbyName)) 
            {
                throw new ArgumentNullException("lobbyName", "Argument should be non-null, non-empty & not only whitespaces.");
            }
            if (maxPlayers < 1) 
            {
                throw new InvalidOperationException("Parameters 'maxPlayers' cannot be less than 1.");
            }

            var createRequest = new CreateRequest(
                name: lobbyName,
                maxPlayers: maxPlayers,
                isPrivate: options?.IsPrivate,
                player:  options?.Player,
                data: options?.Data
            );

            var response = await TryCatchRequest(m_LobbyService.LobbyApi.CreateLobbyAsync, new CreateLobbyRequest(createRequest));
            var lobby = response.Result;
            return lobby;
        }

#if UGS_BETA_LOBBY_EVENTS && UGS_LOBBY_EVENTS
        /// <inheritdoc/>
        public async Task<ILobbyEvents> SubscribeToLobbyEventsAsync(string lobbyId, LobbyEventCallbacks lobbyEventCallbacks)
        {
            if (m_LobbyService.Wire != null)
            {
                var channel = m_LobbyService.Wire.CreateChannel(new LobbyWireTokenProvider(lobbyId, this));
                var lobbyChannel = new LobbyChannel(channel, lobbyEventCallbacks);
                await lobbyChannel.SubscribeAsync();
                return lobbyChannel;
            }
            return null;
        }
#endif

        /// <inheritdoc/>
        public async Task DeleteLobbyAsync(string lobbyId)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException("lobbyId", "Argument should be non-null, non-empty & not only whitespaces.");
            }
            await TryCatchRequest(m_LobbyService.LobbyApi.DeleteLobbyAsync, new DeleteLobbyRequest(lobbyId));
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetJoinedLobbiesAsync() 
        {
            var request = new GetJoinedLobbiesRequest();
            var response = await TryCatchRequest(m_LobbyService.LobbyApi.GetJoinedLobbiesAsync, request);
            return response.Result;
        }

        /// <inheritdoc/>
        public async Task<Models.Lobby> GetLobbyAsync(string lobbyId)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException("lobbyId", "Argument should be non-null, non-empty & not only whitespaces.");
            }

            var response = await TryCatchRequest(m_LobbyService.LobbyApi.GetLobbyAsync, new GetLobbyRequest(lobbyId));
            return response.Result;
        }

        /// <inheritdoc/>
        public async Task SendHeartbeatPingAsync(string lobbyId)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException("lobbyId", "Argument should be non-null, non-empty & not only whitespaces.");
            }

            await TryCatchRequest(m_LobbyService.LobbyApi.HeartbeatAsync, new HeartbeatRequest(lobbyId));
        }

        /// <inheritdoc/>
        public async Task<Models.Lobby> JoinLobbyByCodeAsync(string lobbyCode, JoinLobbyByCodeOptions options = default)
        {
            if (string.IsNullOrWhiteSpace(lobbyCode))
            {
                throw new ArgumentNullException("lobbyCode", "Argument should be non-null, non-empty & not only whitespaces.");
            }

            try
            {
                var joinRequest = new JoinLobbyByCodeRequest(new JoinByCodeRequest(lobbyCode, options?.Player));
                var response = await TryCatchRequest(m_LobbyService.LobbyApi.JoinLobbyByCodeAsync, joinRequest);
                return response.Result;
            }
            catch (LobbyServiceException e) 
            {
                //JoinLobby conflict 409 handling (MPSSDK-92)
                if (e.Reason == LobbyExceptionReason.LobbyConflict)
                {
                    var lobby = await LobbyConflictResolver(options?.Player, null, e);
                    if (lobby != null)
                    {
                        return lobby;
                    }
                }
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Models.Lobby> JoinLobbyByIdAsync(string lobbyId, JoinLobbyByIdOptions options = default)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException("lobbyId", "Argument should be non-null, non-empty & not only whitespaces.");
            }

            try
            {
                var joinRequest = new JoinLobbyByIdRequest(lobbyId, options?.Player);
                var response = await TryCatchRequest(m_LobbyService.LobbyApi.JoinLobbyByIdAsync, joinRequest);
                return response.Result;
            }
            catch (LobbyServiceException e)
            {
                //JoinLobby conflict 409 handling (MPSSDK-92)
                if (e.Reason == LobbyExceptionReason.LobbyConflict)
                {
                    var lobby = await LobbyConflictResolver(options?.Player, lobbyId, e);
                    if (lobby != null)
                    {
                        return lobby;
                    }
                }
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<QueryResponse> QueryLobbiesAsync(QueryLobbiesOptions options = default)
        {
            var queryRequest = options == null ? null : new QueryRequest(options.Count, options.Skip, options.SampleResults, options.Filters, options.Order, options.ContinuationToken);
            var queryLobbiesRequest = new QueryLobbiesRequest(queryRequest);
            var response = await TryCatchRequest(m_LobbyService.LobbyApi.QueryLobbiesAsync, queryLobbiesRequest);
            return response.Result;
        }

        /// <inheritdoc/>
        public async Task<Models.Lobby> QuickJoinLobbyAsync(QuickJoinLobbyOptions options = default)
        {
            try
            {
                var quickJoinRequest = options == null ? null : new QuickJoinRequest(options.Filter, options.Player);
                var quickJoinLobbyRequest = new QuickJoinLobbyRequest(quickJoinRequest);
                var response = await TryCatchRequest(m_LobbyService.LobbyApi.QuickJoinLobbyAsync, quickJoinLobbyRequest);
                return response.Result;
            }
            catch (LobbyServiceException e)
            {
                //JoinLobby conflict 409 handling (MPSSDK-92)
                if (e.Reason == LobbyExceptionReason.LobbyConflict)
                {
                    var lobby = await LobbyConflictResolver(options?.Player, null, e);
                    if (lobby != null)
                    {
                        return lobby;
                    }
                }
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task RemovePlayerAsync(string lobbyId, string playerId)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException("lobbyId", "Argument should be non-null, non-empty & not only whitespaces.");
            }
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new ArgumentNullException("playerId", "Argument should be non-null, non-empty & not only whitespaces.");
            }
            var removePlayerRequest = new RemovePlayerRequest(lobbyId, playerId);
            await TryCatchRequest(m_LobbyService.LobbyApi.RemovePlayerAsync, removePlayerRequest);
        }

        /// <inheritdoc/>
        public async Task<Models.Lobby> UpdateLobbyAsync(string lobbyId, UpdateLobbyOptions options)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException("lobbyId", "Argument should be non-null, non-empty & not only whitespaces.");
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), "Update Lobby Options object must not be null.");
            }
            var updateRequest = options == null ? null : new UpdateRequest(options.Name, options.MaxPlayers, options.IsPrivate, options.IsLocked, options.Data, options.HostId);
            var updateLobbyRequest = new UpdateLobbyRequest(lobbyId, updateRequest);
            var response = await TryCatchRequest(m_LobbyService.LobbyApi.UpdateLobbyAsync, updateLobbyRequest);
            return response.Result;
        }

        /// <inheritdoc/>
        public async Task<Models.Lobby> UpdatePlayerAsync(string lobbyId, string playerId, UpdatePlayerOptions options)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException("lobbyId", "Argument should be non-null, non-empty & not only whitespaces.");
            }
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new ArgumentNullException("playerId", "Argument should be non-null, non-empty & not only whitespaces.");
            }
            if (options == null) 
            {
                throw new ArgumentNullException(nameof(options), "Update Player Options object must not be null.");
            }
            var playerUpdateRequest = options == null ? null : new PlayerUpdateRequest(options.ConnectionInfo, options.Data, options.AllocationId);
            var updatePlayerRequest = new UpdatePlayerRequest(lobbyId, playerId, playerUpdateRequest);
            var response = await TryCatchRequest(m_LobbyService.LobbyApi.UpdatePlayerAsync, updatePlayerRequest);
            return response.Result;
        }

        /// <inheritdoc/>
        public async Task<Models.Lobby> ReconnectToLobbyAsync(string lobbyId)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException("lobbyId", "Argument should be non-null, non-empty & not only whitespaces.");
            }
            var reconnectRequest = new ReconnectRequest(lobbyId);
            var response = await TryCatchRequest(m_LobbyService.LobbyApi.ReconnectAsync, reconnectRequest);
            return response.Result;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, Models.TokenData>> RequestTokensAsync(string lobbyId, params TokenRequest.TokenTypeOptions[] tokenOptions)
        {
            if (tokenOptions == null || tokenOptions.Length < 1)
            {
                throw new ArgumentNullException("Unable to request tokens when no token options were chosen to receive from the request!");
            }
            var tokenRequestOptions = new List<TokenRequest>(tokenOptions.Length);
            foreach (var tokenOption in tokenOptions)
            {
                tokenRequestOptions.Add(new TokenRequest(tokenOption));
            }
            var requestTokensRequest = new RequestTokensRequest(lobbyId, tokenRequestOptions);
            var response = await TryCatchRequest(m_LobbyService.LobbyApi.RequestTokensAsync, requestTokensRequest);
            return response.Result;
        }

        public void SetBasePath(string basePath)
        {
            m_LobbyService.Configuration.BasePath = basePath;
        }

        #region Helper Functions

        // Helper function to reduce code duplication of try-catch
        private async Task<Response> TryCatchRequest<TRequest>(Func<TRequest, Configuration, Task<Response>> func, TRequest request) 
        {
            Response response = null;
            try
            {
                response = await func(request, m_LobbyService.Configuration);
            }
            catch (HttpException<ErrorStatus> he)
            {
                ResolveErrorWrapping((LobbyExceptionReason)he.ActualError.Code, he);
            }
            catch (HttpException he)
            {
                int httpErrorStatusCode = (int)he.Response.StatusCode;
                LobbyExceptionReason reason = LobbyExceptionReason.Unknown;
                if (he.Response.IsNetworkError)
                {
                    reason = LobbyExceptionReason.NetworkError;
                }
                else if (he.Response.IsHttpError)
                {
                    //Elevate unhandled http codes to lobby enum range
                    if (httpErrorStatusCode < 1000)
                    {
                        httpErrorStatusCode += LOBBY_ERROR_MIN_RANGE;
                        if (Enum.IsDefined(typeof(LobbyExceptionReason), httpErrorStatusCode))
                        {
                            reason = (LobbyExceptionReason)httpErrorStatusCode;
                        }
                    }
                }

                ResolveErrorWrapping(reason, he);
            }
            catch (Exception e)
            {
                //Pass error code that will throw default label, provide exception object for stack trace.
                ResolveErrorWrapping(LobbyExceptionReason.Unknown, e);
            }
            return response;
        }

        // Helper function to reduce code duplication of try-catch (generic version)
        private async Task<Response<TReturn>> TryCatchRequest<TRequest, TReturn>(Func<TRequest, Configuration, Task<Response<TReturn>>> func, TRequest request)
        {
            Response<TReturn> response = null;
            try
            {
                response = await func(request, m_LobbyService.Configuration);
            }
            catch (HttpException<ErrorStatus> he) 
            {
                ResolveErrorWrapping((LobbyExceptionReason) he.ActualError.Code, he);
            }
            catch (HttpException he)
            {
                int httpErrorStatusCode = (int) he.Response.StatusCode;
                LobbyExceptionReason reason = LobbyExceptionReason.Unknown;
                if (he.Response.IsNetworkError)
                {
                    reason = LobbyExceptionReason.NetworkError;
                }
                else if (he.Response.IsHttpError)
                {
                    //Elevate unhandled http codes to lobby enum range
                    if (httpErrorStatusCode < 1000)
                    {
                        httpErrorStatusCode += LOBBY_ERROR_MIN_RANGE;
                        if (Enum.IsDefined(typeof(LobbyExceptionReason), httpErrorStatusCode)) 
                        {
                            reason = (LobbyExceptionReason) httpErrorStatusCode;
                        }
                    }
                }

                ResolveErrorWrapping(reason, he);
            }
            catch (Exception e)
            {
                //Pass error code that will throw default label, provide exception object for stack trace.
                ResolveErrorWrapping(LobbyExceptionReason.Unknown, e);
            }
            return response;
        }

        // Helper function to resolve the new wrapped error/exception based on input parameter
        private void ResolveErrorWrapping(LobbyExceptionReason reason, Exception exception = null) 
        {
            if (reason == LobbyExceptionReason.Unknown)
            {
                Debug.LogError($"{Enum.GetName(typeof(LobbyExceptionReason), reason)}, ({(int)reason}). Message: Something went wrong.");
                throw new LobbyServiceException(reason, "Something went wrong.", exception);
            }
            else 
            {
                //Check if the exception is of type HttpException<ErrorStatus> - extract api user-facing message
                HttpException<ErrorStatus> apiException = exception as HttpException<ErrorStatus>;
                if (apiException != null)
                {
                    Debug.LogError($"{Enum.GetName(typeof(LobbyExceptionReason), reason)}, ({(int) reason}). Message: {apiException.ActualError.Detail}");
                    throw new LobbyServiceException(reason, apiException.ActualError.Detail, apiException);
                }
                else 
                {
                    //Other general exception message handling
                    Debug.LogError($"{Enum.GetName(typeof(LobbyExceptionReason), reason)}, ({(int)reason}). Message: {exception.Message}");
                    throw new LobbyServiceException(reason, exception.Message, exception);
                }
            }
        }

        /// <summary>
        /// Helper function to resolve lobby conflicts due to potentially lost responses (MPSSDK-92)
        /// N.B. lobbyId will need to be inferred from GetLobby if parameter is invalid (this may have unintended effects if the user has joined multiple lobbies)
        /// </summary>
        /// <param name="player">Player data to update with if any data mismatches are encountered</param>
        /// <param name="lobbyId">(Optional) target lobbyId that the request failed on</param>
        /// <param name="e">(Optional) exception to nest as innerException if new Exception is thrown</param>
        /// <returns>Lobby currently joined (amending any mismatched data), otherwise null</returns>
        private async Task<Models.Lobby> LobbyConflictResolver(Player player, string lobbyId = default, LobbyServiceException e = null)
        {
            List<string> joinedLobbies;
            if (!string.IsNullOrWhiteSpace(lobbyId))
            {
                joinedLobbies = await GetJoinedLobbiesAsync();
                if (joinedLobbies.Count != 1) 
                {
                    return null;
                }

                lobbyId = joinedLobbies[0];
            }

            //Call GetLobby to get existing details
            Models.Lobby getLobbyResult = await GetLobbyAsync(lobbyId);

            //Check to see we have a valid lobby and a valid player object for amending data.
            //N.B. We do not validate anything beyond PlayerId in the Player object.
            if (getLobbyResult == null || player?.Id == null) 
            {
                return getLobbyResult;
            }

            var lobbyPlayers = getLobbyResult.Players;
            var playerObjectInLobby = lobbyPlayers.FirstOrDefault(x => x.Id == player.Id);
            if (playerObjectInLobby == null)
            {
                throw new LobbyServiceException(LobbyExceptionReason.PlayerNotFound, "Lobby join call failed and player was not added to Lobby due to an unexpected error.", e);
            }

            //if player is part of the lobby and their details don't match (e.g. different relay alloc id), call update player to update details
            if (IsPlayerDataEqual(player, playerObjectInLobby)) 
            {
                return getLobbyResult;
            }

            //Update with the details that was attempted to send (Lobby ones may be outdated).
            UpdatePlayerOptions options = new UpdatePlayerOptions
            {
                ConnectionInfo = player.ConnectionInfo,
                Data = player.Data,
                AllocationId = player.AllocationId
            };

            return await UpdatePlayerAsync(lobbyId, player.Id, options);
        }

        // Helper method for determining if a Player object is equal to another.
        // N.B. Player is a generated class which makes overriding Object.Equals a sub-optimal approach.
        private bool IsPlayerDataEqual(Player a, Player b) 
        {
            bool result = (a.Id == b.Id);
            result &= (a.ConnectionInfo == b.ConnectionInfo);
            result &= (a.AllocationId == b.AllocationId);
            result &= (a.Joined == b.Joined);
            result &= (a.LastUpdated == b.LastUpdated);

            var aKeys = a.Data.Keys;
            var bKeys = b.Data.Keys;
            bool areDictKeysEqual = 
                aKeys.All(bKeys.Contains) && aKeys.Count == bKeys.Count;
            result &= areDictKeysEqual;

            //Early exit opeprtunity here before checking individual data
            if (!result) 
            {
                return false;
            }

            foreach (string key in aKeys) 
            {
                var aPlayerDataObject = a.Data[key];
                var bPlayerDataObject = b.Data[key];
                result &= (aPlayerDataObject.Value == bPlayerDataObject.Value);
                result &= (aPlayerDataObject.Visibility == bPlayerDataObject.Visibility);
            }

            return result;
        }
        #endregion
    }
}
