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
        OnReceived = 32,
        Private = 64,
        ProtectedAndInternal = 128,
        Protected = 256,
        Internal = 512,
        ProtectedOrInternal = 1024,
        Public = 2048
    }
}