using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erinn
{
    /// <summary>
    ///     Network session
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct NetworkSession : IEquatable<NetworkSession>
    {
        /// <summary>
        ///     Id
        /// </summary>
        [FieldOffset(0)] public readonly uint Id;

        /// <summary>
        ///     Token
        /// </summary>
        [FieldOffset(4)] public readonly Guid Token;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="id">Id</param>
        /// <param name="token">Token</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NetworkSession(uint id, in Guid token)
        {
            Id = id;
            Token = token;
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => Token != Guid.Empty;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NetworkSession other) => Token == other.Token;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NetworkSession networkSession && networkSession == this;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => $"NetworkSession[{GetHashCode()}]";

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => Token.GetHashCode();

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NetworkSession left, NetworkSession right) => left.Token == right.Token;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NetworkSession left, NetworkSession right) => left.Token != right.Token;
    }
}