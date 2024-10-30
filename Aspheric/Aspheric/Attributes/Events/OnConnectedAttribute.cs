using System;

// ReSharper disable InconsistentNaming

namespace Erinn
{
    /// <summary>
    ///     On connected attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnConnectedAttribute : Attribute;
}