using System;
using System.Collections.Immutable;
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
            ///     Id
            /// </summary>
            public uint Id;

            /// <summary>
            ///     Data
            /// </summary>
            public nint Data;

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
            ///     On errored
            /// </summary>
            public NetworkOnErroredEvent OnErrored;

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
            public volatile int State;

            /// <summary>
            ///     Threads
            /// </summary>
            public volatile int Threads;

            /// <summary>
            ///     Options
            /// </summary>
            public NetworkHostOptions Options;
        }

        /// <summary>
        ///     Service params
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private readonly struct ServiceParams
        {
            /// <summary>
            ///     Handle
            /// </summary>
            public readonly NetworkHostHandle* Handle;

            /// <summary>
            ///     Host
            /// </summary>
            public readonly ENetHost* Host;

            /// <summary>
            ///     Incomings
            /// </summary>
            public readonly NativeConcurrentQueue<NetworkEvent> Incomings;

            /// <summary>
            ///     Commands
            /// </summary>
            public readonly NativeConcurrentQueue<NetworkCommand> Commands;

            /// <summary>
            ///     Outgoings
            /// </summary>
            public readonly NativeConcurrentQueue<NetworkPacket> Outgoings;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="handle">Handle</param>
            /// <param name="host">Host</param>
            /// <param name="incomings">Incomings</param>
            /// <param name="commands">Commands</param>
            /// <param name="outgoings">Outgoings</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ServiceParams(NetworkHostHandle* handle, ENetHost* host, NativeConcurrentQueue<NetworkEvent> incomings, NativeConcurrentQueue<NetworkCommand> commands, NativeConcurrentQueue<NetworkPacket> outgoings)
            {
                Handle = handle;
                Host = host;
                Incomings = incomings;
                Commands = commands;
                Outgoings = outgoings;
            }
        }

        /// <summary>
        ///     Dedicated host
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private readonly struct DedicatedHost
        {
            /// <summary>
            ///     Commands
            /// </summary>
            public readonly NativeConcurrentQueue<NetworkCommand> Commands;

            /// <summary>
            ///     Outgoings
            /// </summary>
            public readonly NativeConcurrentQueue<NetworkPacket> Outgoings;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="commands">Commands</param>
            /// <param name="outgoings">Outgoings</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DedicatedHost(NativeConcurrentQueue<NetworkCommand> commands, NativeConcurrentQueue<NetworkPacket> outgoings)
            {
                Commands = commands;
                Outgoings = outgoings;
            }
        }

        /// <summary>
        ///     Dedicated service params
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private readonly struct DedicatedServiceParams
        {
            /// <summary>
            ///     Id
            /// </summary>
            public readonly uint Id;

            /// <summary>
            ///     Handle
            /// </summary>
            public readonly NetworkHostHandle* Handle;

            /// <summary>
            ///     Host
            /// </summary>
            public readonly ENetHost* Host;

            /// <summary>
            ///     Incomings
            /// </summary>
            public readonly NativeConcurrentQueue<NetworkEvent> Incomings;

            /// <summary>
            ///     Commands
            /// </summary>
            public readonly NativeConcurrentQueue<NetworkCommand> Commands;

            /// <summary>
            ///     Outgoings
            /// </summary>
            public readonly NativeConcurrentQueue<NetworkPacket> Outgoings;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="id">Id</param>
            /// <param name="handle">Handle</param>
            /// <param name="host">Host</param>
            /// <param name="incomings">Incomings</param>
            /// <param name="commands">Commands</param>
            /// <param name="outgoings">Outgoings</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DedicatedServiceParams(uint id, NetworkHostHandle* handle, ENetHost* host, NativeConcurrentQueue<NetworkEvent> incomings, NativeConcurrentQueue<NetworkCommand> commands, NativeConcurrentQueue<NetworkPacket> outgoings)
            {
                Id = id;
                Handle = handle;
                Host = host;
                Incomings = incomings;
                Commands = commands;
                Outgoings = outgoings;
            }
        }

        /// <summary>
        ///     Dedicated services params
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private readonly struct DedicatedServicesParams
        {
            /// <summary>
            ///     Handle
            /// </summary>
            public readonly NetworkHostHandle* Handle;

            /// <summary>
            ///     Hosts
            /// </summary>
            public readonly NativeMemoryArray<DedicatedHost> Hosts;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="handle">Handle</param>
            /// <param name="hosts">Hosts</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DedicatedServicesParams(NetworkHostHandle* handle, NativeArray<DedicatedHost> hosts)
            {
                Handle = handle;
                Hosts = hosts;
            }
        }

        /// <summary>
        ///     Check events params
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private readonly struct CheckEventsParams
        {
            /// <summary>
            ///     Handle
            /// </summary>
            public readonly NetworkHostHandle* Handle;

            /// <summary>
            ///     Host
            /// </summary>
            public readonly NetworkHost Host;

            /// <summary>
            ///     Incomings
            /// </summary>
            public readonly NativeConcurrentQueue<NetworkEvent> Incomings;

            /// <summary>
            ///     Structure
            /// </summary>
            /// <param name="handle">Handle</param>
            /// <param name="host">Host</param>
            /// <param name="incomings">Incomings</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public CheckEventsParams(NetworkHostHandle* handle, NetworkHost host, NativeConcurrentQueue<NetworkEvent> incomings)
            {
                Handle = handle;
                Host = host;
                Incomings = incomings;
            }
        }

        /// <summary>
        ///     Handle
        /// </summary>
        private readonly NetworkHostHandle* _handle;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="id">Id</param>
        /// <param name="data">Data</param>
        /// <param name="rpcMethods">Rpc methods</param>
        /// <param name="onConnected">On connected</param>
        /// <param name="onDisconnected">On disconnected</param>
        /// <param name="onErrored">On errored</param>
        /// <param name="onReceived">On received</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NetworkHost(uint id, nint data, RpcMethods rpcMethods, NetworkOnConnectedEvent onConnected, NetworkOnDisconnectedEvent onDisconnected, NetworkOnErroredEvent onErrored, NetworkOnReceivedEvent onReceived)
        {
            var handle = (NetworkHostHandle*)NativeMemoryAllocator.AllocZeroed((uint)sizeof(NetworkHostHandle));
            handle->Id = id;
            handle->Data = data;
            handle->RpcMethods = rpcMethods;
            handle->OnConnected = onConnected;
            handle->OnDisconnected = onDisconnected;
            handle->OnErrored = onErrored;
            handle->OnReceived = onReceived;
            _handle = handle;
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle != null;
        }

        /// <summary>
        ///     Id
        /// </summary>
        public uint Id
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle->Id;
        }

        /// <summary>
        ///     Data
        /// </summary>
        public nint Data
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle->Data;
        }

        /// <summary>
        ///     Rpc methods
        /// </summary>
        public RpcMethods RpcMethods
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle->RpcMethods;
        }

        /// <summary>
        ///     On connected
        /// </summary>
        public NetworkOnConnectedEvent OnConnected
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle->OnConnected;
        }

        /// <summary>
        ///     On disconnected
        /// </summary>
        public NetworkOnDisconnectedEvent OnDisconnected
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle->OnDisconnected;
        }

        /// <summary>
        ///     On errored
        /// </summary>
        public NetworkOnErroredEvent OnErrored
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle->OnErrored;
        }

        /// <summary>
        ///     On received
        /// </summary>
        public NetworkOnReceivedEvent OnReceived
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle->OnReceived;
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
            var spinWait = new FastSpinWait();
            while (handle->Threads != 0)
                spinWait.SpinOnce();
            NativeMemoryAllocator.Free(handle);
        }

        /// <summary>
        ///     Start
        /// </summary>
        /// <param name="port">Port</param>
        /// <param name="options">Options</param>
        /// <returns>Started</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SocketError Start(ushort port, in NetworkHostOptions options) => Start(port, options.PeerCount, options.PingInterval, options.Timeout, options.IncomingBandwidth, options.OutgoingBandwidth);

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

            host->maximumPacketSize = 1392;
            handle->Tokens = new NativeArray<Guid>(peerCount, true);
            handle->Commands = new NativeConcurrentQueue<NetworkCommand>(1, 0);
            handle->Outgoings = new NativeConcurrentQueue<NetworkPacket>(1, 2);
            handle->Options = new NetworkHostOptions(peerCount, pingInterval, timeout, incomingBandwidth, outgoingBandwidth);
            var incomings = new NativeConcurrentQueue<NetworkEvent>(1, 2);
            var serviceThread = new Thread(Service) { IsBackground = true };
            serviceThread.Start(new ServiceParams(handle, host, incomings, handle->Commands, handle->Outgoings));
            var checkEventsThread = new Thread(CheckEvents) { IsBackground = true };
            checkEventsThread.Start(new CheckEventsParams(handle, this, incomings));
            Interlocked.Exchange(ref handle->Threads, 2);
            return SocketError.Success;
        }

        /// <summary>
        ///     Start
        /// </summary>
        /// <param name="ports">Ports</param>
        /// <param name="options">Options</param>
        /// <returns>Started</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SocketError Start(ImmutableHashSet<ushort> ports, in NetworkHostOptions options) => Start(ports, options.PeerCount, options.PingInterval, options.Timeout, options.IncomingBandwidth, options.OutgoingBandwidth);

        /// <summary>
        ///     Start
        /// </summary>
        /// <param name="ports">Ports</param>
        /// <param name="peerCount">Peer count</param>
        /// <param name="pingInterval">Ping interval</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="incomingBandwidth">Incoming bandwidth</param>
        /// <param name="outgoingBandwidth">Outgoing bandwidth</param>
        /// <returns>Started</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SocketError Start(ImmutableHashSet<ushort> ports, ushort peerCount, uint pingInterval = 500, uint timeout = 5000, uint incomingBandwidth = 0, uint outgoingBandwidth = 0)
        {
            var hostCount = ports.Count;
            if (hostCount == 0)
                return SocketError.Fault;
            var handle = _handle;
            if (handle->Threads != 0 || Interlocked.CompareExchange(ref handle->State, 2, 0) != 0)
                return SocketError.InProgress;
            if (enet_initialize() != 0)
            {
                Interlocked.Exchange(ref handle->State, 0);
                return SocketError.NotInitialized;
            }

            for (var i = 1; i < hostCount; ++i)
                _ = enet_initialize();
            var id = 0;
            var hosts = stackalloc nint[hostCount];
            foreach (var port in ports)
            {
                ENetAddress address;
                _ = enet_set_ip(&address, Socket.OSSupportsIPv6 ? "::0" : "0.0.0.0");
                address.port = port;
                var host = enet_host_create(&address, peerCount, 2, incomingBandwidth, outgoingBandwidth);
                if (host == null)
                {
                    for (var j = 0; j < id; ++j)
                        enet_host_destroy((ENetHost*)hosts[j]);
                    for (var j = 0; j < hostCount; ++j)
                        enet_deinitialize();
                    Interlocked.Exchange(ref handle->State, 0);
                    return SocketError.AddressAlreadyInUse;
                }

                host->maximumPacketSize = 1392;
                hosts[id++] = (nint)host;
            }

            var dedicatedHosts = new NativeArray<DedicatedHost>(hostCount);
            handle->Tokens = new NativeArray<Guid>(hostCount * peerCount, true);
            handle->Commands = new NativeConcurrentQueue<NetworkCommand>(1, Math.Max(hostCount / 2, 1));
            handle->Outgoings = new NativeConcurrentQueue<NetworkPacket>(1, Math.Max(hostCount, 2));
            handle->Options = new NetworkHostOptions(peerCount, pingInterval, timeout, incomingBandwidth, outgoingBandwidth);
            var incomings = new NativeConcurrentQueue<NetworkEvent>(1, Math.Max(hostCount, 2));
            for (uint i = 0; i < hostCount; ++i)
            {
                var commands = new NativeConcurrentQueue<NetworkCommand>(1, 0);
                var outgoings = new NativeConcurrentQueue<NetworkPacket>(1, 2);
                dedicatedHosts[i] = new DedicatedHost(commands, outgoings);
                var dedicatedServiceThread = new Thread(DedicatedService) { IsBackground = true };
                dedicatedServiceThread.Start(new DedicatedServiceParams(i, handle, (ENetHost*)hosts[i], incomings, commands, outgoings));
            }

            var dedicatedServicesThread = new Thread(DedicatedServices) { IsBackground = true };
            dedicatedServicesThread.Start(new DedicatedServicesParams(handle, dedicatedHosts));
            var dedicatedCheckEventsThread = new Thread(DedicatedCheckEvents) { IsBackground = true };
            dedicatedCheckEventsThread.Start(new CheckEventsParams(handle, this, incomings));
            Interlocked.Exchange(ref handle->Threads, hostCount + 2);
            return SocketError.Success;
        }

        /// <summary>
        ///     Service
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Service(object? obj)
        {
            var @params = (ServiceParams)obj;
            var handle = @params.Handle;
            var host = @params.Host;
            var incomings = @params.Incomings;
            var commands = @params.Commands;
            var outgoings = @params.Outgoings;
            var options = handle->Options;
            var peerCount = options.PeerCount;
            var @event = new ENetEvent();
            var spinWait = new FastSpinWait();
            while (handle->State == 1)
            {
                while (commands.TryDequeue(out var command))
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

                while (outgoings.TryDequeue(out var outgoing))
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

                spinWait.SpinOnce();
            }

            for (var i = 0; i < peerCount; ++i)
                enet_peer_disconnect_now(&host->peers[i], 0);
            enet_host_flush(host);
            enet_host_destroy(host);
            enet_deinitialize();
            commands.Dispose();
            while (outgoings.TryDequeue(out var outgoing))
                enet_packet_destroy(outgoing.Payload);
            outgoings.Dispose();
            Interlocked.Decrement(ref handle->Threads);
        }

        /// <summary>
        ///     Check events
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckEvents(object? obj)
        {
            var @params = (CheckEventsParams)obj;
            var handle = @params.Handle;
            var host = @params.Host;
            var incomings = @params.Incomings;
            var buffer = stackalloc byte[24];
            var stream = new DataStream(buffer, 24);
            var spinWait = new FastSpinWait();
            while (handle->State == 1)
            {
                while (incomings.TryDequeue(out var incoming))
                {
                    var peer = new NetworkPeer(host, incoming.Session, incoming.Address);
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
                                        handle->OnErrored.Invoke(peer, flags, packet->data, (int)packet->dataLength, e);
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
                                    handle->OnErrored.Invoke(peer, flags, packet->data, (int)packet->dataLength, e);
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

                spinWait.SpinOnce();
            }

            Interlocked.Decrement(ref handle->Threads);
            spinWait.Reset();
            while (handle->Threads != 0)
                spinWait.SpinOnce();
            while (incomings.TryDequeue(out var incoming))
                enet_packet_destroy(incoming.Payload);
            incomings.Dispose();
            handle->Tokens.Dispose();
        }

        /// <summary>
        ///     Dedicated service
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DedicatedService(object? obj)
        {
            var @params = (DedicatedServiceParams)obj;
            var handle = @params.Handle;
            var host = @params.Host;
            var incomings = @params.Incomings;
            var commands = @params.Commands;
            var outgoings = @params.Outgoings;
            var options = handle->Options;
            var peerCount = options.PeerCount;
            var sessionOffset = peerCount * @params.Id;
            var @event = new ENetEvent();
            var spinWait = new FastSpinWait();
            while (handle->State == 2)
            {
                while (commands.TryDequeue(out var command))
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
                            enet_peer_disconnect(&host->peers[id % peerCount], 0);
                            continue;
                        case NetworkCommandType.Ping:
                            enet_host_ping(host, &command.Address);
                            continue;
                        default:
                            continue;
                    }
                }

                while (outgoings.TryDequeue(out var outgoing))
                {
                    var id = outgoing.Session.Id;
                    var packet = outgoing.Payload;
                    if (outgoing.Session.Token == handle->Tokens[id])
                    {
                        var channel = (packet->flags & (uint)ENetPacketFlag.ENET_PACKET_FLAG_RELIABLE) != 0 ? (byte)0 : (byte)1;
                        if (enet_peer_send(&host->peers[id % peerCount], channel, packet) == 0)
                            continue;
                    }

                    enet_packet_destroy(packet);
                }

                while (enet_host_service(host, &@event, 0) > 0)
                {
                    var peer = @event.peer;
                    var id = sessionOffset + peer->incomingPeerID;
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

                spinWait.SpinOnce();
            }

            for (var i = 0; i < peerCount; ++i)
                enet_peer_disconnect_now(&host->peers[i], 0);
            enet_host_flush(host);
            enet_host_destroy(host);
            enet_deinitialize();
            Interlocked.Decrement(ref handle->Threads);
            spinWait.Reset();
            while (handle->Threads != 0)
                spinWait.SpinOnce();
            commands.Dispose();
            while (outgoings.TryDequeue(out var outgoing))
                enet_packet_destroy(outgoing.Payload);
            outgoings.Dispose();
        }

        /// <summary>
        ///     Dedicated services
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DedicatedServices(object? obj)
        {
            var @params = (DedicatedServicesParams)obj;
            var handle = @params.Handle;
            var peerCount = handle->Options.PeerCount;
            var hosts = @params.Hosts;
            var spinWait = new FastSpinWait();
            while (handle->State == 2)
            {
                while (handle->Commands.TryDequeue(out var command))
                {
                    uint hostId;
                    switch (command.CommandType)
                    {
                        case NetworkCommandType.None:
                            continue;
                        case NetworkCommandType.Connect:
                        case NetworkCommandType.Ping:
                            hostId = command.Id;
                            if (hostId < hosts.Length)
                                hosts[hostId]->Commands.Enqueue(command);
                            continue;
                        case NetworkCommandType.Disconnect:
                            hostId = command.Session.Id / peerCount;
                            if (hostId < hosts.Length)
                                hosts[hostId]->Commands.Enqueue(command);
                            continue;
                        default:
                            continue;
                    }
                }

                while (handle->Outgoings.TryDequeue(out var outgoing))
                {
                    var hostId = outgoing.Session.Id / peerCount;
                    if (hostId < hosts.Length)
                    {
                        hosts[hostId]->Outgoings.Enqueue(outgoing);
                        continue;
                    }

                    enet_packet_destroy(outgoing.Payload);
                }

                spinWait.SpinOnce();
            }

            hosts.Dispose();
            handle->Commands.Dispose();
            while (handle->Outgoings.TryDequeue(out var outgoing))
                enet_packet_destroy(outgoing.Payload);
            handle->Outgoings.Dispose();
            Interlocked.Decrement(ref handle->Threads);
        }

        /// <summary>
        ///     Dedicated check events
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DedicatedCheckEvents(object? obj)
        {
            var @params = (CheckEventsParams)obj;
            var handle = @params.Handle;
            var host = @params.Host;
            var incomings = @params.Incomings;
            var buffer = stackalloc byte[24];
            var stream = new DataStream(buffer, 24);
            var spinWait = new FastSpinWait();
            while (handle->State == 2)
            {
                while (incomings.TryDequeue(out var incoming))
                {
                    var peer = new NetworkPeer(host, incoming.Session, incoming.Address);
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
                                        handle->OnErrored.Invoke(peer, flags, packet->data, (int)packet->dataLength, e);
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
                                    handle->OnErrored.Invoke(peer, flags, packet->data, (int)packet->dataLength, e);
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

                spinWait.SpinOnce();
            }

            Interlocked.Decrement(ref handle->Threads);
            spinWait.Reset();
            while (handle->Threads != 0)
                spinWait.SpinOnce();
            while (incomings.TryDequeue(out var incoming))
                enet_packet_destroy(incoming.Payload);
            incomings.Dispose();
            handle->Tokens.Dispose();
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
        ///     Connect
        /// </summary>
        /// <param name="address">Address</param>
        /// <returns>Started</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Connect(in ENetAddress address)
        {
            var handle = _handle;
            if (handle->State != 1)
                return false;
            handle->Commands.Enqueue(new NetworkCommand
            {
                CommandType = NetworkCommandType.Connect,
                Address = address
            });
            return true;
        }

        /// <summary>
        ///     Connect
        /// </summary>
        /// <param name="id">Dedicated id</param>
        /// <param name="ip">IP</param>
        /// <param name="port">Port</param>
        /// <returns>Started</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Connect(uint id, string ip, ushort port)
        {
            var handle = _handle;
            if (handle->State != 2)
                return false;
            ENetAddress address;
            _ = enet_set_ip(&address, ip);
            address.port = port;
            handle->Commands.Enqueue(new NetworkCommand
            {
                CommandType = NetworkCommandType.Connect,
                Id = id,
                Address = address
            });
            return true;
        }

        /// <summary>
        ///     Connect
        /// </summary>
        /// <param name="id">Dedicated id</param>
        /// <param name="address">Address</param>
        /// <returns>Started</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Connect(uint id, in ENetAddress address)
        {
            var handle = _handle;
            if (handle->State != 2)
                return false;
            handle->Commands.Enqueue(new NetworkCommand
            {
                CommandType = NetworkCommandType.Connect,
                Id = id,
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
        public bool Ping(string ip, ushort port)
        {
            var handle = _handle;
            if (handle->State != 1)
                return false;
            ENetAddress address;
            enet_set_ip(&address, ip);
            address.port = port;
            handle->Commands.Enqueue(new NetworkCommand
            {
                CommandType = NetworkCommandType.Ping,
                Address = address
            });
            return true;
        }

        /// <summary>
        ///     Ping
        /// </summary>
        /// <param name="address">Address</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Ping(in ENetAddress address)
        {
            var handle = _handle;
            if (handle->State != 1)
                return false;
            handle->Commands.Enqueue(new NetworkCommand
            {
                CommandType = NetworkCommandType.Ping,
                Address = address
            });
            return true;
        }

        /// <summary>
        ///     Ping
        /// </summary>
        /// <param name="id">Dedicated id</param>
        /// <param name="ip">IP</param>
        /// <param name="port">Port</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Ping(uint id, string ip, ushort port)
        {
            var handle = _handle;
            if (handle->State != 2)
                return false;
            ENetAddress address;
            enet_set_ip(&address, ip);
            address.port = port;
            handle->Commands.Enqueue(new NetworkCommand
            {
                CommandType = NetworkCommandType.Ping,
                Id = id,
                Address = address
            });
            return true;
        }

        /// <summary>
        ///     Ping
        /// </summary>
        /// <param name="id">Dedicated id</param>
        /// <param name="address">Address</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Ping(uint id, in ENetAddress address)
        {
            var handle = _handle;
            if (handle->State != 2)
                return false;
            handle->Commands.Enqueue(new NetworkCommand
            {
                CommandType = NetworkCommandType.Ping,
                Id = id,
                Address = address
            });
            return true;
        }

        /// <summary>
        ///     Shutdown
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Shutdown()
        {
            var handle = _handle;
            return Interlocked.CompareExchange(ref handle->State, 0, 1) == 1 || Interlocked.CompareExchange(ref handle->State, 0, 2) == 2;
        }

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