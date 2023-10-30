using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Internal;

namespace Feast.CodeAnalysis.Extensions.Generators;

// ReSharper disable once InconsistentNaming
[Generator]
public class ExtendedClassGenerator : IIncrementalGenerator
{

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource(Global.GenerateFileName(nameof(Global)), 
                """
                #if !ROSLYN_4_3_1_OR_GREATER
                using Microsoft.CodeAnalysis.Internal;
                #endif
                """);
            ctx.AddSource(Global.GenerateFileName(nameof(GeneratorAttributeSyntaxContext)),
                GeneratorAttributeSyntaxContext.GeneratorAttributeSyntaxContextText
            );
            ctx.AddSource(Global.GenerateFileName("ImmutableArrayBuilder{T}"),
                ImmutableArrayBuilder<object>.ImmutableArrayBuilderText);
            ctx.AddSource(Global.GenerateFileName(nameof(SyntaxValueProviderExtensions)),
                SyntaxValueProviderExtensions.SyntaxValueProviderText
            );
        });
    }
}