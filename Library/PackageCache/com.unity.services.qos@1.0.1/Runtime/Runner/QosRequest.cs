using System;
#if UGS_QOS_SUPPORTED
using Unity.Baselib.LowLevel;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Unity.Networking.QoS
{
    internal class QosRequest
    {
        private const int MinPacketLen = 15; // A packet with a 1 byte title is the bare minimum
        private const int MaxPacketLen = 1500; // Enough data to force packet fragmentation if you really want
        private const byte RequestMagic = 0x59;
        private const int ConstructedPacketLen = 14; // Packet length at object construction (contains no title)

        // Not making these internal properties because we need to be able to take the address of them in Send()
        private byte m_Magic = RequestMagic;
        private byte m_VerAndFlow = 0x00;
        private byte m_TitleLen = 0x00;
        private byte[] m_Title = null;
        private byte m_Sequence = 0x00;
        private ushort m_Identifier = 0;
        private ulong m_Timestamp = 0;

        private ushort m_PacketLength = MinPacketLen - 1; // Still need a title

        // The internal properties are backed by the private data above
        internal byte Magic => m_Magic;

        internal byte Version =>
            (byte)((m_VerAndFlow >> 4) & 0x0F); // Won't implement set until there is a reason to do so

        internal byte FlowControl => (byte)(m_VerAndFlow & 0x0F); // Never need to set flow control on the client

        internal byte[] Title
        {
            // Title is odd because we get and set it as an array of bytes.  But it should be a UTF8-encoded string in
            // the guise of an array of bytes.
            get => m_Title;

            set
            {
                if (MinPacketLen + value.Length > MaxPacketLen)
                {
                    throw new ArgumentException(
                        $"Encoded title would make the QosPacket have size {MinPacketLen + value.Length}. Max size is {MaxPacketLen}.");
                }

                m_Title = value;
                m_TitleLen = (byte)(m_Title.Length + 1); // Title length includes the size byte
                m_PacketLength =
                    (ushort)(ConstructedPacketLen + m_Title.Length); // +1 is already included in the MinPacketLen
            }
        }

        internal byte Sequence
        {
            get => m_Sequence;
            set => m_Sequence = value;
        }

        internal ushort Identifier
        {
            get => m_Identifier;
            set => m_Identifier = value;
        }

        internal ulong Timestamp
        {
            get => m_Timestamp;
            set => m_Timestamp = value;
        }

        internal int Length => m_PacketLength; // Can't externally set the packet length

        /// <summary>
        /// Send the QosRequest packet to the given endpoint
        /// </summary>
        /// <param name="socketHandle">Native socket descriptor</param>
        /// <param name="endPoint">Remote endpoint to send the request</param>
        /// <param name="expireTimeUtc">When to stop trying to send</param>
        /// <returns>
        /// (sent, errorcode) where sent is the number of bytes sent and errorcode is the error code if Send fails.
        /// 0 means no error.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if no title is set on the request.</exception>
        internal unsafe (uint, int) Send(IntPtr socketHandle, NetworkEndPoint endPoint, DateTime expireTimeUtc)
        {
#if UGS_QOS_SUPPORTED
            if (Title == null)
            {
                // A title will guarantee the packet length is sufficient
                throw new InvalidOperationException("QosRequest requires a title.");
            }

            // No byte swizzling here since the required fields are one byte (or arrays of bytes), and the
            // custom data is reflected back to us in the same format it was sent.

            // The maximum packet size is 1500, we want to make sure we reserve enough space for the buffer.
            // Calculate the next nearest power of two for the buffer capacity
            var bufferCapacity = (int)Math.Pow(2, (int)Math.Log(m_PacketLength - 1, 2) + 1);
            var buffer = new UnsafeAppendBuffer(bufferCapacity, 16, Allocator.TempJob);
            buffer.Add(m_Magic);
            buffer.Add(m_VerAndFlow);
            buffer.Add(m_TitleLen);
            for (var i = 0; i < m_TitleLen - 1; i++)
            {
                buffer.Add(m_Title[i]);
            }
            buffer.Add(m_Sequence);
            buffer.Add(m_Identifier);
            buffer.Add(m_Timestamp);

            var dataLen = (uint)buffer.Length;

            var message = default(Binding.Baselib_Socket_Message);
            message.address = &endPoint.rawNetworkAddress;
            message.data = new IntPtr(buffer.Ptr);
            message.dataLen = dataLen;

            var errorState = default(Binding.Baselib_ErrorState);
            var socket = new Binding.Baselib_Socket_Handle {handle = socketHandle};
            uint sent = 0;

            do
            {
                sent = Binding.Baselib_Socket_UDP_Send(socket, &message, 1, &errorState);
            }
            while (sent == 0 && QosHelper.WouldBlock(errorState.nativeErrorCode) && !QosHelper.ExpiredUtc(expireTimeUtc));
            // Free memory
            buffer.Dispose();
            return ((uint)Length, (int)errorState.code);
#else
            throw new NotImplementedException();
#endif
        }
    }
}
