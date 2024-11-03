using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Erinn
{
    public sealed unsafe class Program
    {
        private static NetworkPeer _peer1;
        private static NetworkPeer _peer2;
        private static NetworkPeer _peer3;
        private static NetworkPeer _peer4;
        private static NetworkHost _host1;
        private static NetworkHost _host2;
        private static bool _isConnected1;
        private static bool _isConnected2;

        public static void Main()
        {
            var rpcMethods = new RpcMethods(0);
            var onConnected = new NetworkOnConnectedEvent(0);
            var onDisconnected = new NetworkOnDisconnectedEvent(0);
            var onErrored = new NetworkOnErroredEvent(0);
            var onReceived = new NetworkOnReceivedEvent(0);
            onReceived.Add(&TestData);
            onErrored.Add(OnErrored);
            _RpcService_App._Initialize(rpcMethods);
            onConnected.Add(&OnConnected);
            rpcMethods.AddCommand(0, TestProgram.Test4);
            rpcMethods.AddCommands();
            var server = new NetworkHost(0, 0, rpcMethods, onConnected, onDisconnected, onErrored, onReceived);
            server.Start([7777, 7778], 100);
            var client = new NetworkHost(1, 0, rpcMethods, onConnected, onDisconnected, onErrored, onReceived);
            var client2 = new NetworkHost(2, 0, rpcMethods, onConnected, onDisconnected, onErrored, onReceived);
            _host1 = client;
            _host2 = client2;
            client.Start(0, 100);
            client2.Start(0, 100);

            var running = true;

            Console.CancelKeyPress += (sender, args) =>
            {
                running = false;
                Thread.Sleep(100);
                server.Shutdown();
                client.Shutdown();
                client2.Shutdown();
                Thread.Sleep(100);
                server.Dispose();
                client.Dispose();
                client2.Dispose();
            };

            Thread.Sleep(1000);

            client.Connect("127.0.0.1", 7777);
            client2.Connect("127.0.0.1", 7778);

            var i = 0;
            var j = -1;
            DataStream stream = stackalloc byte[1416];
            var buffer = stackalloc byte[1416];
            while (true)
            {
                j++;
                j %= 3;
                Thread.Sleep(1000);
                if (!running)
                    break;
                if (j == 0)
                {
                    if (_isConnected1)
                    {
                        client.Send(_peer1, NetworkPacketFlag.Reliable, &TestProgram.Test, $"1. this is roslyn test. {i++}");
                        Thread.Sleep(100);
                    }

                    if (_isConnected2)
                    {
                        stream.Write(1);
                        stream.Write($"2. this is attribute test. {i++}");
                        client2.Send(_peer2, NetworkPacketFlag.Reliable, stream);
                        stream.Flush();
                    }
                }
                else if (j == 1)
                {
                    if (_isConnected1)
                    {
                        stream.Write(0);
                        stream.Write($"3. this is delegate test. {i++}");
                        client.Send(_peer1, NetworkPacketFlag.Reliable, stream);
                        stream.Flush();
                        Thread.Sleep(100);
                    }

                    if (_isConnected2)
                    {
                        var length = Encoding.UTF8.GetBytes($"4. this is raw test. {i++}", MemoryMarshal.CreateSpan(ref *buffer, 1416));
                        stream.WriteBytes(buffer, length);
                        client2.Send(_peer2, NetworkPacketFlag.Reliable, stream);
                        stream.Flush();
                    }
                }
                else
                {
                    if (_isConnected1)
                    {
                        stream.Write(0);
                        stream.Write($"5. this is dedicated delegate test. {i++}");
                        server.Send(_peer3, NetworkPacketFlag.Reliable, stream);
                        stream.Flush();
                        Thread.Sleep(100);
                    }

                    if (_isConnected2)
                    {
                        var length = Encoding.UTF8.GetBytes($"6. this is dedicated raw test. {i++}", MemoryMarshal.CreateSpan(ref *buffer, 1416));
                        stream.WriteBytes(buffer, length);
                        server.Send(_peer4, NetworkPacketFlag.Reliable, stream);
                        stream.Flush();
                    }
                }
            }
        }

        [OnErrored]
        private static void OnErrored(in NetworkPeer peer, in NetworkPacketFlag flags, in Span<byte> buffer, in Exception e)
        {
        }

        private static void TestData(in NetworkPeer peer, in NetworkPacketFlag flags, in Span<byte> buffer) => Console.WriteLine(Encoding.UTF8.GetString(buffer));

        [OnConnected]
        private static void OnConnected(in NetworkPeer peer)
        {
            if (peer.Host == _host1)
            {
                _peer1 = peer;
                _isConnected1 = true;
                Console.WriteLine($"Connected1 {peer.IsConnected()}");
            }
            else if (peer.Host == _host2)
            {
                _peer2 = peer;
                _isConnected2 = true;
                Console.WriteLine("Connected2");
            }
            else
            {
                if (peer.Session.Id == 0)
                    _peer3 = peer;
                else
                    _peer4 = peer;
            }
        }
    }
}