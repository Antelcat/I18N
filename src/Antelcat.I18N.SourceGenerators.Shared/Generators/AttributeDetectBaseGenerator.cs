using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Internal;

namespace Antelcat.I18N.WPF.SourceGenerators.Generators;

internal abstract class AttributeDetectBaseGenerator : IIncrementalGenerator
{
    protected abstract string AttributeName { get; }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        try
        {
            var info = context.SyntaxProvider.ForAttributeWithMetadataName(
                AttributeName,
                static (node, _) =>
                    node is ClassDeclarationSyntax { Parent: not ClassDeclarationSyntax } classDeclarationSyntax
                    && classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword),
                (ctx, token) =>
                {
                    var node      = ctx.TargetNode as ClassDeclarationSyntax;
                    var attribute = node.GetSpecifiedAttribute(ctx.SemanticModel, AttributeName, token);
                    if (attribute == null) return (ctx, null);
                    var argumentSyntax = attribute.ArgumentList?.Arguments.FirstOrDefault();
                    return argumentSyntax is not { Expression: TypeOfExpressionSyntax typeOfExp }
                        ? (ctx, null)
                        : (ctx, typeOfExp.Type);
                }
            ).Where(x => x.Type != null);

            context.RegisterSourceOutput(info.Collect(), GenerateCode);
        }
        catch (System.Exception e)
        {
            Debugger.Launch();
            throw;
        }
    }

    protected abstract void GenerateCode(SourceProductionContext context,
        ImmutableArray<(GeneratorAttributeSyntaxContext, TypeSyntax)> targets);
}