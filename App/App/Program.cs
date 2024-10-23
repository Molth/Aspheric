using System;
using System.Collections.Immutable;
using System.Threading;

namespace Erinn
{
    public sealed unsafe class Program
    {
        private static void Main() => Test();

        private static void Test()
        {
            var rpcMethods = new RpcMethods(_RpcService_App.RPC_METHOD_COUNT);
            var onConnected = new NetworkOnConnectedEvent(0);
            var onConnected2 = new NetworkOnConnectedEvent(0);
            var onDisconnected = new NetworkOnDisconnectedEvent(0);
            var onErrored = new NetworkOnErroredEvent(0);
            var onReceived = new NetworkOnReceivedEvent(0);

            onConnected2.Add(&OnConnected);

            var dedicatedServer = new NetworkHost(0, 0, rpcMethods, onConnected2, onDisconnected, onErrored, onReceived);
            var ports = ImmutableArray.Create<ushort>(7777, 7778);

            var client0 = new NetworkHost(0, 0, rpcMethods, onConnected, onDisconnected, onErrored, onReceived);
            var client1 = new NetworkHost(0, 0, rpcMethods, onConnected, onDisconnected, onErrored, onReceived);

            Console.CancelKeyPress += (sender, args) =>
            {
                dedicatedServer.Dispose();
                client0.Dispose();
                client1.Dispose();
            };

            dedicatedServer.Start(ports, 2);
            client0.Start(0, 1);
            client1.Start(0, 1);

            Thread.Sleep(1000);

            client0.Connect("127.0.0.1", 7777);
            client1.Connect("127.0.0.1", 7778);

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        private static void OnConnected(in NetworkPeer peer)
        {
            Console.WriteLine(peer.Session.Id);
        }
    }
}