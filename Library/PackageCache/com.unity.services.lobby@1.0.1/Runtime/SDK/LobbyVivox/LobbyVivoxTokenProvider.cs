#if UGS_LOBBY_VIVOX
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Authentication.Internal;
using Unity.Services.Vivox.Internal;
using UnityEngine;

namespace Unity.Services.Lobbies.Internal
{
    internal class LobbyVivoxTokenProvider : IVivoxTokenProviderInternal
    {
        private const string k_VivoxJoinTokenKey = "vivoxJoin";

        private ILobbyServiceInternal m_LobbyService;

        private IEnvironmentId m_EnvironmentId;

        public LobbyVivoxTokenProvider(ILobbyServiceInternal lobbyService, IEnvironmentId environmentId)
        {
            m_LobbyService = lobbyService;
            m_EnvironmentId = environmentId;
        }

        public async Task<string> GetTokenAsync(string issuer = null, TimeSpan? expiration = null, string userUri = null, string action = null, string conferenceUri = null, string fromUserUri = null, string realm = null)
        {
            if (conferenceUri == null) {
                throw new InvalidOperationException($"Unable to get Token for null lobbyId[{conferenceUri}]!");
            }

            var environmentId = m_EnvironmentId.EnvironmentId;
            if (environmentId == null)
            {
                throw new InvalidOperationException("Unable to get environmentId! Therefore, we are unable to get the channel name!");
            }
            var lobbyId = ExtractChannelNameFromConferenceUri(conferenceUri, environmentId);

            var response = await m_LobbyService.RequestTokensAsync(lobbyId, Models.TokenRequest.TokenTypeOptions.VivoxJoin);
            if (response == null)
            {
                throw new InvalidOperationException($"{nameof(m_LobbyService.RequestTokensAsync)} response was null!");
            }

            if (!response.TryGetValue(k_VivoxJoinTokenKey, out var tokenData)) {
                var builder = new StringBuilder($"Failed to get Vivox Token for Lobby using key[{k_VivoxJoinTokenKey} Response contained the following tokens:\n");
                foreach (var token in response)
                {
                    builder.Append($"{{ \"{token.Key}\": {{ \"Uri\":\"{token.Value.Uri}\" \"TokenValue\":\"{token.Value.TokenValue}\" }} }}");
                }
                throw new InvalidOperationException(builder.ToString());
            }

            return tokenData.TokenValue;
        }

        internal static string ExtractChannelNameFromConferenceUri(string conferenceUri, string environmentId)
        {
            // conferenceUri is of the format:
            //   sip:confctl-{GetUriDesignator(_type)}-{_issuer}.{_channelName}.{_environmentId}@{_domain}

            environmentId = environmentId.Replace("-", "");

            //   sip:confctl-{GetUriDesignator(_type)}-{_issuer}.{_channelName}.{_environmentId}@{_domain}
            //                                                  ^ start                         ^ domainStart
            var start = conferenceUri.IndexOf('.') + 1;
            var domainStart = conferenceUri.LastIndexOf('@');
            if (start == -1 || domainStart == -1) {
                throw new InvalidOperationException($"Unable to parse channel name for lobby from conferenceUri[{conferenceUri}]! Expected the form: sip:confctl-{{GetUriDesignator(_type)}}-{{_issuer}}.{{_channelName}}.{{_environmentId}}@{{_domain}}");
            }

            //   sip:confctl-{GetUriDesignator(_type)}-{_issuer}.{_channelName_____}.{_environmentId______}@{_domain_______}
            //                                                   [-----------------] [--------------------][---------------]
            //                                                   |channelNameLength| |environmentId.Length||domainLength   |
            //                                                                      [--------------------------------------]
            //                                                                      |trimRightLength                       |
            var domainLength = conferenceUri.Length - domainStart;
            var trimRightLength = environmentId.Length + domainLength + 1;
            var channelNameLength = conferenceUri.Length - (start + trimRightLength);

            if (channelNameLength < 0 || channelNameLength >= conferenceUri.Length - start) {
                throw new InvalidOperationException($"Unable to parse channel name for lobby from conferenceUri[{conferenceUri}]! channelNameLength calculated as[{channelNameLength}], with trimRightLength[{trimRightLength}] and conferenceUri.Length[{conferenceUri.Length}]!");
            }

            // sip:confctl-g-24741-tanks-73903-test.P9Lapiqq2Dguvcx29ZQHUF.96c1f2e66e6342efbb0e070f612bae5c@mtu1xp.vivox.com
            //                                      ^ start              |channelNameLength
            //                                      [--------------------] gives us the channel name!
            var channelName = conferenceUri.Substring(start, channelNameLength);
            return channelName;
        }
    }
}
#endif
