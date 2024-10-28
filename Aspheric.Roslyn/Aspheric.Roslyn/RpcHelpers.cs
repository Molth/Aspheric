using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Erinn.Roslyn
{
    internal static unsafe class RpcHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPartialType(INamedTypeSymbol typeSymbol)
        {
            for (var i = 0; i < typeSymbol.DeclaringSyntaxReferences.Length; ++i)
            {
                var syntaxReference = typeSymbol.DeclaringSyntaxReferences[i];
                var syntaxNode = syntaxReference.GetSyntax();
                if (syntaxNode is TypeDeclarationSyntax typeDeclaration)
                    return typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidRefKind(IParameterSymbol parameterSymbol) => parameterSymbol.GetType() != typeof(IPointerTypeSymbol) && parameterSymbol.RefKind == RefKind.In && !parameterSymbol.Type.IsRefLikeType;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidRpcManualParameters(ImmutableArray<IParameterSymbol> parameters) => parameters.Length == 3 && parameters[0].RefKind == RefKind.In && parameters[0].Type.ToDisplayString() == "Erinn.NetworkPeer" && parameters[0].Type.ContainingAssembly.Name == "Aspheric" && parameters[1].RefKind == RefKind.In && parameters[1].Type.ToDisplayString() == "Erinn.NetworkPacketFlag" && parameters[1].Type.ContainingAssembly.Name == "Aspheric" && parameters[2].RefKind == RefKind.In && parameters[2].Type.ToDisplayString() == "Erinn.DataStream" && parameters[2].Type.ContainingAssembly.Name == "Aspheric";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Hash32(StringBuilder sb)
        {
            Span<char> chars = stackalloc char[sb.Length];
            sb.CopyTo(0, chars, sb.Length);
            var length = Encoding.UTF8.GetByteCount(chars);
            var buffer = stackalloc byte[length];
            Encoding.UTF8.GetBytes(chars, MemoryMarshal.CreateSpan(ref *buffer, length));
            return Hash32(buffer, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Hash32(byte* input, int length, uint seed = 0U)
        {
            var num1 = seed + 374761393U;
            if (length >= 16)
            {
                var num2 = (uint)((int)seed - 1640531535 - 2048144777);
                var num3 = seed + 2246822519U;
                var num4 = seed;
                var num5 = seed - 2654435761U;
                var num6 = length >> 4;
                for (var index = 0; index < num6; ++index)
                {
                    var num7 = *(uint*)input;
                    var num8 = *(uint*)(input + 4);
                    var num9 = *(uint*)(input + 8);
                    var num10 = *(uint*)(input + 12);
                    var num11 = num2 + num7 * 2246822519U;
                    num2 = ((num11 << 13) | (num11 >> 19)) * 2654435761U;
                    var num12 = num3 + num8 * 2246822519U;
                    num3 = ((num12 << 13) | (num12 >> 19)) * 2654435761U;
                    var num13 = num4 + num9 * 2246822519U;
                    num4 = ((num13 << 13) | (num13 >> 19)) * 2654435761U;
                    var num14 = num5 + num10 * 2246822519U;
                    num5 = ((num14 << 13) | (num14 >> 19)) * 2654435761U;
                    input += 16;
                }

                num1 = (uint)((((int)num2 << 1) | (int)(num2 >> 31)) + (((int)num3 << 7) | (int)(num3 >> 25)) + (((int)num4 << 12) | (int)(num4 >> 20)) + (((int)num5 << 18) | (int)(num5 >> 14)));
            }

            var num15 = num1 + (uint)length;
            for (length &= 15; length >= 4; length -= 4)
            {
                var num16 = num15 + *(uint*)input * 3266489917U;
                num15 = (uint)((((int)num16 << 17) | (int)(num16 >> 15)) * 668265263);
                input += 4;
            }

            for (; length > 0; --length)
            {
                var num17 = num15 + *input * 374761393U;
                num15 = (uint)((((int)num17 << 11) | (int)(num17 >> 21)) * -1640531535);
                ++input;
            }

            var num18 = (num15 ^ (num15 >> 15)) * 2246822519U;
            var num19 = (num18 ^ (num18 >> 13)) * 3266489917U;
            return num19 ^ (num19 >> 16);
        }
    }
}