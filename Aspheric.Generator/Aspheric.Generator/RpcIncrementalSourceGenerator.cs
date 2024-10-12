using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable RS1041

namespace Erinn
{
    [Generator]
    internal sealed class RpcIncrementalSourceGenerator : IIncrementalGenerator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var methodsWithAttribute = context.SyntaxProvider.CreateSyntaxProvider(static (node, _) => node is MethodDeclarationSyntax method && IsAnnotatedWithRpcAttribute(method), static (ctx, _) => GetMethodDeclarationForSourceGen(ctx)).Where(t => t.Found).Select((t, _) => t.Item1);
            context.RegisterSourceOutput(context.CompilationProvider.Combine(methodsWithAttribute.Collect()), static (ctx, t) => GenerateCode(ctx, t.Left, t.Right));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAnnotatedWithRpcAttribute(MethodDeclarationSyntax method) => method.AttributeLists.Any(s => s.Attributes.Any(a => a.Name.ToString() == "Rpc"));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (MethodDeclarationSyntax, bool Found) GetMethodDeclarationForSourceGen(GeneratorSyntaxContext context)
        {
            var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;
            foreach (var attributeListSyntax in methodDeclarationSyntax.AttributeLists)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                        continue;
                    var attributeName = attributeSymbol.ContainingType.ToDisplayString();
                    if (attributeName == "Erinn.RpcAttribute")
                        return (methodDeclarationSyntax, true);
                }
            }

            return (methodDeclarationSyntax, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateCode(SourceProductionContext context, Compilation compilation, ImmutableArray<MethodDeclarationSyntax> methodDeclarations)
        {
            var sb = new StringBuilder(1024);
            var classCodeMap = new Dictionary<string, (StringBuilder CodeBuilder, bool HasNamespace, List<uint> Defines, List<string>Addresses)>();
            var rpcClasses = new List<(string FullName, bool HasNamespace)>();
            foreach (var methodDeclarationSyntax in methodDeclarations)
            {
                var semanticModel = compilation.GetSemanticModel(methodDeclarationSyntax.SyntaxTree);
                var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                if (methodSymbol is null || !IsValidType(methodSymbol) || IsNestedType(methodSymbol.ContainingType) || !IsPartialType(methodSymbol.ContainingType))
                    continue;
                var parameters = methodSymbol.Parameters;
                if (parameters.Length < 1 || parameters[0].Type.ToDisplayString() != "Erinn.NetworkPeer" || parameters[0].Type.ContainingAssembly.Name != "Aspheric")
                    continue;
                var namespaceName = methodSymbol.ContainingType.ContainingNamespace.ToDisplayString();
                var fullName = methodSymbol.ContainingType.ToDisplayString();
                var methodName = methodSymbol.Name;
                var hasNamespace = HasNamespace(methodSymbol);
                if (!classCodeMap.TryGetValue(fullName, out var value))
                {
                    value = (new StringBuilder(), hasNamespace, [], []);
                    classCodeMap[fullName] = value;
                    var type = methodSymbol.ContainingType.TypeKind == TypeKind.Class ? "class" : "struct";
                    var className = methodSymbol.ContainingType.Name;
                    var codeBuilder = value.CodeBuilder;
                    codeBuilder.AppendLine("// <auto-generated/>");
                    codeBuilder.AppendLine("using Erinn;");
                    codeBuilder.AppendLine();
                    if (hasNamespace)
                    {
                        codeBuilder.AppendLine($"namespace {namespaceName}");
                        codeBuilder.AppendLine("{");
                        codeBuilder.AppendLine($"\tpartial {type} {className}");
                        codeBuilder.AppendLine("\t{");
                    }
                    else
                    {
                        codeBuilder.AppendLine($"partial {type} {className}");
                        codeBuilder.AppendLine("{");
                    }
                }

                var partialMethodCode = GeneratePartialMethodCode(sb, hasNamespace, fullName, methodName, parameters, value.Addresses, out var command);
                value.Defines.Add(command);
                value.CodeBuilder.AppendLine(partialMethodCode);
            }

            foreach (var (fullName, (codeBuilder, hasNamespace, rpcMethods, addresses)) in classCodeMap)
            {
                codeBuilder.Append(GenerateRpcInitializeMethod(sb, hasNamespace, rpcMethods, addresses));
                if (hasNamespace)
                {
                    codeBuilder.AppendLine("\t}");
                    codeBuilder.Append('}');
                }
                else
                {
                    codeBuilder.Append('}');
                }

                context.AddSource($"{fullName}.Rpc.g.cs", SourceText.From(codeBuilder.ToString(), Encoding.UTF8));
                rpcClasses.Add((fullName, hasNamespace));
            }

            GenerateRpcManager(sb, context, rpcClasses);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidType(IMethodSymbol methodSymbol) => methodSymbol.IsDefinition && methodSymbol.IsStatic && methodSymbol.ReturnType.SpecialType == SpecialType.System_Void && methodSymbol.ContainingType.TypeKind is TypeKind.Class or TypeKind.Struct;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNestedType(INamedTypeSymbol typeSymbol) => typeSymbol.ContainingType != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPartialType(INamedTypeSymbol typeSymbol)
        {
            foreach (var syntaxReference in typeSymbol.DeclaringSyntaxReferences)
            {
                var syntaxNode = syntaxReference.GetSyntax();
                if (syntaxNode is TypeDeclarationSyntax typeDeclaration)
                    return typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasNamespace(IMethodSymbol methodSymbol)
        {
            var namespaceSymbol = methodSymbol.ContainingType?.ContainingNamespace;
            return namespaceSymbol != null && !namespaceSymbol.IsGlobalNamespace;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint Hash32(byte* input, int length, uint seed = 0U)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe string GeneratePartialMethodCode(StringBuilder sb, bool hasNamespace, string fullName, string methodName, ImmutableArray<IParameterSymbol> parameters, List<string> addresses, out uint command)
        {
            var tab = hasNamespace ? "\t" : "";
            sb.Clear();
            sb.Append(fullName);
            sb.Append('.');
            sb.Append(methodName);
            for (var i = 1; i < parameters.Length; ++i)
            {
                var parameter = parameters[i];
                sb.Append(parameter.Type.ToDisplayString());
            }

            Span<char> stringBuffer = stackalloc char[sb.Length];
            sb.CopyTo(0, stringBuffer, sb.Length);
            var length = Encoding.UTF8.GetByteCount(stringBuffer);
            var buffer = stackalloc byte[length];
            Encoding.UTF8.GetBytes(stringBuffer, MemoryMarshal.CreateSpan(ref *buffer, length));
            command = Hash32(buffer, length);
            sb.Clear();
            if (parameters.Length == 1)
            {
                sb.Append($"delegate* <NetworkPeer, void> address{command} = &{methodName};");
            }
            else
            {
                sb.Append("delegate* <NetworkPeer, ");
                for (var i = 1; i < parameters.Length; ++i)
                {
                    var parameterSymbol = parameters[i];
                    var parameterType = parameterSymbol.Type;
                    if (i != parameters.Length - 1)
                        sb.Append($"{parameterType}, ");
                    else
                        sb.Append($"{parameterType}");
                }

                sb.Append($", void> address{command} = &{methodName};");
            }

            sb.AppendLine();
            sb.Append($"{tab}\t\taddressToCommand[(nint)address{command}] = {command};");
            addresses.Add(sb.ToString());
            sb.Clear();
            sb.AppendLine($"{tab}\t[_Rpc({command})]");
            sb.AppendLine($"{tab}\tprivate static void _Rpc_{command}(NetworkPeer peer, NativeStream reader)");
            sb.AppendLine($"{tab}\t{{");
            if (parameters.Length == 1)
            {
                sb.AppendLine($"{tab}\t\t{methodName}(peer);");
                sb.AppendLine($"{tab}\t}}");
                return sb.ToString();
            }

            for (var i = 1; i < parameters.Length; ++i)
            {
                var parameterSymbol = parameters[i];
                var parameterType = parameterSymbol.Type;
                sb.AppendLine($"{tab}\t\tvar arg{i} = reader.Read<{parameterType}>();");
            }

            sb.Append($"{tab}\t\t{methodName}(peer, ");
            for (var i = 1; i < parameters.Length; ++i)
            {
                if (i != parameters.Length - 1)
                    sb.Append($"arg{i}, ");
                else
                    sb.Append($"arg{i});");
            }

            sb.AppendLine();
            sb.AppendLine($"{tab}\t}}");
            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GenerateRpcInitializeMethod(StringBuilder sb, bool hasNamespace, List<uint> rpcMethods, List<string> addresses)
        {
            var tab = hasNamespace ? "\t" : "";
            sb.Clear();
            sb.AppendLine($"{tab}\t[_RpcInitialize]");
            sb.AppendLine($"{tab}\tpublic static unsafe void _Rpc_Initialize(NativeDictionary<uint, nint> commandToAddress, NativeDictionary<nint, uint> addressToCommand)");
            sb.AppendLine($"{tab}\t{{");
            sb.AppendLine($"{tab}\t\tdelegate* <NetworkPeer, NativeStream, void> address;");
            for (var i = 0; i < rpcMethods.Count; ++i)
            {
                var command = rpcMethods[i];
                sb.AppendLine($"{tab}\t\taddress = &_Rpc_{command};");
                sb.AppendLine($"{tab}\t\tcommandToAddress[{command}] = (nint)address;");
                sb.AppendLine($"{tab}\t\t{addresses[i]}");
            }

            sb.AppendLine($"{tab}\t}}");
            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateRpcManager(StringBuilder sb, SourceProductionContext context, List<(string FullName, bool HasNamespace)> rpcClasses)
        {
            sb.Clear();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("using Erinn;");
            sb.AppendLine();
            sb.AppendLine("namespace Erinn");
            sb.AppendLine("{");
            sb.AppendLine("\tpublic static class RpcManager");
            sb.AppendLine("\t{");
            sb.AppendLine("\t\tpublic static void _Initialize(NativeDictionary<uint, nint> commandToAddress, NativeDictionary<nint, uint> addressToCommand)");
            sb.AppendLine("\t\t{");
            foreach (var (fullname, hasNamespace) in rpcClasses)
            {
                if (hasNamespace)
                    sb.AppendLine($"\t\t\t{fullname}._Rpc_Initialize(commandToAddress, addressToCommand);");
                else
                    sb.AppendLine($"\t\t\tglobal::{fullname}._Rpc_Initialize(commandToAddress, addressToCommand);");
            }

            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");
            sb.Append('}');
            context.AddSource("RpcManager.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}