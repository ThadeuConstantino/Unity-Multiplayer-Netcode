#if UGS_BETA_LOBBY_EVENTS && UGS_LOBBY_EVENTS
using Unity.Services.Lobbies;

namespace Unity.Services.Lobbies
{
    /// <summary>
    /// Helper class for instantiating ChangedLobbyValue and ChangedOrRemovedLobbyValue!
    /// </summary>
    public static class LobbyValue
    {
        /// <summary>
        /// Instantiates a changed lobby value.
        /// </summary>
        /// <typeparam name="T">The type of the value to change.</typeparam>
        /// <param name="value">The new value it has changed to.</param>
        /// <returns>A changed lobby value.</returns>
        public static ChangedLobbyValue<T> Changed<T>(T value) => new ChangedLobbyValue<T>(value);

        /// <summary>
        /// Instantiates a changed lobby value.
        /// </summary>
        /// <typeparam name="T">The type of the value to change.</typeparam>
        /// <param name="value">The new value it has changed to.</param>
        /// <returns>A changed lobby value.</returns>
        public static ChangedOrRemovedLobbyValue<T> ChangedNotRemoved<T>(T value) => new ChangedOrRemovedLobbyValue<T>(value, LobbyValueChangeType.Changed);

        /// <summary>
        /// Provides a removed lobby value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ChangedOrRemovedLobbyValue<T> Removed<T>() => ChangedOrRemovedLobbyValue<T>.RemoveThisValue;
    }
}
#endif
