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
#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8605

// ReSharper disable ConvertIfStatementToSwitchStatement

namespace Erinn.Roslyn
{
    [Generator(LanguageNames.CSharp)]
    internal sealed class RpcIncrementalSourceGenerator : IIncrementalGenerator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(IncrementalGeneratorInitializationContext context) => context.RegisterImplementationSourceOutput(context.CompilationProvider.Combine(context.SyntaxProvider.CreateSyntaxProvider(static (node, _) => node is MethodDeclarationSyntax method && IsAnnotatedWithRpcAttribute(method), static (ctx, _) => GetRpcMethodDeclarationForSourceGen(ctx)).Where(t => t.Data.State != RpcState.NotFound).Select((t, _) => (t.Method, t.Data)).Collect()), static (ctx, t) => GenerateCode(ctx, t.Item1, t.Item2));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAnnotatedWithRpcAttribute(MethodDeclarationSyntax method) => method.AttributeLists.Any(s => s.Attributes.Any(a => a.Name.ToString() is "Rpc" or "RpcAttribute" or "Erinn.Rpc" or "Erinn.RpcAttribute" or "RpcManual" or "RpcManualAttribute" or "Erinn.RpcManual" or "Erinn.RpcManualAttribute"));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (MethodDeclarationSyntax Method, RpcData Data) GetRpcMethodDeclarationForSourceGen(GeneratorSyntaxContext context)
        {
            var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;
            var foundRpc = false;
            var foundRpcManual = false;
            var data = new RpcData();
            for (var i = 0; i < methodDeclarationSyntax.AttributeLists.Count; ++i)
            {
                var attributeListSyntax = methodDeclarationSyntax.AttributeLists[i];
                for (var j = 0; j < attributeListSyntax.Attributes.Count; ++j)
                {
                    var attributeSyntax = attributeListSyntax.Attributes[j];
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                        continue;
                    var attributeName = attributeSymbol.ContainingType.ToDisplayString();
                    if (attributeSymbol.ContainingAssembly.Name == "Aspheric")
                    {
                        if (!foundRpc && attributeName == "Erinn.RpcAttribute")
                        {
                            foundRpc = true;
                            var declaredAccessibility = 0;
                            if (attributeSyntax.ArgumentList?.Arguments.Count == 1)
                                declaredAccessibility = int.Parse(context.SemanticModel.GetConstantValue(attributeSyntax.ArgumentList.Arguments[0].Expression).Value.ToString());
                            data.DeclaredAccessibility = (Accessibility)declaredAccessibility;
                        }

                        if (!foundRpcManual && attributeName == "Erinn.RpcManualAttribute")
                        {
                            foundRpcManual = true;
                            uint command = 0;
                            if (attributeSyntax.ArgumentList?.Arguments.Count == 1)
                                command = uint.Parse(context.SemanticModel.GetConstantValue(attributeSyntax.ArgumentList.Arguments[0].Expression).Value.ToString());
                            data.Command = command;
                        }
                    }
                }
            }

