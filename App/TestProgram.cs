using System;

namespace Erinn
{
    [RpcService]
    public sealed partial class TestProgram
    {
        [Rpc(RpcAccessibility.Public)]
        private static void Test2(in NetworkPeer peer, in NetworkPacketFlag flags, in string message)
        {
            Console.WriteLine(peer.Session.Id + " " + message);
        }

        public static void Test4(in NetworkPeer peer, in NetworkPacketFlag flags, in DataStream stream)
        {
            var message = stream.Read<string>();
            Console.WriteLine(peer.Session.Id + " " + message);
        }

        [RpcHandler(1)]
        public static void Test5(in NetworkPeer peer, in NetworkPacketFlag flags, in DataStream stream)
        {
            var message = stream.Read<string>();
            Console.WriteLine(peer.Session.Id + " " + message);
        }
    }
}