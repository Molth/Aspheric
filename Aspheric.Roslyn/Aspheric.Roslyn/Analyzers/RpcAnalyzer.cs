using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable RS1041
#pragma warning disable RS2008
#pragma warning disable CS8602
#pragma warning disable CS8604

// ReSharper disable InconsistentNaming
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

namespace Erinn.Roslyn
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class RpcAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor RPC003 = new("RPC003", "Method Not Defined", "The method must be defined", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC004 = new("RPC004", "Method Not Static", "The method must be a static method", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC005 = new("RPC005", "Return Type Must Be Void", "The method must have a void return type", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC006 = new("RPC006", "Invalid Containing Type", "The method must be defined in a class or struct", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC007 = new("RPC007", "Method in Nested Type", "The method cannot be defined within a nested type", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC008 = new("RPC008", "Type Not Partial", "The method must be defined in a partial type", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC009 = new("RPC009", "Invalid Method Parameters", "The first two parameters must be 'Erinn.NetworkPeer' and 'Erinn.NetworkPacketFlag'", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC010 = new("RPC010", "Invalid RefKind", "Parameter '0' must have the 'in' modifier and cannot be pointer types", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC011 = new("RPC011", "Invalid Method Parameters", "The three parameters must be 'Erinn.NetworkPeer' and 'Erinn.NetworkPacketFlag' 'Erinn.DataStream'", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC012 = new("RPC012", "Incompatible Attributes", "The method cannot have both [Rpc] and [RpcManual] attributes", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC013 = new("RPC013", "Duplicate Command", "The command is already associated with another method", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC014 = new("RPC014", "Automatically Generated Attributes Not Allowed", "Automatically generated attributes are not allowed", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC015 = new("RPC015", "Invalid Method Parameters", "The one parameter must be 'Erinn.NetworkPeer'", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC016 = new("RPC016", "Invalid Method Parameters", "The four parameters must be 'Erinn.NetworkPeer', 'Erinn.NetworkPacketFlag', 'System.Span<byte>', and 'System.Exception'", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC017 = new("RPC017", "Invalid Method Parameters", "The three parameters must be 'Erinn.NetworkPeer', 'Erinn.NetworkPacketFlag', and 'System.Span<byte>'", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC018 = new("RPC018", "Invalid Accessibility", "At most one accessibility modifier is allowed", "Erinn.Roslyn", DiagnosticSeverity.Error, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [RPC003, RPC004, RPC005, RPC006, RPC007, RPC008, RPC009, RPC010, RPC011, RPC012, RPC013, RPC014, RPC015, RPC016, RPC017, RPC018];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            var methods = new ConcurrentDictionary<uint, IMethodSymbol>();
            var stringBuilders = new ConcurrentQueue<StringBuilder>();
            context.RegisterCompilationStartAction(analysisContext => analysisContext.RegisterSymbolAction(symbolAnalysisContext => AnalyzeMethod(symbolAnalysisContext, methods, stringBuilders), SymbolKind.Method));
            context.RegisterCompilationStartAction(analysisContext => analysisContext.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AnalyzeMethod(SymbolAnalysisContext context, ConcurrentDictionary<uint, IMethodSymbol> methods, ConcurrentQueue<StringBuilder> stringBuilders)
        {
            var methodSymbol = (IMethodSymbol)context.Symbol;
            var state = FindAttributes(methodSymbol, out var command);
            switch (state)
            {
                case MethodFlag.NotFound:
                    return;
                case MethodFlag.AutoGenerated:
                    ReportDiagnostic(context, methodSymbol, RPC014);
                    return;
                case MethodFlag.RpcAndRpcManual:
                    ReportDiagnostic(context, methodSymbol, RPC012);
                    return;
            }

            if (!methodSymbol.IsDefinition)
            {
                ReportDiagnostic(context, methodSymbol, RPC003);
                return;
            }

            if (!methodSymbol.IsStatic)
            {
                ReportDiagnostic(context, methodSymbol, RPC004);
                return;
            }

            if (methodSymbol.ReturnType.SpecialType != SpecialType.System_Void)
            {
                ReportDiagnostic(context, methodSymbol, RPC005);
                return;
            }

            if (methodSymbol.ContainingType.TypeKind is not (TypeKind.Class or TypeKind.Struct))
            {
                ReportDiagnostic(context, methodSymbol, RPC006);
                return;
            }

            if (methodSymbol.ContainingType.ContainingType != null)
            {
                ReportDiagnostic(context, methodSymbol, RPC007);
                return;
            }

            ImmutableArray<IParameterSymbol> parameters;
            switch (state)
            {
                case MethodFlag.Rpc:
                    if (!RpcHelpers.IsPartialType(methodSymbol.ContainingType))
                    {
                        ReportDiagnostic(context, methodSymbol, RPC008);
                        return;
                    }

                    parameters = methodSymbol.Parameters;
                    if (parameters.Length < 2 || parameters[0].Type.ToDisplayString() != "Erinn.NetworkPeer" || parameters[0].Type.ContainingAssembly.Name != "Aspheric" || parameters[1].Type.ToDisplayString() != "Erinn.NetworkPacketFlag" || parameters[1].Type.ContainingAssembly.Name != "Aspheric")
                    {
                        ReportDiagnostic(context, methodSymbol, RPC009);
                        return;
                    }

                    for (var j = 0; j < parameters.Length; ++j)
                    {
                        var parameter = parameters[j];
                        if (!RpcHelpers.IsValidRefKind(parameter))
                        {
                            ReportDiagnostic(context, methodSymbol, RPC010);
                            return;
                        }
                    }

                    if (!stringBuilders.TryDequeue(out var sb))
                        sb = new StringBuilder();
                    else
                        sb.Clear();
                    sb.Append(methodSymbol.ContainingType.ToDisplayString());
                    sb.Append('.');
                    sb.Append(methodSymbol.Name);
                    for (var i = 2; i < parameters.Length; ++i)
                        sb.Append(parameters[i].Type.ToDisplayString());
                    command = RpcHelpers.Hash32(sb);
                    stringBuilders.Enqueue(sb);
                    break;
                case MethodFlag.RpcManual:
                    parameters = methodSymbol.Parameters;
                    if (!RpcHelpers.IsValidRpcManualParameters(parameters))
                        ReportDiagnostic(context, methodSymbol, RPC011);
                    break;
                case MethodFlag.OnConnectedAndOnDisconnected:
                case MethodFlag.OnConnected:
                case MethodFlag.OnDisconnected:
                    parameters = methodSymbol.Parameters;
                    if (!RpcHelpers.IsValidConnectedDisconnected(parameters))
                        ReportDiagnostic(context, methodSymbol, RPC015);
                    return;
                case MethodFlag.OnErrored:
                    parameters = methodSymbol.Parameters;
                    if (!RpcHelpers.IsValidErrored(parameters))
                        ReportDiagnostic(context, methodSymbol, RPC016);
                    return;
                case MethodFlag.OnReceived:
                    parameters = methodSymbol.Parameters;
                    if (!RpcHelpers.IsValidReceived(parameters))
                        ReportDiagnostic(context, methodSymbol, RPC017);
                    return;
            }

            if (methods.TryGetValue(command, out var method))
            {
                ReportDiagnostic(context, methodSymbol, RPC013);
                ReportDiagnostic(context, method, RPC013);
                return;
            }

            methods[command] = methodSymbol;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AnalyzeType(SymbolAnalysisContext context)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
            if (namedTypeSymbol.TypeKind is not (TypeKind.Class or TypeKind.Struct))
                return;
            var target = RpcHelpers.GetRpcServiceTarget(namedTypeSymbol);
            if (!RpcHelpers.IsValidAccessibility(target))
            {
                ReportDiagnostic(context, namedTypeSymbol, RPC018);
                return;
            }

            var attributes = namedTypeSymbol.GetAttributes();
            for (var i = 0; i < attributes.Length; ++i)
            {
                var attribute = attributes[i];
                if (attribute.AttributeClass.ContainingAssembly.Name == "Aspheric")
                {
                    if (attribute.AttributeClass?.ToDisplayString() == "Erinn._RpcServiceAttribute")
                    {
                        ReportDiagnostic(context, namedTypeSymbol, RPC014);
                        return;
                    }

                    if (attribute.AttributeClass?.ToDisplayString() == "Erinn.RpcServiceAttribute")
                    {
                        if (namedTypeSymbol.ContainingType != null)
                            ReportDiagnostic(context, namedTypeSymbol, RPC007);
                        return;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static MethodFlag FindAttributes(IMethodSymbol methodSymbol, out uint command)
        {
            var attributes = methodSymbol.GetAttributes();
            var foundRpc = false;
            var foundRpcManual = false;
            var foundOnConnected = false;
            var foundOnDisconnected = false;
            var foundOnErrored = false;
            var foundOnReceived = false;
            command = 0;
            for (var i = 0; i < attributes.Length; ++i)
            {
                var attribute = attributes[i];
                if (attribute.AttributeClass.ContainingAssembly.Name == "Aspheric")
                {
                    if (attribute.AttributeClass?.ToDisplayString() is "Erinn._RpcAttribute" or "Erinn._RpcInitializeAttribute" or "Erinn._RpcDeinitializeAttribute")
                        return MethodFlag.AutoGenerated;
                    if (!foundRpc && attribute.AttributeClass?.ToDisplayString() == "Erinn.RpcAttribute")
                        foundRpc = true;
                    if (!foundRpcManual && attribute.AttributeClass?.ToDisplayString() == "Erinn.RpcManualAttribute")
                    {
                        foundRpcManual = true;
                        command = uint.Parse(attribute.ConstructorArguments[0].Value.ToString());
                    }

                    if (!foundOnConnected && attribute.AttributeClass?.ToDisplayString() == "Erinn.OnConnectedAttribute")
                        foundOnConnected = true;
                    if (!foundOnDisconnected && attribute.AttributeClass?.ToDisplayString() == "Erinn.OnDisconnectedAttribute")
                        foundOnDisconnected = true;
                    if (!foundOnErrored && attribute.AttributeClass?.ToDisplayString() == "Erinn.OnErroredAttribute")
                        foundOnErrored = true;
                    if (!foundOnReceived && attribute.AttributeClass?.ToDisplayString() == "Erinn.OnReceivedAttribute")
                        foundOnReceived = true;
                }
            }

            MethodFlag state;
            if (foundRpc && foundRpcManual)
                state = MethodFlag.RpcAndRpcManual;
            else if (foundRpc)
                state = MethodFlag.Rpc;
            else if (foundRpcManual)
                state = MethodFlag.RpcManual;
            else if (foundOnConnected && foundOnDisconnected)
                state = MethodFlag.OnConnectedAndOnDisconnected;
            else if (foundOnConnected)
                state = MethodFlag.OnConnected;
            else if (foundOnDisconnected)
                state = MethodFlag.OnDisconnected;
            else if (foundOnErrored)
                state = MethodFlag.OnErrored;
            else if (foundOnReceived)
                state = MethodFlag.OnReceived;
            else
                state = MethodFlag.NotFound;
            return state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReportDiagnostic(SymbolAnalysisContext context, ISymbol symbol, DiagnosticDescriptor descriptor)
        {
            var diagnostic = Diagnostic.Create(descriptor, symbol.Locations[0]);
            context.ReportDiagnostic(diagnostic);
        }
    }
}