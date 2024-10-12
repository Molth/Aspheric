using System;
using enet;

namespace Erinn
{
    public unsafe struct NetworkPeer : IEquatable<NetworkPeer>
    {
        public uint Id;
        public NetworkHost* Host;
        public ENetPeer* Peer;

        public NetworkPeer(uint id, NetworkHost* host, ENetPeer* peer)
        {
            Id = id;
            Host = host;
            Peer = peer;
        }

        public bool Equals(NetworkPeer other) => Id == other.Id && Host == other.Host && Peer == other.Peer;
        public override bool Equals(object? obj) => obj is NetworkPeer other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Id;
                hashCode = (hashCode * 397) ^ unchecked((int)(long)Host);
                hashCode = (hashCode * 397) ^ unchecked((int)(long)Peer);
                return hashCode;
            }
        }

        public static bool operator ==(NetworkPeer left, NetworkPeer right) => left.Equals(right);
        public static bool operator !=(NetworkPeer left, NetworkPeer right) => !(left == right);
    }
}