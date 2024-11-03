using System;

// ReSharper disable ALL

namespace Erinn
{
    /// <summary>
    ///     Rpc deinitialize attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class _RpcDeinitializeAttribute : Attribute
    {
        /// <summary>
        ///     Rpc method count
        /// </summary>
        public uint RpcMethodCount;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="rpcMethodCount">Rpc method count</param>
        public _RpcDeinitializeAttribute(uint rpcMethodCount) => RpcMethodCount = rpcMethodCount;
    }
}