using System;

// ReSharper disable InconsistentNaming

namespace Erinn
{
    /// <summary>
    ///     On disconnected attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnDisconnectedAttribute : Attribute;
}