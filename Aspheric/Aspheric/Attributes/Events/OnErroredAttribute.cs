using System;

// ReSharper disable InconsistentNaming

namespace Erinn
{
    /// <summary>
    ///     On errored attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OnErroredAttribute : Attribute;
}