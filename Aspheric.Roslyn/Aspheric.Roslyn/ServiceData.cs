using System.Runtime.InteropServices;

namespace Erinn.Roslyn
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ServiceData
    {
        public uint RpcMethodCount;
        public uint OnConnectedCount;
        public uint OnDisconnectedCount;
        public uint OnErroredCount;
        public uint OnReceivedCount;
    }
}