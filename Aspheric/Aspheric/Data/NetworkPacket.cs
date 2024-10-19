using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using enet;

namespace Erinn
{
    /// <summary>
    ///     Network packet
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NetworkPacket
    {
        /// <summary>
        ///     Session
        /// </summary>
        public readonly NetworkSession Session;

        /// <summary>
        ///     Payload
        /// </summary>
        public readonly ENetPacket* Payload;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="session">Session</param>
        /// <param name="payload">Payload</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NetworkPacket(in NetworkSession session, ENetPacket* payload)
        {
            Session = session;
            Payload = payload;
        }
    }
}