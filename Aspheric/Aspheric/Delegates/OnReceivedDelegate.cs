using System;

namespace Erinn
{
    /// <summary>
    ///     On received
    /// </summary>
    public delegate void OnReceivedDelegate(in NetworkPeer peer, in NetworkPacketFlag flags, in Span<byte> buffer);
}