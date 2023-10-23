using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToolkit.Mvvm.SourceGenerators.Generators;

[Generator(LanguageNames.CSharp)]
internal class ResourceKeysGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var info = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{nameof(WpfToolkit)}.{nameof(Mvvm)}.Attributes.ResourceKeysOfAttribute",
            static (node, _) =>
                node is ClassDeclarationSyntax { Parent: not ClassDeclarationSyntax } classDeclarationSyntax 
                && classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword),
            (ctx, token) =>
            {
                Debugger.Break();
                var attr   = ctx.Attributes.First().AttributeClass;
                var origin = attr.OriginalDefinition;
                return (ctx.TargetNode, ctx.Attributes.Select(x => x.ConstructorArguments[0].Value));
            }
        );
        Debugger.Launch();
        context.RegisterSourceOutput(info, (ctx, constant) =>
        {
            ctx.AddSource("LangKeys.g.cs","""
                                          namespace WpfToolkit.Mvvm.Sample.Strings;

                                          partial class LangKeys{
                                              public const string Str = nameof(Str);
                                          }
                                          """);
        });
    }
}