using System;

// ReSharper disable ALL

namespace Erinn
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class _RpcAttribute : Attribute
    {
        public uint Command;

        public _RpcAttribute(uint command) => Command = command;
    }
}