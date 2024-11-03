using System;

// ReSharper disable ALL

namespace Erinn
{
    /// <summary>
    ///     Rpc attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class _RpcAttribute : Attribute
    {
        /// <summary>
        ///     Command
        /// </summary>
        public uint Command;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="command">Command</param>
        public _RpcAttribute(uint command) => Command = command;
    }
}