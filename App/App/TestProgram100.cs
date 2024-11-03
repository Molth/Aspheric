namespace Erinn
{
    [RpcService]
    public sealed partial class TestProgram100
    {
        [OnConnected]
        private static void OnConnected(in NetworkPeer peer)
        {
        }

        [Rpc]
        public static void Test(in NetworkPeer peer, in NetworkPacketFlag flags, in string message)
        {
        }

        [Rpc]
        public static void Test2(in NetworkPeer peer, in NetworkPacketFlag flags, in string message)
        {
        }

        [Rpc]
        public static void Test4(in NetworkPeer peer, in NetworkPacketFlag flags, in string message)
        {
        }
    }
}