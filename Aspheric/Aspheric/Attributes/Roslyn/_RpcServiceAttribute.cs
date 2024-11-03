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
        ///     On connected count
        /// </summary>
        public uint OnConnectedCount;

        /// <summary>
        ///     On disconnected count
        /// </summary>
        public uint OnDisconnectedCount;

        /// <summary>
        ///     On errored count
        /// </summary>
        public uint OnErroredCount;

        /// <summary>
        ///     On received count
        /// </summary>
        public uint OnReceivedCount;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="rpcMethodCount">Rpc method count</param>
        /// <param name="onConnectedCount">On connected count</param>
        /// <param name="onDisconnectedCount">On disconnected count</param>
        /// <param name="onErroredCount">On errored count</param>
        /// <param name="onReceivedCount">On received count</param>
        public _RpcServiceAttribute(uint rpcMethodCount, uint onConnectedCount, uint onDisconnectedCount, uint onErroredCount, uint onReceivedCount)
        {
            RpcMethodCount = rpcMethodCount;
            OnConnectedCount = onConnectedCount;
            OnDisconnectedCount = onDisconnectedCount;
            OnErroredCount = onErroredCount;
            OnReceivedCount = onReceivedCount;
        }
    }
}