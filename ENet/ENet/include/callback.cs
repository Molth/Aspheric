using System.Runtime.InteropServices;
using size_t = nint;

// ReSharper disable ALL

namespace enet
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ENetCallbacks
    {
        public delegate* managed<size_t, void*> malloc;
        public delegate* managed<void*, void> free;

        public ENetCallbacks(delegate* managed<size_t, void*> malloc, delegate* managed<void*, void> free)
        {
            this.malloc = malloc;
            this.free = free;
        }
    }
}