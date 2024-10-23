using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erinn
{
    /// <summary>
    ///     Network host options
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct NetworkHostOptions
    {
        /// <summary>
        ///     Peer count
        /// </summary>
        public readonly ushort PeerCount;

        /// <summary>
        ///     Ping interval
        /// </summary>
        public readonly uint PingInterval;

        /// <summary>
        ///     Timeout
        /// </summary>
        public readonly uint Timeout;

        /// <summary>
        ///     Incoming bandwidth
        /// </summary>
        public readonly uint IncomingBandwidth;

        /// <summary>
        ///     Outgoing bandwidth
        /// </summary>
        public readonly uint OutgoingBandwidth;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="peerCount">Peer count</param>
        /// <param name="pingInterval">Ping interval</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="incomingBandwidth">Incoming bandwidth</param>
        /// <param name="outgoingBandwidth">Outgoing bandwidth</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NetworkHostOptions(ushort peerCount, uint pingInterval, uint timeout, uint incomingBandwidth, uint outgoingBandwidth)
        {
            PeerCount = peerCount;
            PingInterval = pingInterval;
            Timeout = timeout;
            IncomingBandwidth = incomingBandwidth;
            OutgoingBandwidth = outgoingBandwidth;
        }
    }
}