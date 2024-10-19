using System;

namespace Erinn
{
    /// <summary>
    ///     Rpc service attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RpcServiceAttribute : Attribute;
}