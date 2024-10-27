using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable RS1041
#pragma warning disable RS2008

// ReSharper disable InconsistentNaming
// ReSharper disable ConvertIfStatementToSwitchStatement

namespace Erinn
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class RpcManualAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor RPC003 = new("RPC003", "Method Not Defined", "The method must be defined", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC004 = new("RPC004", "Method Not Static", "The method must be a static method", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC005 = new("RPC005", "Return Type Must Be Void", "The method must have a void return type", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC006 = new("RPC006", "Invalid Containing Type", "The method must be defined in a class or struct", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC007 = new("RPC007", "Method in Nested Type", "The method cannot be defined within a nested type", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC008 = new("RPC008", "Type Not Partial", "The method must be defined in a partial type", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC009 = new("RPC009", "Invalid Method Parameters", "The first two parameters must be 'Erinn.NetworkPeer' and 'Erinn.NetworkPacketFlag' from the 'Aspheric' assembly", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC010 = new("RPC010", "Invalid RefKind", "Parameter '0' must have the 'in' modifier and cannot be pointer types", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC011 = new("RPC011", "Invalid Method Parameters", "The three parameters must be 'Erinn.NetworkPeer' and 'Erinn.NetworkPacketFlag' 'Erinn.DataStream' and from the 'Aspheric' assembly", "Erinn.Roslyn", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor RPC012 = new("RPC012", "Incompatible Attributes", "The method cannot have both [Rpc] and [RpcManual] attributes", "Erinn.Roslyn", DiagnosticSeverity.Error, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [RPC003, RPC004, RPC005, RPC006, RPC007, RPC008, RPC009, RPC010, RPC011, RPC012];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var methodSymbol = (IMethodSymbol)context.Symbol;
            var hasRpcAttribute = HasRpcAttribute(methodSymbol);
            var hasRpcManualAttribute = HasRpcManualAttribute(methodSymbol);
            if (hasRpcAttribute && hasRpcManualAttribute)
            {
                ReportDiagnostic(context, methodSymbol, RPC012);
                return;
            }

            if (hasRpcAttribute)
            {
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

                if (!IsPartialType(methodSymbol.ContainingType))
                {
                    ReportDiagnostic(context, methodSymbol, RPC008);
                    return;
                }

                var parameters = methodSymbol.Parameters;
                if (parameters.Length < 2 || parameters[0].Type.ToDisplayString() != "Erinn.NetworkPeer" || parameters[0].Type.ContainingAssembly.Name != "Aspheric" || parameters[1].Type.ToDisplayString() != "Erinn.NetworkPacketFlag" || parameters[1].Type.ContainingAssembly.Name != "Aspheric")
                {
                    ReportDiagnostic(context, methodSymbol, RPC009);
                    return;
                }

                for (var j = 0; j < parameters.Length; ++j)
                {
                    var parameter = parameters[j];
                    if (!IsValidRefKind(parameter))
                    {
                        ReportDiagnostic(context, methodSymbol, RPC010);
                        return;
                    }
                }
            }
            else if (hasRpcManualAttribute)
            {
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

                if (!AreParametersValid(methodSymbol))
                    ReportDiagnostic(context, methodSymbol, RPC011);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasRpcAttribute(IMethodSymbol methodSymbol) => methodSymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Erinn.RpcAttribute" && a.AttributeClass?.ContainingAssembly.Name == "Aspheric");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasRpcManualAttribute(IMethodSymbol methodSymbol) => methodSymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Erinn.RpcManualAttribute" && a.AttributeClass?.ContainingAssembly.Name == "Aspheric");

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
        private static bool IsValidRefKind(IParameterSymbol parameterSymbol) => parameterSymbol.GetType() != typeof(IPointerTypeSymbol) && parameterSymbol.RefKind == RefKind.In && !parameterSymbol.Type.IsRefLikeType;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AreParametersValid(IMethodSymbol methodSymbol)
        {
            var parameters = methodSymbol.Parameters;
            return parameters.Length == 3 && parameters[0].RefKind == RefKind.In && parameters[0].Type.ToDisplayString() == "Erinn.NetworkPeer" && parameters[0].Type.ContainingAssembly.Name == "Aspheric" && parameters[1].RefKind == RefKind.In && parameters[1].Type.ToDisplayString() == "Erinn.NetworkPacketFlag" && parameters[1].Type.ContainingAssembly.Name == "Aspheric" && parameters[2].RefKind == RefKind.In && parameters[2].Type.ToDisplayString() == "Erinn.DataStream" && parameters[2].Type.ContainingAssembly.Name == "Aspheric";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReportDiagnostic(SymbolAnalysisContext context, IMethodSymbol methodSymbol, DiagnosticDescriptor descriptor)
        {
            var diagnostic = Diagnostic.Create(descriptor, methodSymbol.Locations[0]);
            context.ReportDiagnostic(diagnostic);
        }
    }
}