using System;

// ReSharper disable ALL

namespace Erinn
{
    /// <summary>
    ///     Rpc initialize attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class _RpcInitializeAttribute : Attribute
    {
        /// <summary>
        ///     Rpc method count
        /// </summary>
        public uint RpcMethodCount;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="rpcMethodCount">Rpc method count</param>
        public _RpcInitializeAttribute(uint rpcMethodCount) => RpcMethodCount = rpcMethodCount;
    }
}