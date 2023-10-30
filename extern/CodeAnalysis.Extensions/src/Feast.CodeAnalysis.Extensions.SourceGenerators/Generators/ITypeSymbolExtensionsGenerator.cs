using Microsoft.CodeAnalysis;
namespace Feast.CodeAnalysis.Extensions.Generators;

[Generator]
public class ITypeSymbolExtensionsGenerator : IIncrementalGenerator
{
    private const string ClassName = nameof(ITypeSymbolExtensions);
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource(Global.GenerateFileName(ClassName),
                Global.Generate(ClassName,
                    ITypeSymbolExtensions.AppendFullyQualifiedMetadataNameText,
                    ITypeSymbolExtensions.HasFullyQualifiedMetadataNameText
                ));
        });
    }
}