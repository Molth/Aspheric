namespace Erinn
{
    /// <summary>
    ///     Rpc accessibility
    /// </summary>
    public enum RpcAccessibility
    {
        /// <summary>
        ///     None
        /// </summary>
        NotApplicable = 0,

        /// <summary>
        ///     private
        /// </summary>
        Private = 1,

        /// <summary>
        ///     private protected
        /// </summary>
        ProtectedAndInternal = 2,

        /// <summary>
        ///     protected
        /// </summary>
        Protected = 3,

        /// <summary>
        ///     internal
        /// </summary>
        Internal = 4,

        /// <summary>
        ///     protected internal
        /// </summary>
        ProtectedOrInternal = 5,

        /// <summary>
        ///     public
        /// </summary>
        Public = 6
    }
}