using System;

namespace Erinn
{
    /// <summary>
    ///     On errored
    /// </summary>
    public delegate void OnErroredDelegate(in NetworkPeer peer, in NetworkPacketFlag flags, in Span<byte> buffer, in Exception e);
}