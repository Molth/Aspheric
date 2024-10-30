using System;

namespace Erinn
{
    /// <summary>
    ///     Rpc manual attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RpcManualAttribute : Attribute
    {
        /// <summary>
        ///     Command
        /// </summary>
        public uint Command;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="command">Command</param>
        public RpcManualAttribute(uint command) => Command = command;
    }
}