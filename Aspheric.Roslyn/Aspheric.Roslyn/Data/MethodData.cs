using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Erinn.Roslyn
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct MethodData
    {
        [FieldOffset(0)] public MethodFlag Flags;
        [FieldOffset(4)] public uint Command;
        [FieldOffset(4)] public Accessibility DeclaredAccessibility;
    }
}