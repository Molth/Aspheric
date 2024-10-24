using System;

namespace Erinn
{
    [RpcService]
    public sealed partial class TestProgram
    {
        [Rpc(RpcAccessibility.Public)]
        private static void Test2(in NetworkPeer peer, in NetworkPacketFlag flags, in string message)
        {
            Console.WriteLine(message);
        }

        public static void Test4(in NetworkPeer peer, in NetworkPacketFlag flags, in DataStream stream)
        {
            var message = stream.Read<string>();
            Console.WriteLine(message);
        }

        [RpcHandler(1)]
        public static void Test5(in NetworkPeer peer, in NetworkPacketFlag flags, in DataStream stream)
        {
            var message = stream.Read<string>();
            Console.WriteLine(message);
        }
    }
}