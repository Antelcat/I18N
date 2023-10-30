using System.Collections;
using Feast.CodeAnalysis.Extensions.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace Feast.CodeAnalysis.Extensions.Generators;

[Generator]
public class SyntaxExtensionsGenerator : IIncrementalGenerator
{
    private const string ClassName = nameof(SyntaxExtensions);
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource(Global.GenerateFileName(ClassName),
                Global.Generate(ClassName, 
                    SyntaxExtensions.GetAllAttributesText,
                    SyntaxExtensions.GetSpecifiedAttributesText,
                    SyntaxExtensions.GetSpecifiedAttributeText,
                    SyntaxExtensions.HasSpecifiedAttributeText
                ));
        });
    }
}