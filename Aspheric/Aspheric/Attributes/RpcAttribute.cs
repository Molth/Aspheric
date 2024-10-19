using System;

namespace Erinn
{
    /// <summary>
    ///     Rpc attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RpcAttribute : Attribute
    {
        /// <summary>
        ///     Method declared accessibility
        /// </summary>
        public readonly RpcAccessibility DeclaredAccessibility;

        /// <summary>
        ///     Structure
        /// </summary>
        public RpcAttribute() => DeclaredAccessibility = RpcAccessibility.NotApplicable;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="declaredAccessibility">Method declared accessibility</param>
        public RpcAttribute(RpcAccessibility declaredAccessibility) => DeclaredAccessibility = declaredAccessibility;
    }
}