using System;

namespace Erinn
{
    /// <summary>
    ///     Rpc handler attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RpcHandlerAttribute : Attribute
    {
        /// <summary>
        ///     Command
        /// </summary>
        public uint Command;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="command">Command</param>
        public RpcHandlerAttribute(uint command) => Command = command;
    }
}