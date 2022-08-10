#if UGS_BETA_LOBBY_EVENTS && UGS_LOBBY_EVENTS
namespace Unity.Services.Lobbies
{
    /// <summary>
    /// Contains whether or not a particular change has occurred, and if it has, the value of the change.
    /// </summary>
    /// <typeparam name="T">The type of the value of the change.</typeparam>
    public struct ChangedLobbyValue<T>
    {
        /// <summary>
        /// The new value provided by the change.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// True if a change has occurred, false if there has been no change.
        /// </summary>
        public bool Changed { get; }

        /// <summary>
        /// Creates a changed value.
        /// </summary>
        /// <param name="value">The new value provided by the change.</param>
        public ChangedLobbyValue(T value)
        {
            Value = value;
            Changed = true;
        }
    }
}
#endif
