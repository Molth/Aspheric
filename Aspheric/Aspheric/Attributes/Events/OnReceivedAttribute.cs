using System;

// ReSharper disable InconsistentNaming

namespace Erinn
{
    /// <summary>
    ///     On received attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnReceivedAttribute : Attribute;
}