using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using enet;

namespace Erinn
{
    /// <summary>
    ///     Network event
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NetworkEvent
    {
        /// <summary>
        ///     Event type
        /// </summary>
        public readonly NetworkEventType EventType;

        /// <summary>
        ///     Session
        /// </summary>
        public readonly NetworkSession Session;

        /// <summary>
        ///     Address
        /// </summary>
        public readonly ENetAddress Address;

        /// <summary>
        ///     Payload
        /// </summary>
        public readonly ENetPacket* Payload;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="session">Session</param>
        /// <param name="address">Address</param>
        /// <param name="payload">Payload</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NetworkEvent(NetworkEventType eventType, in NetworkSession session, in ENetAddress address, ENetPacket* payload = null)
        {
            EventType = eventType;
            Session = session;
            Address = address;
            Payload = payload;
        }
    }
}