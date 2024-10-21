using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using enet;
using static enet.ENet;

#pragma warning disable CS8605

// ReSharper disable LoopVariableIsNeverChangedInsideLoop

namespace Erinn
{
    /// <summary>
    ///     Network host
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NetworkHost : IDisposable, IEquatable<NetworkHost>
    {
        /// <summary>
        ///     Handle
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NetworkHostHandle
        {
            /// <summary>
            ///     Rpc methods
            /// </summary>
            public RpcMethods RpcMethods;

            /// <summary>
            ///     On connected
            /// </summary>
            public NetworkOnConnectedEvent OnConnected;

            /// <summary>
            ///     On disconnected
            /// </summary>
            public NetworkOnDisconnectedEvent OnDisconnected;

            /// <summary>
            ///     On error
            /// </summary>
            public NetworkOnErrorEvent OnError;

            /// <summary>
            ///     On received
            /// </summary>
            public NetworkOnReceivedEvent OnReceived;

            /// <summary>
            ///     Tokens
            /// </summary>
            public NativeArray<Guid> Tokens;

            /// <summary>
            ///     Commands
            /// </summary>
            public NativeConcurrentQueue<NetworkCommand> Commands;

            /// <summary>
            ///     Outgoings
            /// </summary>
            public NativeConcurrentQueue<NetworkPacket> Outgoings;

            /// <summary>
            ///     State
            /// </summary>
            public int State;

            /// <summary>
            ///     Threads
            /// </summary>
            public int Threads;

            /// <summary>
            ///     Options
            /// </summary>
            public NetworkHostOptions Options;
        }

        /// <summary>
        ///     Handle
        /// </summary>
        private readonly NetworkHostHandle* _handle;

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _handle != null;

        /// <summary>
        ///     State
        /// </summary>
        public NetworkHostState State
        {
            get
            {
                var handle = _handle;
                return handle->State switch
                {
                    -1 => handle->Threads == 0 ? NetworkHostState.Disposed : NetworkHostState.Disposing,
                    1 => handle->Threads == 0 ? NetworkHostState.Starting : NetworkHostState.Started,
                    _ => handle->Threads == 0 ? NetworkHostState.None : NetworkHostState.Shutting
                };
            }
        }

        /// <summary>
        ///     Rpc methods
        /// </summary>
        public RpcMethods RpcMethods
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var handle = _handle;
                if (handle->Threads != 0)
                    throw new InvalidOperationException("ConcurrentOperationsNotSupported");
                return handle->RpcMethods;
            }
        }

        /// <summary>
        ///     On connected
        /// </summary>
        public NetworkOnConnectedEvent OnConnected
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var handle = _handle;
                if (handle->Threads != 0)
                    throw new InvalidOperationException("ConcurrentOperationsNotSupported");
                return handle->OnConnected;
            }
        }

        /// <summary>
        ///     On disconnected
        /// </summary>
        public NetworkOnDisconnectedEvent OnDisconnected
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var handle = _handle;
                if (handle->Threads != 0)
                    throw new InvalidOperationException("ConcurrentOperationsNotSupported");
                return handle->OnDisconnected;
            }
        }

        /// <summary>
        ///     On error
        /// </summary>
        public NetworkOnErrorEvent OnError
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var handle = _handle;
                if (handle->Threads != 0)
                    throw new InvalidOperationException("ConcurrentOperationsNotSupported");
                return handle->OnError;
            }
        }

        /// <summary>
        ///     On received
        /// </summary>
        public NetworkOnReceivedEvent OnReceived
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var handle = _handle;
                if (handle->Threads != 0)
                    throw new InvalidOperationException("ConcurrentOperationsNotSupported");
                return handle->OnReceived;
            }
        }

        /// <summary>
        ///     Options
        /// </summary>
        public NetworkHostOptions Options
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle->Options;
        }

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="rpcMethods">Rpc methods</param>
        /// <param name="onConnected">On connected</param>
        /// <param name="onDisconnected">On disconnected</param>
        /// <param name="onError">On error</param>
        /// <param name="onReceived">On received</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NetworkHost(RpcMethods rpcMethods, NetworkOnConnectedEvent onConnected, NetworkOnDisconnectedEvent onDisconnected, NetworkOnErrorEvent onError, NetworkOnReceivedEvent onReceived)
        {
            var handle = (NetworkHostHandle*)NativeMemoryAllocator.AllocZeroed((uint)sizeof(NetworkHostHandle));
            handle->RpcMethods = rpcMethods;
            handle->OnConnected = onConnected;
            handle->OnDisconnected = onDisconnected;
            handle->OnError = onError;
            handle->OnReceived = onReceived;
            _handle = handle;
        }

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NetworkHost other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NetworkHost networkHost && networkHost == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => ((nint)_handle).GetHashCode();

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => $"NetworkHost[{GetHashCode()}]";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NetworkHost left, NetworkHost right) => left._handle == right._handle;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NetworkHost left, NetworkHost right) => left._handle != right._handle;

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            var handle = _handle;
            if (handle == null || Interlocked.CompareExchange(ref handle->State, -1, handle->State) == -1)
                return;
            var spinCount = 0;
            while (handle->Threads != 0)
            {
                if ((spinCount >= 10 && (spinCount - 10) % 2 == 0) || Environment.ProcessorCount == 1)
                {
                    var yieldsSoFar = spinCount >= 10 ? (spinCount - 10) / 2 : spinCount;
                    if (yieldsSoFar % 5 == 4)
                        Thread.Sleep(0);
                    else
                        Thread.Yield();
                }
                else
                {
                    var iterations = Environment.ProcessorCount / 2;
                    if (spinCount <= 30 && 1 << spinCount < iterations)
                        iterations = 1 << spinCount;
                    Thread.SpinWait(iterations);
                }

                spinCount = spinCount == int.MaxValue ? 10 : spinCount + 1;
            }

            NativeMemoryAllocator.Free(handle);
        }

        /// <summary>
        ///     Start
        /// </summary>
        /// <param name="port">Port</param>
        /// <param name="peerCount">Peer count</param>
        /// <param name="pingInterval">Ping interval</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="incomingBandwidth">Incoming bandwidth</param>
        /// <param name="outgoingBandwidth">Outgoing bandwidth</param>
        /// <returns>Started</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SocketError Start(ushort port, ushort peerCount, uint pingInterval = 500, uint timeout = 5000, uint incomingBandwidth = 0, uint outgoingBandwidth = 0)
        {
            var handle = _handle;
            if (handle->Threads != 0 || Interlocked.CompareExchange(ref handle->State, 1, 0) != 0)
                return SocketError.InProgress;
            if (enet_initialize() != 0)
            {
                Interlocked.Exchange(ref handle->State, 0);
                return SocketError.NotInitialized;
            }

            ENetAddress address;
            _ = enet_set_ip(&address, Socket.OSSupportsIPv6 ? "::0" : "0.0.0.0");
            address.port = port;
            var host = enet_host_create(&address, peerCount, 2, incomingBandwidth, outgoingBandwidth);
            if (host == null)
            {
                enet_deinitialize();
                Interlocked.Exchange(ref handle->State, 0);
                return SocketError.AddressAlreadyInUse;
            }

            handle->Options = new NetworkHostOptions(port, peerCount, pingInterval, timeout, incomingBandwidth, outgoingBandwidth);
            var thread = new Thread(Service) { IsBackground = true };
            thread.Start((nint)host);
            return SocketError.Success;
        }

        /// <summary>
        ///     Service
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Service(object? obj)
        {
            var handle = _handle;
            var host = (ENetHost*)(nint)obj;
            host->maximumPacketSize = 1392;
            var options = handle->Options;
            Interlocked.Exchange(ref handle->Threads, 2);
            handle->Tokens = new NativeArray<Guid>(options.PeerCount, true);
            handle->Commands = new NativeConcurrentQueue<NetworkCommand>(1, 0);
            handle->Outgoings = new NativeConcurrentQueue<NetworkPacket>(1, 2);
            var @event = new ENetEvent();
            var spinCount = 0;
            var incomings = new NativeConcurrentQueue<NetworkEvent>(1, 2);
            var thread = new Thread(CheckEvents) { IsBackground = true };
            thread.Start(incomings);
            while (handle->State == 1)
            {
                while (handle->Commands.TryDequeue(out var command))
                {
                    switch (command.CommandType)
                    {
                        case NetworkCommandType.None:
                            continue;
                        case NetworkCommandType.Connect:
                            var peer = enet_host_connect(host, &command.Address, 2, 0);
                            if (peer == null)
                            {
                                incomings.Enqueue(new NetworkEvent(NetworkEventType.Disconnect, new NetworkSession(), command.Address));
                                continue;
                            }

                            enet_peer_ping_interval(peer, options.PingInterval);
                            enet_peer_timeout(peer, 0, options.Timeout, options.Timeout);
                            continue;
                        case NetworkCommandType.Disconnect:
                            var session = command.Session;
                            var id = session.Id;
                            if (session.Token != handle->Tokens[id])
                                continue;
                            enet_peer_disconnect(&host->peers[id], 0);
                            continue;
                        case NetworkCommandType.Ping:
                            enet_host_ping(host, &command.Address);
                            continue;
                        default:
                            continue;
                    }
                }

                while (handle->Outgoings.TryDequeue(out var outgoing))
                {
                    var id = outgoing.Session.Id;
                    var packet = outgoing.Payload;
                    if (outgoing.Session.Token == handle->Tokens[id])
                    {
                        var channel = (packet->flags & (uint)ENetPacketFlag.ENET_PACKET_FLAG_RELIABLE) != 0 ? (byte)0 : (byte)1;
                        if (enet_peer_send(&host->peers[id], channel, packet) == 0)
                            continue;
                    }

                    enet_packet_destroy(packet);
                }

                while (enet_host_service(host, &@event, 0) > 0)
                {
                    var peer = @event.peer;
                    var id = peer->incomingPeerID;
                    switch (@event.type)
                    {
                        case ENetEventType.ENET_EVENT_TYPE_NONE:
                            continue;
                        case ENetEventType.ENET_EVENT_TYPE_CONNECT:
                            enet_peer_ping_interval(peer, options.PingInterval);
                            enet_peer_timeout(peer, 0, options.Timeout, options.Timeout);
                            ref var token = ref handle->Tokens[id];
                            token = Guid.NewGuid();
                            incomings.Enqueue(new NetworkEvent(NetworkEventType.Connect, new NetworkSession(id, token), peer->address));
                            continue;
                        case ENetEventType.ENET_EVENT_TYPE_DISCONNECT:
                            incomings.Enqueue(new NetworkEvent(NetworkEventType.Disconnect, new NetworkSession(id, handle->Tokens[id]), peer->address));
                            continue;
                        case ENetEventType.ENET_EVENT_TYPE_RECEIVE:
                            incomings.Enqueue(new NetworkEvent(NetworkEventType.Receive, new NetworkSession(id, handle->Tokens[id]), peer->address, @event.packet));
                            continue;
                        default:
                            continue;
                    }
                }

                if ((spinCount >= 10 && (spinCount - 10) % 2 == 0) || Environment.ProcessorCount == 1)
                {
                    var yieldsSoFar = spinCount >= 10 ? (spinCount - 10) / 2 : spinCount;
                    if (yieldsSoFar % 5 == 4)
                        Thread.Sleep(0);
                    else
                        Thread.Yield();
                }
                else
                {
                    var iterations = Environment.ProcessorCount / 2;
                    if (spinCount <= 30 && 1 << spinCount < iterations)
                        iterations = 1 << spinCount;
                    Thread.SpinWait(iterations);
                }

                spinCount = spinCount == int.MaxValue ? 10 : spinCount + 1;
            }

            for (var i = 0; i < options.PeerCount; ++i)
                enet_peer_disconnect_now(&host->peers[i], 0);
            enet_host_flush(host);
            enet_host_destroy(host);
            handle->Tokens.Dispose();
            handle->Commands.Dispose();
            while (handle->Outgoings.TryDequeue(out var outgoing))
                enet_packet_destroy(outgoing.Payload);
            handle->Outgoings.Dispose();
            enet_deinitialize();
            Interlocked.Decrement(ref handle->Threads);
        }

        /// <summary>
        ///     Check events
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckEvents(object? obj)
        {
            var handle = _handle;
            var incomings = (NativeConcurrentQueue<NetworkEvent>)obj;
            var buffer = stackalloc byte[24];
            var stream = new DataStream(buffer, 24);
            var spinCount = 0;
            while (handle->State == 1)
            {
                while (incomings.TryDequeue(out var incoming))
                {
                    var peer = new NetworkPeer(this, incoming.Session, incoming.Address);
                    switch (incoming.EventType)
                    {
                        case NetworkEventType.None:
                            continue;
                        case NetworkEventType.Connect:
                            handle->OnConnected.Invoke(peer);
                            continue;
                        case NetworkEventType.Disconnect:
                            handle->OnDisconnected.Invoke(peer);
                            continue;
                        case NetworkEventType.Receive:
                            var packet = incoming.Payload;
                            var flags = (NetworkPacketFlag)packet->flags;
                            try
                            {
                                if (packet->dataLength < 4)
                                    goto label;
                                var command = Unsafe.ReadUnaligned<uint>(packet->data);
                                if (handle->RpcMethods.TryGetAddress(command, out var address))
                                {
                                    try
                                    {
                                        stream.SetBuffer(new NativeSlice<byte>(packet->data + 4, (int)packet->dataLength - 4));
                                        address(peer, flags, stream);
                                    }
                                    catch (Exception e)
                                    {
                                        handle->OnError.Invoke(peer, flags, packet->data, (int)packet->dataLength, e);
                                    }
                                }
                                else
                                {
                                    goto label;
                                }

                                continue;
                                label:
                                try
                                {
                                    handle->OnReceived.Invoke(peer, flags, packet->data, (int)packet->dataLength);
                                }
                                catch (Exception e)
                                {
                                    handle->OnError.Invoke(peer, flags, packet->data, (int)packet->dataLength, e);
                                }
                            }
                            finally
                            {
                                enet_packet_destroy(packet);
                            }

                            continue;
                        default:
                            continue;
                    }
                }

                if ((spinCount >= 10 && (spinCount - 10) % 2 == 0) || Environment.ProcessorCount == 1)
                {
                    var yieldsSoFar = spinCount >= 10 ? (spinCount - 10) / 2 : spinCount;
                    if (yieldsSoFar % 5 == 4)
                        Thread.Sleep(0);
                    else
                        Thread.Yield();
                }
                else
                {
                    var iterations = Environment.ProcessorCount / 2;
                    if (spinCount <= 30 && 1 << spinCount < iterations)
                        iterations = 1 << spinCount;
                    Thread.SpinWait(iterations);
                }

                spinCount = spinCount == int.MaxValue ? 10 : spinCount + 1;
            }

            while (incomings.TryDequeue(out var incoming))
                enet_packet_destroy(incoming.Payload);
            incomings.Dispose();
            Interlocked.Decrement(ref handle->Threads);
        }

        /// <summary>
        ///     Connect
        /// </summary>
        /// <param name="ip">IP</param>
        /// <param name="port">Port</param>
        /// <returns>Started</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Connect(string ip, ushort port)
        {
            var handle = _handle;
            if (handle->State != 1)
                return false;
            ENetAddress address;
            _ = enet_set_ip(&address, ip);
            address.port = port;
            handle->Commands.Enqueue(new NetworkCommand
            {
                CommandType = NetworkCommandType.Connect,
                Address = address
            });
            return true;
        }

        /// <summary>
        ///     Ping
        /// </summary>
        /// <param name="ip">IP</param>
        /// <param name="port">Port</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Ping(string ip, ushort port)
        {
            ENetAddress address;
            enet_set_ip(&address, ip);
            address.port = port;
            Ping(address);
        }

        /// <summary>
        ///     Ping
        /// </summary>
        /// <param name="address">Address</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Ping(in ENetAddress address)
        {
            _handle->Commands.Enqueue(new NetworkCommand
            {
                CommandType = NetworkCommandType.Ping,
                Address = address
            });
        }

        /// <summary>
        ///     Shutdown
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Shutdown() => Interlocked.CompareExchange(ref _handle->State, 0, 2) == 1;

        /// <summary>
        ///     Check is connected
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsConnected(in NetworkPeer peer)
        {
            var session = peer.Session;
            return session.Token != Guid.Empty && session.Token == _handle->Tokens[session.Id];
        }

        /// <summary>
        ///     Disconnect
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Disconnect(in NetworkPeer peer)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            handle->Commands.Enqueue(new NetworkCommand
            {
                CommandType = NetworkCommandType.Disconnect,
                Session = session
            });
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send(in NetworkPeer peer, in NetworkPacketFlag flags, byte* buffer, int length)
        {
            if (length > 1392)
                return false;
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            var packet = enet_packet_create(buffer, length, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send(in NetworkPeer peer, in NetworkPacketFlag flags, in DataStream stream)
        {
            if (stream.BytesWritten > 1392)
                return false;
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            var packet = enet_packet_create(stream.Bytes, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, void> address)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var packet = enet_packet_create(&command, 4, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, void> address, in T0 arg0)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, void> address, in T0 arg0, in T1 arg1)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, void> address, in T0 arg0, in T1 arg1, in T2 arg2)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            stream.Write(arg9);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            stream.Write(arg9);
            stream.Write(arg10);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10, in T11 arg11)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            stream.Write(arg9);
            stream.Write(arg10);
            stream.Write(arg11);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10, in T11 arg11, in T12 arg12)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            stream.Write(arg9);
            stream.Write(arg10);
            stream.Write(arg11);
            stream.Write(arg12);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10, in T11 arg11, in T12 arg12, in T13 arg13)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            stream.Write(arg9);
            stream.Write(arg10);
            stream.Write(arg11);
            stream.Write(arg12);
            stream.Write(arg13);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10, in T11 arg11, in T12 arg12, in T13 arg13, in T14 arg14)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            stream.Write(arg9);
            stream.Write(arg10);
            stream.Write(arg11);
            stream.Write(arg12);
            stream.Write(arg13);
            stream.Write(arg14);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(in NetworkPeer peer, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10, in T11 arg11, in T12 arg12, in T13 arg13, in T14 arg14, in T15 arg15)
        {
            var handle = _handle;
            var session = peer.Session;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            stream.Write(arg9);
            stream.Write(arg10);
            stream.Write(arg11);
            stream.Write(arg12);
            stream.Write(arg13);
            stream.Write(arg14);
            stream.Write(arg15);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Check is connected
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsConnected(in NetworkSession session) => session.Token != Guid.Empty && session.Token == _handle->Tokens[session.Id];

        /// <summary>
        ///     Disconnect
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Disconnect(in NetworkSession session)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            handle->Commands.Enqueue(new NetworkCommand
            {
                CommandType = NetworkCommandType.Disconnect,
                Session = session
            });
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send(in NetworkSession session, in NetworkPacketFlag flags, byte* buffer, int length)
        {
            if (length > 1392)
                return false;
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            var packet = enet_packet_create(buffer, length, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send(in NetworkSession session, in NetworkPacketFlag flags, in DataStream stream)
        {
            if (stream.BytesWritten > 1392)
                return false;
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            var packet = enet_packet_create(stream.Bytes, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, void> address)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var packet = enet_packet_create(&command, 4, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, void> address, in T0 arg0)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, void> address, in T0 arg0, in T1 arg1)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, void> address, in T0 arg0, in T1 arg1, in T2 arg2)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            stream.Write(arg9);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            stream.Write(arg9);
            stream.Write(arg10);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10, in T11 arg11)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            stream.Write(arg9);
            stream.Write(arg10);
            stream.Write(arg11);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10, in T11 arg11, in T12 arg12)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            stream.Write(arg9);
            stream.Write(arg10);
            stream.Write(arg11);
            stream.Write(arg12);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10, in T11 arg11, in T12 arg12, in T13 arg13)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            stream.Write(arg9);
            stream.Write(arg10);
            stream.Write(arg11);
            stream.Write(arg12);
            stream.Write(arg13);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10, in T11 arg11, in T12 arg12, in T13 arg13, in T14 arg14)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            stream.Write(arg9);
            stream.Write(arg10);
            stream.Write(arg11);
            stream.Write(arg12);
            stream.Write(arg13);
            stream.Write(arg14);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(in NetworkSession session, in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10, in T11 arg11, in T12 arg12, in T13 arg13, in T14 arg14, in T15 arg15)
        {
            var handle = _handle;
            if (session.Token != handle->Tokens[session.Id])
                return false;
            if (!handle->RpcMethods.TryGetCommand((nint)address, out var command))
                return false;
            var buffer = stackalloc byte[1416];
            var stream = new DataStream(buffer, 1416);
            stream.Write(command);
            stream.Write(arg0);
            stream.Write(arg1);
            stream.Write(arg2);
            stream.Write(arg3);
            stream.Write(arg4);
            stream.Write(arg5);
            stream.Write(arg6);
            stream.Write(arg7);
            stream.Write(arg8);
            stream.Write(arg9);
            stream.Write(arg10);
            stream.Write(arg11);
            stream.Write(arg12);
            stream.Write(arg13);
            stream.Write(arg14);
            stream.Write(arg15);
            var data = buffer + 24;
            var packet = enet_packet_create(data, stream.BytesWritten, (uint)flags);
            handle->Outgoings.Enqueue(new NetworkPacket(session, packet));
            return true;
        }
    }
}