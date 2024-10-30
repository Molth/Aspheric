using System;

namespace Erinn
{
    /// <summary>
    ///     Rpc service attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class RpcServiceAttribute : Attribute
    {
        /// <summary>
        ///     Declared target
        /// </summary>
        public RpcServiceTarget DeclaredTarget;

        /// <summary>
        ///     Structure
        /// </summary>
        public RpcServiceAttribute() => DeclaredTarget = RpcServiceTarget.None;

        /// <summary>
        ///     Structure
        /// </summary>
        public RpcServiceAttribute(RpcServiceTarget declaredTarget) => DeclaredTarget = declaredTarget;
    }
}