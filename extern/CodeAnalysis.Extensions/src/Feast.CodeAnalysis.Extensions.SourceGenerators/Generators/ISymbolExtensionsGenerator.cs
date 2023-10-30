using System.Threading;
using Feast.CodeAnalysis.Extensions.Internals;
using Microsoft.CodeAnalysis;

namespace Feast.CodeAnalysis.Extensions.Generators;

// ReSharper disable once InconsistentNaming
[Generator]
public class ISymbolExtensionsGenerator : IIncrementalGenerator
{
    private const string ClassName = nameof(ISymbolExtensions);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource(Global.GenerateFileName(ClassName),
                Global.Generate(ClassName,
                    ISymbolExtensions.GetFullyQualifiedNameText,
                    ISymbolExtensions.TryGetAttributeWithFullyQualifiedMetadataNameText
                )
            );
        });
    }
}