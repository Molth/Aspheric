// ReSharper disable InconsistentNaming

using System;

namespace Erinn
{
    /// <summary>
    ///     Rpc service target
    /// </summary>
    [Flags]
    public enum RpcServiceTarget
    {
        /// <summary>
        ///     None
        /// </summary>
        None = 0,

        /// <summary>
        ///     Rpc
        /// </summary>
        Rpc = 1,

        /// <summary>
        ///     Rpc manual
        /// </summary>
        RpcManual = 2,

        /// <summary>
        ///     On connected
        /// </summary>
        OnConnected = 4,

        /// <summary>
        ///     On disconnected
        /// </summary>
        OnDisconnected = 8,

        /// <summary>
        ///     On errored
        /// </summary>
        OnErrored = 16,

        /// <summary>
        ///     On received
        /// </summary>
        OnReceived = 32,

        /// <summary>
        ///     All
        /// </summary>
        All = Rpc | RpcManual | OnConnected | OnDisconnected | OnErrored | OnReceived,

        /// <summary>
        ///     private
        /// </summary>
        Private = 64,

        /// <summary>
        ///     private protected
        /// </summary>
        ProtectedAndInternal = 128,

        /// <summary>
        ///     protected
        /// </summary>
        Protected = 256,

        /// <summary>
        ///     internal
        /// </summary>
        Internal = 512,

        /// <summary>
        ///     protected internal
        /// </summary>
        ProtectedOrInternal = 1024,

        /// <summary>
        ///     public
        /// </summary>
        Public = 2048
    }
}