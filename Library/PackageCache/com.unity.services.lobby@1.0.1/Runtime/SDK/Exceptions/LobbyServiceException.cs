using System;
using Unity.Services.Core;

namespace Unity.Services.Lobbies
{
    /// <summary>
    /// Represents an exception that occurs when communicating with the Unity Lobby Service.
    /// </summary>
    public class LobbyServiceException : RequestFailedException
    {
        /// <summary>
        /// The reason of the exception.
        /// </summary>
        public LobbyExceptionReason Reason { get; private set; }

        /// <summary>
        /// Creates a LobbyServiceException.
        /// </summary>
        /// <param name="reason">The error code or the HTTP Status returned by the service.</param>
        /// <param name="message">The description of the exception.</param>
        /// <param name="innerException">The exception raised by the service, if any.</param>
        public LobbyServiceException(LobbyExceptionReason reason, string message, Exception innerException) : base((int)reason, message, innerException)
        {
            Reason = reason;
        }

        /// <summary>
        /// Creates a LobbyServiceException.
        /// </summary>
        /// <param name="reason">The error code or the HTTP Status returned by the service.</param>
        /// <param name="message">The description of the exception.</param>
        public LobbyServiceException(LobbyExceptionReason reason, string message) : base((int)reason, message)
        {
            Reason = reason;
        }

        /// <summary>
        /// Creates a LobbyServiceException.
        /// </summary>
        /// <param name="errorCode">The error code or the HTTP Status returned by the service.</param>
        /// <param name="message">The description of the exception.</param>
        public LobbyServiceException(long errorCode, string message) : base((int)errorCode, message)
        {
            if (Enum.IsDefined(typeof(LobbyExceptionReason), errorCode))
            {
                Reason = (LobbyExceptionReason)errorCode;
            }
            else
            {
                Reason = LobbyExceptionReason.Unknown;
            }
        }

        /// <summary>
        /// Creates a RelayServiceException.
        /// </summary>
        /// <param name="innerException">The exception raised by the service, if any.</param>
        public LobbyServiceException(Exception innerException) : base((int)LobbyExceptionReason.Unknown, "Unknown Lobby Service Exception", innerException)
        {
        }
    }
}