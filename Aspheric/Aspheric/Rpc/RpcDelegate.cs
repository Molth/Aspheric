namespace Erinn
{
    /// <summary>
    ///     Rpc delegate
    /// </summary>
    public delegate void RpcDelegate(in NetworkPeer peer, in NetworkPacketFlag flags, in DataStream stream);
}