// ReSharper disable InconsistentNaming

using System;

namespace Erinn.Roslyn
{
    [Flags]
    internal enum ServiceTarget
    {
        None = 0,
        Rpc = 1,
        RpcManual = 2,
        OnConnected = 4,
        OnDisconnected = 8,
        OnErrored = 16,
        OnReceived = 32
    }
}