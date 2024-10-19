using System;

// ReSharper disable ALL

namespace Erinn
{
    /// <summary>
    ///     Rpc service attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class _RpcServiceAttribute : Attribute
    {
        /// <summary>
        ///     Rpc method count
        /// </summary>
        public uint RpcMethodCount;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="rpcMethodCount">Rpc method count</param>
        public _RpcServiceAttribute(uint rpcMethodCount) => RpcMethodCount = rpcMethodCount;
    }
}