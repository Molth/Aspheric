namespace Erinn
{
    [RpcService(RpcServiceTarget.Rpc)]
    public sealed partial class TestProgram1000
    {
        [Rpc]
        public static void Test100(in NetworkPeer peer, in NetworkPacketFlag flags, in string message)
        {
        }
    }
}