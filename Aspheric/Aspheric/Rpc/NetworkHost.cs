using enet;

namespace Erinn
{
    public unsafe struct NetworkHost
    {
        public ENetHost* Host;
        public NativeConcurrentDictionary<uint, NetworkPeer> Peers;
        public RpcDictionary RpcDictionary;
        public NativeConcurrentQueue<NetworkOutgoing> Outgoings;
        public NativeConcurrentQueue<NativeArray<byte>> Buffers;
        public delegate* <NetworkPeer, void> OnConnected;
        public delegate* <NetworkPeer, void> OnDisconnected;

        public void Send(NetworkPeer peer, uint channel, delegate* <void> address)
        {
            var value = (nint)address;
            if(!RpcDictionary.TryGetCommand(value,out var command))
                return;
            NativeStream stream = stackalloc byte[8];
            if (!Buffers.TryDequeue(out var buffer))
                buffer = new NativeArray<byte>(1024);
            stream.SetBuffer(buffer);
            stream.Write(command);
        }

        public void Send<T0>(NetworkPeer peer, uint channel, delegate* <T0, void> address, T0 arg0)
        {
            var value = (nint)address;
            if(!RpcDictionary.TryGetCommand(value,out var command))
                return;
            NativeStream stream = stackalloc byte[8];
            if (!Buffers.TryDequeue(out var buffer))
                buffer = new NativeArray<byte>(1024);
            stream.SetBuffer(buffer);
            stream.Write(command);
            stream.Write(arg0);
        }
    }
}