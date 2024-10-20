using System.Runtime.InteropServices;
using enet;

namespace Erinn
{
    /// <summary>
    ///     Network command
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct NetworkCommand
    {
        /// <summary>
        ///     Command type
        /// </summary>
        [FieldOffset(0)] public NetworkCommandType CommandType;

        /// <summary>
        ///     Session
        /// </summary>
        [FieldOffset(8)] public NetworkSession Session;

        /// <summary>
        ///     Address
        /// </summary>
        [FieldOffset(8)] public ENetAddress Address;
    }
}