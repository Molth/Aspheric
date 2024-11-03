using System;

namespace Erinn
{
    [RpcService(RpcServiceTarget.All | RpcServiceTarget.Internal)]
    public sealed partial class TestProgram
    {
        [Rpc(RpcAccessibility.Public)]
        public static void Test(in NetworkPeer peer, in NetworkPacketFlag flags, in string message)
        {
            Console.WriteLine(message);
        }

        [Rpc(RpcAccessibility.Public)]
        private static void Test2(in NetworkPeer peer, in NetworkPacketFlag flags, in string message)
        {
            Console.WriteLine(message);
        }

        [RpcManual(1)]
        public static void Test4(in NetworkPeer peer, in NetworkPacketFlag flags, in DataStream stream)
        {
            var message = stream.Read<string>();
            Console.WriteLine(message);
        }

        [RpcManual(100)]
        public static extern void Test5(in NetworkPeer peer, in NetworkPacketFlag flags, in DataStream stream);

        [Rpc(RpcAccessibility.Public)]
        private static void Test6(in NetworkPeer peer, in NetworkPacketFlag flags, in string message)
        {
            Console.WriteLine(message);
        }

        [Rpc(RpcAccessibility.Public)]
        private static void Test7(in NetworkPeer peer, in NetworkPacketFlag flags, in string message)
        {
            Console.WriteLine(message);
        }

        [Rpc(RpcAccessibility.Public)]
        private static void Test8(in NetworkPeer peer, in NetworkPacketFlag flags, in string message)
        {
            Console.WriteLine(message);
        }

        [OnErrored]
        private static void OnErrored(in NetworkPeer peer, in NetworkPacketFlag flags, in Span<byte> buffer, in Exception e)
        {
        }

        [OnConnected]
        private static void OnConnected(in NetworkPeer peer)
        {
        }

        [OnConnected]
        private static void OnConnected2(in NetworkPeer peer)
        {
        }

        [OnConnected]
        private static void OnConnected4(in NetworkPeer peer)
        {
        }

        [OnDisconnected]
        private static void OnDisconnected(in NetworkPeer peer)
        {
        }

        [OnReceived]
        private static void TestData(in NetworkPeer peer, in NetworkPacketFlag flags, in Span<byte> buffer)
        {
        }

        [OnReceived]
        private static void TestData2(in NetworkPeer peer, in NetworkPacketFlag flags, in Span<byte> buffer)
        {
        }

        [OnReceived]
        private static void TestData3(in NetworkPeer peer, in NetworkPacketFlag flags, in Span<byte> buffer)
        {
        }
    }
}