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
#pragma warning disable CS8605

namespace Erinn
{
    [Generator]
    internal sealed class RpcIncrementalSourceGenerator : IIncrementalGenerator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var methodsWithAttribute = context.SyntaxProvider.CreateSyntaxProvider(static (node, _) => node is MethodDeclarationSyntax method && IsAnnotatedWithRpcAttribute(method), static (ctx, _) => GetMethodDeclarationForSourceGen(ctx)).Where(t => t.Found).Select((t, _) => (t.Method, t.DeclaredAccessibility));
            context.RegisterSourceOutput(context.CompilationProvider.Combine(methodsWithAttribute.Collect()), static (ctx, t) => GenerateCode(ctx, t.Left, t.Right));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAnnotatedWithRpcAttribute(MethodDeclarationSyntax method) => method.AttributeLists.Any(s => s.Attributes.Any(a => a.Name.ToString() == "Rpc"));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (MethodDeclarationSyntax Method, Accessibility DeclaredAccessibility, bool Found) GetMethodDeclarationForSourceGen(GeneratorSyntaxContext context)
        {
            var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;
            for (var i = 0; i < methodDeclarationSyntax.AttributeLists.Count; ++i)
            {
                var attributeListSyntax = methodDeclarationSyntax.AttributeLists[i];
                for (var j = 0; j < attributeListSyntax.Attributes.Count; ++j)
                {
                    var attributeSyntax = attributeListSyntax.Attributes[j];
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                        continue;
                    var attributeName = attributeSymbol.ContainingType.ToDisplayString();
                    if (attributeName == "Erinn.RpcAttribute" && attributeSymbol.ContainingAssembly.Name == "Aspheric")
                    {
                        var declaredAccessibility = 0;
                        if (attributeSyntax.ArgumentList?.Arguments.Count == 1)
                            declaredAccessibility = (int)context.SemanticModel.GetConstantValue(attributeSyntax.ArgumentList.Arguments[0].Expression).Value;
                        return (methodDeclarationSyntax, (Accessibility)declaredAccessibility, true);
                    }
                }
            }

            return (methodDeclarationSyntax, Accessibility.NotApplicable, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateCode(SourceProductionContext context, Compilation compilation, ImmutableArray<(MethodDeclarationSyntax, Accessibility)> methodDeclarations)
        {
            var sb = new StringBuilder(1024);
            var classCodeMap = new Dictionary<string, (StringBuilder CodeBuilder, bool HasNamespace, List<uint> Commands, List<string> Addresses)>();
            var rpcClasses = new List<(string FullName, bool HasNamespace)>();
            var rpcMethodCount = 0;
            for (var i = 0; i < methodDeclarations.Length; ++i)
            {
                var methodDeclarationSyntax = methodDeclarations[i].Item1;
                var declaredAccessibility = methodDeclarations[i].Item2;
                var semanticModel = compilation.GetSemanticModel(methodDeclarationSyntax.SyntaxTree);
                var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                if (methodSymbol is null || !IsValidType(methodSymbol) || IsNestedType(methodSymbol.ContainingType) || !IsPartialType(methodSymbol.ContainingType))
                    continue;
                var parameters = methodSymbol.Parameters;
                if (parameters.Length < 2 || parameters[0].Type.ToDisplayString() != "Erinn.NetworkPeer" || parameters[0].Type.ContainingAssembly.Name != "Aspheric" || parameters[1].Type.ToDisplayString() != "Erinn.NetworkPacketFlag" || parameters[1].Type.ContainingAssembly.Name != "Aspheric")
                    continue;
                for (var j = 0; j < parameters.Length; ++j)
                {
                    if (!IsValidRefKind(parameters[j]))
                        goto next;
                }

                goto label;
                next:
                continue;
                label:
                var namespaceName = methodSymbol.ContainingType.ContainingNamespace.ToDisplayString();
                var fullName = methodSymbol.ContainingType.ToDisplayString();
                var methodName = methodSymbol.Name;
                var hasNamespace = HasNamespace(methodSymbol);
                if (!classCodeMap.TryGetValue(fullName, out var value))
                {
                    value = (new StringBuilder(), HasNamespace: hasNamespace, [], []);
                    classCodeMap[fullName] = value;
                    var type = methodSymbol.ContainingType.TypeKind == TypeKind.Class ? "class" : "struct";
                    var className = methodSymbol.ContainingType.Name;
                    var codeBuilder = value.CodeBuilder;
                    codeBuilder.AppendLine("// <auto-generated/>");
                    codeBuilder.AppendLine("using System.Runtime.CompilerServices;");
                    if (namespaceName != "Erinn")
                        codeBuilder.AppendLine("using Erinn;");
                    codeBuilder.AppendLine();
                    if (hasNamespace)
                    {
                        codeBuilder.AppendLine($"namespace {namespaceName}");
                        codeBuilder.AppendLine("{");
                        codeBuilder.AppendLine($"\tunsafe partial {type} {className}");
                        codeBuilder.AppendLine("\t{");
                    }
                    else
                    {
                        codeBuilder.AppendLine($"partial {type} {className}");
                        codeBuilder.AppendLine("{");
                    }
                }

                rpcMethodCount++;
                var partialMethodCode = GeneratePartialMethodCode(sb, hasNamespace, fullName, methodName, declaredAccessibility, parameters, value.Addresses, out var command);
                value.Commands.Add(command);
                value.CodeBuilder.AppendLine(partialMethodCode);
            }

            foreach (var (fullName, (codeBuilder, hasNamespace, commands, addresses)) in classCodeMap)
            {
                codeBuilder.Append(GenerateRpcInitializeMethod(sb, hasNamespace, commands, addresses));
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

            GenerateAssemblyRpc(compilation.Assembly.Name, sb, rpcMethodCount, context, rpcClasses);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidRefKind(IParameterSymbol parameterSymbol) => parameterSymbol.GetType() != typeof(IPointerTypeSymbol) && parameterSymbol.RefKind == RefKind.In && !parameterSymbol.Type.IsRefLikeType;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidType(IMethodSymbol methodSymbol) => methodSymbol.IsDefinition && methodSymbol.IsStatic && methodSymbol.ReturnType.SpecialType == SpecialType.System_Void && methodSymbol.ContainingType.TypeKind is TypeKind.Class or TypeKind.Struct;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNestedType(INamedTypeSymbol typeSymbol) => typeSymbol.ContainingType != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPartialType(INamedTypeSymbol typeSymbol)
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
        private static unsafe string GeneratePartialMethodCode(StringBuilder sb, bool hasNamespace, string fullName, string methodName, Accessibility declaredAccessibility, ImmutableArray<IParameterSymbol> parameters, List<string> addresses, out uint command)
        {
            var tab = hasNamespace ? "\t" : "";
            sb.Clear();
            sb.Append(fullName);
            sb.Append('.');
            sb.Append(methodName);
            for (var i = 2; i < parameters.Length; ++i)
                sb.Append(parameters[i].Type.ToDisplayString());
            Span<char> chars = stackalloc char[sb.Length];
            sb.CopyTo(0, chars, sb.Length);
            var length = Encoding.UTF8.GetByteCount(chars);
            var buffer = stackalloc byte[length];
            Encoding.UTF8.GetBytes(chars, MemoryMarshal.CreateSpan(ref *buffer, length));
            command = Hash32(buffer, length);
            sb.Clear();
            if (parameters.Length == 2)
            {
                sb.Append($"delegate* managed<in NetworkPeer, in NetworkPacketFlag, void> address{command} = &{methodName};");
            }
            else
            {
                sb.Append("delegate* managed<in NetworkPeer, in NetworkPacketFlag, ");
                for (var i = 2; i < parameters.Length; ++i)
                {
                    var parameterSymbol = parameters[i];
                    var parameterType = parameterSymbol.Type;
                    if (i != parameters.Length - 1)
                        sb.Append($"in {parameterType}, ");
                    else
                        sb.Append($"in {parameterType}");
                }

                sb.Append($", void> address{command} = &{methodName};");
            }

            addresses.Add(sb.ToString());
            sb.Clear();
            string? accessibility = null;
            switch (declaredAccessibility)
            {
                case Accessibility.NotApplicable:
                    break;
                case Accessibility.Private:
                    accessibility = "private";
                    break;
                case Accessibility.ProtectedAndInternal:
                    accessibility = "private protected";
                    break;
                case Accessibility.Protected:
                    accessibility = "protected";
                    break;
                case Accessibility.Internal:
                    accessibility = "internal";
                    break;
                case Accessibility.ProtectedOrInternal:
                    accessibility = "protected internal";
                    break;
                case Accessibility.Public:
                    accessibility = "public";
                    break;
            }

            if (accessibility != null)
            {
                sb.AppendLine($"{tab}\t[_Rpc({command})]");
                if (parameters.Length == 2)
                {
                    sb.Append($"{tab}\t{accessibility} static delegate* managed<in NetworkPeer, in NetworkPacketFlag, void> {methodName}_Rpc_{command} => &{methodName};");
                }
                else
                {
                    sb.Append($"{tab}\t{accessibility} static delegate* managed<in NetworkPeer, in NetworkPacketFlag, ");
                    for (var i = 2; i < parameters.Length; ++i)
                    {
                        var parameterSymbol = parameters[i];
                        var parameterType = parameterSymbol.Type;
                        if (i != parameters.Length - 1)
                            sb.Append($"in {parameterType}, ");
                        else
                            sb.Append($"in {parameterType}, void> {methodName}_Rpc_{command} => &{methodName};");
                    }
                }

                sb.AppendLine();
                sb.AppendLine();
            }

            sb.AppendLine($"{tab}\t[_Rpc({command})]");
            sb.AppendLine($"{tab}\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{tab}\tprivate static void _Rpc_{command}(in NetworkPeer peer, in NetworkPacketFlag flags, in DataStream stream)");
            sb.AppendLine($"{tab}\t{{");
            if (parameters.Length == 2)
            {
                sb.AppendLine($"{tab}\t\t{methodName}(peer, flags);");
                sb.AppendLine($"{tab}\t}}");
                return sb.ToString();
            }

            for (var i = 2; i < parameters.Length; ++i)
            {
                var parameterSymbol = parameters[i];
                var parameterType = parameterSymbol.Type;
                sb.AppendLine($"{tab}\t\tvar arg{i - 2} = stream.Read<{parameterType}>();");
            }

            sb.Append($"{tab}\t\t{methodName}(peer, flags, ");
            for (var i = 2; i < parameters.Length; ++i)
            {
                if (i != parameters.Length - 1)
                    sb.Append($"arg{i - 2}, ");
                else
                    sb.Append($"arg{i - 2});");
            }

            sb.AppendLine();
            sb.AppendLine($"{tab}\t}}");
            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GenerateRpcInitializeMethod(StringBuilder sb, bool hasNamespace, List<uint> commands, List<string> addresses)
        {
            var tab = hasNamespace ? "\t" : "";
            sb.Clear();
            sb.AppendLine($"{tab}\t[_RpcInitialize({commands.Count})]");
            sb.AppendLine($"{tab}\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{tab}\tpublic static void _Rpc_Initialize(in RpcMethods rpcMethods)");
            sb.AppendLine($"{tab}\t{{");
            sb.AppendLine($"{tab}\t\tdelegate* managed<in NetworkPeer, in NetworkPacketFlag, in DataStream, void> address;");
            for (var i = 0; i < commands.Count; ++i)
            {
                var command = commands[i];
                sb.AppendLine($"{tab}\t\taddress = &_Rpc_{command};");
                sb.AppendLine($"{tab}\t\trpcMethods.AddCommand({command}, address);");
                sb.AppendLine($"{tab}\t\t{addresses[i]}");
                sb.AppendLine($"{tab}\t\trpcMethods.AddAddress((nint)address{commands[i]}, {commands[i]});");
            }

            sb.AppendLine($"{tab}\t}}");
            sb.AppendLine();
            sb.AppendLine($"{tab}\t[_RpcDeinitialize({commands.Count})]");
            sb.AppendLine($"{tab}\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{tab}\tpublic static void _Rpc_Deinitialize(in RpcMethods rpcMethods)");
            sb.AppendLine($"{tab}\t{{");
            for (var i = 0; i < commands.Count; ++i)
            {
                var command = commands[i];
                sb.AppendLine($"{tab}\t\trpcMethods.RemoveCommand({command});");
                sb.AppendLine($"{tab}\t\t{addresses[i]}");
                sb.AppendLine($"{tab}\t\trpcMethods.RemoveAddress((nint)address{commands[i]});");
            }

            sb.AppendLine($"{tab}\t}}");
            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateAssemblyRpc(string assembly, StringBuilder sb, int rpcMethodCount, SourceProductionContext context, List<(string FullName, bool HasNamespace)> rpcClasses)
        {
            var name = $"_RpcService_{assembly}";
            sb.Clear();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("using System.Runtime.CompilerServices;");
            sb.AppendLine();
            sb.AppendLine("namespace Erinn");
            sb.AppendLine("{");
            sb.AppendLine($"\t[_RpcService({rpcMethodCount})]");
            sb.AppendLine($"\tpublic static class {name}");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\tpublic const int RPC_METHOD_COUNT = {rpcMethodCount};");
            sb.AppendLine();
            sb.AppendLine("\t\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine("\t\tpublic static void _Initialize(in RpcMethods rpcMethods)");
            sb.AppendLine("\t\t{");
            for (var i = 0; i < rpcClasses.Count; ++i)
            {
                var (fullname, hasNamespace) = rpcClasses[i];
                if (hasNamespace)
                    sb.AppendLine($"\t\t\t{fullname}._Rpc_Initialize(rpcMethods);");
                else
                    sb.AppendLine($"\t\t\tglobal::{fullname}._Rpc_Initialize(rpcMethods);");
            }

            sb.AppendLine("\t\t}");
            sb.AppendLine();
            sb.AppendLine("\t\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine("\t\tpublic static void _Deinitialize(in RpcMethods rpcMethods)");
            sb.AppendLine("\t\t{");
            for (var i = 0; i < rpcClasses.Count; ++i)
            {
                var (fullname, hasNamespace) = rpcClasses[i];
                if (hasNamespace)
                    sb.AppendLine($"\t\t\t{fullname}._Rpc_Deinitialize(rpcMethods);");
                else
                    sb.AppendLine($"\t\t\tglobal::{fullname}._Rpc_Deinitialize(rpcMethods);");
            }

            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");
            sb.Append('}');
            context.AddSource($"{name}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}