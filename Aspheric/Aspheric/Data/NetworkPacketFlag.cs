using System;

namespace Erinn
{
    /// <summary>
    ///     Network packet flag
    /// </summary>
    [Flags]
    public enum NetworkPacketFlag
    {
        /// <summary>
        ///     None
        /// </summary>
        None = 0,

        /// <summary>
        ///     Reliable
        /// </summary>
        Reliable = 1 << 0,

        /// <summary>
        ///     Unsequenced
        /// </summary>
        Unsequenced = 1 << 1,

        /// <summary>
        ///     No allocate
        /// </summary>
        NoAllocate = 1 << 2,

        /// <summary>
        ///     Unreliable fragment
        /// </summary>
        UnreliableFragment = 1 << 3,

        /// <summary>
        ///     Sent
        /// </summary>
        Sent = 1 << 8
    }
}