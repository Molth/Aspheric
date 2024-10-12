using System;

// ReSharper disable ALL

namespace Erinn
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RpcAttribute : Attribute;
}