            data.State = foundRpc && foundRpcManual ? RpcState.Both : foundRpc ? RpcState.Rpc : foundRpcManual ? RpcState.RpcManual : RpcState.NotFound;
            return (methodDeclarationSyntax, data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateCode(SourceProductionContext context, Compilation compilation, ImmutableArray<(MethodDeclarationSyntax, RpcData)> methodDeclarations)
        {
            if (!((CSharpCompilation)compilation).Options.AllowUnsafe)
            {
                ReportDiagnostic(context, "RPC001", "UnsafeBlocks Not Allowed", $"Unsafe blocks is not allowed in this project: [{compilation.Assembly.ToDisplayString()}].", "Erinn.Roslyn", DiagnosticSeverity.Error, Location.None);
                return;
            }

            var methods = new Dictionary<uint, MethodDeclarationSyntax>(0);
            var sb = new StringBuilder(1024);
            var classCodeMap = new Dictionary<string, (StringBuilder CodeBuilder, bool HasNamespace, List<uint> Commands, List<string> Addresses, List<(string, uint)>ManualCommands)>();
            var rpcClasses = new List<(string FullName, bool HasNamespace)>();
            var rpcMethodCount = 0;
            for (var i = 0; i < methodDeclarations.Length; ++i)
            {
                var (methodDeclarationSyntax, data) = methodDeclarations[i];
                if (data.State == RpcState.Both)
                {
                    ReportDiagnostic(context, "RPC012", "Incompatible Attributes", "The method cannot have both [Rpc] and [RpcManual] attributes.", "Erinn.Roslyn", DiagnosticSeverity.Error, methodDeclarationSyntax.GetLocation());
                    continue;
                }

                var semanticModel = compilation.GetSemanticModel(methodDeclarationSyntax.SyntaxTree);
                var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                if (methodSymbol == null)
                {
                    ReportDiagnostic(context, "RPC002", "Method Symbol Not Found", "The method symbol is null, which means the method could not be resolved.", "Erinn.Roslyn", DiagnosticSeverity.Error, methodDeclarationSyntax.GetLocation());
                    continue;
                }

                if (!methodSymbol.IsDefinition)
                {
                    ReportDiagnostic(context, "RPC003", "Method Not Defined", "The method must be defined.", "Erinn.Roslyn", DiagnosticSeverity.Error, methodDeclarationSyntax.GetLocation());
                    continue;
                }

                if (!methodSymbol.IsStatic)
                {
                    ReportDiagnostic(context, "RPC004", "Method Not Static", "The method must be a static method.", "Erinn.Roslyn", DiagnosticSeverity.Error, methodDeclarationSyntax.GetLocation());
                    continue;
                }

                if (methodSymbol.ReturnType.SpecialType != SpecialType.System_Void)
                {
                    ReportDiagnostic(context, "RPC005", "Return Type Must Be Void", "The method must have a void return type.", "Erinn.Roslyn", DiagnosticSeverity.Error, methodDeclarationSyntax.GetLocation());
                    continue;
                }

                if (methodSymbol.ContainingType.TypeKind is not (TypeKind.Class or TypeKind.Struct))
                {
                    ReportDiagnostic(context, "RPC006", "Invalid Containing Type", "The method must be defined in a class or struct.", "Erinn.Roslyn", DiagnosticSeverity.Error, methodDeclarationSyntax.GetLocation());
                    continue;
                }

                ImmutableArray<IParameterSymbol> parameters;
                if (data.State == RpcState.Rpc)
                {
                    if (methodSymbol.ContainingType.ContainingType != null)
                    {
                        ReportDiagnostic(context, "RPC007", "Method in Nested Type", "The method cannot be defined within a nested type.", "Erinn.Roslyn", DiagnosticSeverity.Error, methodDeclarationSyntax.GetLocation());
                        continue;
                    }

                    if (!RpcHelpers.IsPartialType(methodSymbol.ContainingType))
                    {
                        ReportDiagnostic(context, "RPC008", "Type Not Partial", "The method must be defined in a partial type.", "Erinn.Roslyn", DiagnosticSeverity.Error, methodDeclarationSyntax.GetLocation());
                        continue;
                    }

                    parameters = methodSymbol.Parameters;
                    if (parameters.Length < 2 || parameters[0].Type.ToDisplayString() != "Erinn.NetworkPeer" || parameters[0].Type.ContainingAssembly.Name != "Aspheric" || parameters[1].Type.ToDisplayString() != "Erinn.NetworkPacketFlag" || parameters[1].Type.ContainingAssembly.Name != "Aspheric")
                    {
                        ReportDiagnostic(context, "RPC009", "Invalid Method Parameters", "The first two parameters must be 'Erinn.NetworkPeer' and 'Erinn.NetworkPacketFlag' from the 'Aspheric' assembly.", "Erinn.Roslyn", DiagnosticSeverity.Error, methodDeclarationSyntax.GetLocation());
                        continue;
                    }

                    for (var j = 0; j < parameters.Length; ++j)
                    {
                        var parameter = parameters[j];
                        if (!RpcHelpers.IsValidRefKind(parameter))
                        {
                            Location? location = null;
                            if (parameter.Locations != null)
                                location = parameter.Locations.FirstOrDefault();
                            if (location == null)
                                location = methodDeclarationSyntax.GetLocation();
                            ReportDiagnostic(context, "RPC010", "Invalid RefKind", $"Parameter '{parameter.Name}' must have the 'in' modifier and cannot be pointer types.", "Erinn.Roslyn", DiagnosticSeverity.Error, location);
                            goto next;
                        }
                    }
                }
                else
                {
                    parameters = methodSymbol.Parameters;
                    if (!RpcHelpers.IsValidRpcManualParameters(parameters))
                    {
                        ReportDiagnostic(context, "RPC011", "Invalid Method Parameters", "The three parameters must be 'Erinn.NetworkPeer' and 'Erinn.NetworkPacketFlag' 'Erinn.DataStream' and from the 'Aspheric' assembly.", "Erinn.Roslyn", DiagnosticSeverity.Error, methodDeclarationSyntax.GetLocation());
                        goto next;
                    }
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
                    value = (new StringBuilder(), HasNamespace: hasNamespace, [], [], []);
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
                if (data.State == RpcState.Rpc)
                {
                    var partialMethodCode = GenerateRpcMethodCode(sb, hasNamespace, fullName, methodName, data.DeclaredAccessibility, parameters, value.Addresses, out var command);
                    if (methods.TryGetValue(command, out var method))
                    {
                        ReportDiagnostic(context, "RPC013", "Duplicate Command", "The command is already associated with another method.", "Erinn.Roslyn", DiagnosticSeverity.Error, methodDeclarationSyntax.GetLocation());
                        ReportDiagnostic(context, "RPC013", "Duplicate Command", "The command is already associated with another method.", "Erinn.Roslyn", DiagnosticSeverity.Error, method.GetLocation());
                        continue;
                    }

                    methods[command] = methodDeclarationSyntax;
                    value.Commands.Add(command);
                    value.CodeBuilder.AppendLine(partialMethodCode);
                }
                else
                {
                    var command = data.Command;
                    if (methods.TryGetValue(command, out var method))
                    {
                        ReportDiagnostic(context, "RPC013", "Duplicate Command", "The command is already associated with another method.", "Erinn.Roslyn", DiagnosticSeverity.Error, methodDeclarationSyntax.GetLocation());
                        ReportDiagnostic(context, "RPC013", "Duplicate Command", "The command is already associated with another method.", "Erinn.Roslyn", DiagnosticSeverity.Error, method.GetLocation());
                        continue;
                    }

                    methods[command] = methodDeclarationSyntax;
                    value.ManualCommands.Add((methodName, command));
                }
            }

            foreach (var (fullName, (codeBuilder, hasNamespace, commands, addresses, manualCommands)) in classCodeMap)
            {
                codeBuilder.Append(GenerateRpcInitializeMethod(sb, hasNamespace, commands, addresses, manualCommands));
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
        private static void ReportDiagnostic(SourceProductionContext context, string id, string title, string messageFormat, string category, DiagnosticSeverity defaultSeverity, Location location)
        {
            var diagnostic = Diagnostic.Create(new DiagnosticDescriptor(id, title, messageFormat, category, defaultSeverity, true), location);
            context.ReportDiagnostic(diagnostic);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasNamespace(IMethodSymbol methodSymbol)
        {
            var namespaceSymbol = methodSymbol.ContainingType?.ContainingNamespace;
            return namespaceSymbol != null && !namespaceSymbol.IsGlobalNamespace;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GenerateRpcMethodCode(StringBuilder sb, bool hasNamespace, string fullName, string methodName, Accessibility declaredAccessibility, ImmutableArray<IParameterSymbol> parameters, List<string> addresses, out uint command)
        {
            var tab = hasNamespace ? "\t" : "";
            sb.Clear();
            sb.Append(fullName);
            sb.Append('.');
            sb.Append(methodName);
            for (var i = 2; i < parameters.Length; ++i)
                sb.Append(parameters[i].Type.ToDisplayString());
            command = RpcHelpers.Hash32(sb);
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
        private static string GenerateRpcInitializeMethod(StringBuilder sb, bool hasNamespace, List<uint> commands, List<string> addresses, List<(string, uint)> manualCommands)
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

            for (var i = 0; i < manualCommands.Count; ++i)
            {
                var (methodName, command) = manualCommands[i];
                sb.AppendLine($"{tab}\t\taddress = &{methodName};");
                sb.AppendLine($"{tab}\t\trpcMethods.AddCommand({command}, address);");
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

            for (var i = 0; i < manualCommands.Count; ++i)
            {
                var (_, command) = manualCommands[i];
                sb.AppendLine($"{tab}\t\trpcMethods.RemoveCommand({command});");
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

        [StructLayout(LayoutKind.Explicit)]
        private struct RpcData
        {
            [FieldOffset(0)] public RpcState State;
            [FieldOffset(4)] public uint Command;
            [FieldOffset(4)] public Accessibility DeclaredAccessibility;
        }
    }
}