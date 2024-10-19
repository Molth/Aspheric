using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using enet;

namespace Erinn
{
    /// <summary>
    ///     Network peer
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NetworkPeer : IEquatable<NetworkPeer>
    {
        /// <summary>
        ///     Host
        /// </summary>
        public readonly NetworkHost Host;

        /// <summary>
        ///     Session
        /// </summary>
        public readonly NetworkSession Session;

        /// <summary>
        ///     Address
        /// </summary>
        public readonly ENetAddress Address;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="host">Host</param>
        /// <param name="session">Session</param>
        /// <param name="address">Address</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NetworkPeer(in NetworkHost host, in NetworkSession session, in ENetAddress address)
        {
            Host = host;
            Session = session;
            Address = address;
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => Session.IsCreated;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NetworkPeer other) => other.Session == Session;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NetworkPeer networkPeer && networkPeer == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => Session.GetHashCode();

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => $"NetworkPeer[{GetHashCode()}]";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NetworkPeer left, NetworkPeer right) => left.Session == right.Session;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NetworkPeer left, NetworkPeer right) => left.Session != right.Session;

        /// <summary>
        ///     Check is connected
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsConnected() => Host.IsConnected(this);

        /// <summary>
        ///     Disconnect
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Disconnect() => Host.Disconnect(this);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send(in NetworkPacketFlag flags, in DataStream stream) => Host.Send(this, flags, stream);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, void> address) => Host.Send(this, flags, address);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, void> address, in T0 arg0) => Host.Send(this, flags, address, arg0);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, void> address, in T0 arg0, in T1 arg1) => Host.Send(this, flags, address, arg0, arg1);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, void> address, in T0 arg0, in T1 arg1, in T2 arg2) => Host.Send(this, flags, address, arg0, arg1, arg2);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3) => Host.Send(this, flags, address, arg0, arg1, arg2, arg3);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4) => Host.Send(this, flags, address, arg0, arg1, arg2, arg3, arg4);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5) => Host.Send(this, flags, address, arg0, arg1, arg2, arg3, arg4, arg5);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6) => Host.Send(this, flags, address, arg0, arg1, arg2, arg3, arg4, arg5, arg6);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7) => Host.Send(this, flags, address, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8) => Host.Send(this, flags, address, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9) => Host.Send(this, flags, address, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10) => Host.Send(this, flags, address, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10, in T11 arg11) => Host.Send(this, flags, address, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10, in T11 arg11, in T12 arg12) => Host.Send(this, flags, address, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10, in T11 arg11, in T12 arg12, in T13 arg13) => Host.Send(this, flags, address, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10, in T11 arg11, in T12 arg12, in T13 arg13, in T14 arg14) => Host.Send(this, flags, address, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);

        /// <summary>
        ///     Send
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(in NetworkPacketFlag flags, delegate* managed<in NetworkPeer, in NetworkPacketFlag, in T0, in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, void> address, in T0 arg0, in T1 arg1, in T2 arg2, in T3 arg3, in T4 arg4, in T5 arg5, in T6 arg6, in T7 arg7, in T8 arg8, in T9 arg9, in T10 arg10, in T11 arg11, in T12 arg12, in T13 arg13, in T14 arg14, in T15 arg15) => Host.Send(this, flags, address, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
    }
}