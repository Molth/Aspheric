using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Erinn
{
    public sealed unsafe class Program2
    {
        private static NetworkPeer _peer;
        private static NetworkPeer _peer2;
        private static NetworkHost _host;
        private static NetworkHost _host2;
        private static bool _isConnected;
        private static bool _isConnected2;

        public static void Test()
        {
            var rpcMethods = new RpcMethods(0);
            var onConnected = new NetworkOnConnectedEvent(0);
            var onDisconnected = new NetworkOnDisconnectedEvent(0);
            var onErrored = new NetworkOnErroredEvent(0);
            var onReceived = new NetworkOnReceivedEvent(0);
            onReceived.Add(&TestData);
            _RpcService_App._Initialize(rpcMethods);
            onConnected.Add(&OnConnected);
            rpcMethods.AddCommand(0, TestProgram.Test4);
            rpcMethods.AddCommands();
            var server = new NetworkHost(0, 0, rpcMethods, onConnected, onDisconnected, onErrored, onReceived);
            server.Start(7778, 100);
            var client = new NetworkHost(1, 0, rpcMethods, onConnected, onDisconnected, onErrored, onReceived);
            var client2 = new NetworkHost(2, 0, rpcMethods, onConnected, onDisconnected, onErrored, onReceived);
            _host = client;
            _host2 = client2;
            client.Start(0, 100);
            client2.Start(0, 100);

            Console.CancelKeyPress += (sender, args) =>
            {
                server.Shutdown();
                client.Shutdown();
                client2.Shutdown();
                Thread.Sleep(100);
                server.Dispose();
                client.Dispose();
                client2.Dispose();
            };

            Thread.Sleep(1000);

            client.Connect("127.0.0.1", 7778);
            client2.Connect("127.0.0.1", 7778);

            var i = 0;
            var j = 0;
            DataStream stream = stackalloc byte[1416];
            var buffer = stackalloc byte[1416];
            while (true)
            {
                Thread.Sleep(1000);
                if (j++ % 2 == 0)
                {
                    if (_isConnected)
                        client.Send(_peer, NetworkPacketFlag.Unsequenced, TestProgram.Test2_Rpc_735264182, $"1. this is roslyn test. {i++}");
                    if (_isConnected2)
                    {
                        stream.Write(1);
                        stream.Write($"2. this is attribute test. {i++}");
                        client2.Send(_peer2, NetworkPacketFlag.Reliable, stream);
                        stream.Flush();
                    }
                }
                else
                {
                    if (_isConnected)
                    {
                        stream.Write(0);
                        stream.Write($"3. this is delegate test. {i++}");
                        client.Send(_peer, NetworkPacketFlag.Reliable, stream);
                        stream.Flush();
                    }

                    if (_isConnected2)
                    {
                        var length = Encoding.UTF8.GetBytes($"4. this is raw test. {i++}", MemoryMarshal.CreateSpan(ref *buffer, 1416));
                        stream.WriteBytes(buffer, length);
                        client2.Send(_peer2, NetworkPacketFlag.Reliable, stream);
                        stream.Flush();
                    }
                }
            }
        }

        private static void TestData(in NetworkPeer peer, in NetworkPacketFlag flags, in Span<byte> buffer) => Console.WriteLine(peer.Session.Id + " " + Encoding.UTF8.GetString(buffer));

        private static void OnConnected(in NetworkPeer peer)
        {
            if (peer.Host == _host)
            {
                _peer = peer;
                _isConnected = true;
                Console.WriteLine($"Connected1 {peer.IsConnected()}");
            }
            else if (peer.Host == _host2)
            {
                _peer2 = peer;
                _isConnected2 = true;
                Console.WriteLine("Connected2");
            }
        }
    }
}