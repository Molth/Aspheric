namespace Erinn
{
    public sealed partial class TestProgram1000
    {
        [Rpc]
        public static void Test5(NetworkPeer peer, uint flags, string message)
        {
        }

        [Rpc]
        public static void Test3(in NetworkPeer peer, in NetworkPacketFlag flags, in string message)
        {
        }

        [Rpc]
        public static void Test3(NetworkPeer peer, uint flags, string message)
        {
        }
    }
}