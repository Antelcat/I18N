using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Antelcat.I18N.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Antelcat.I18N.WPF.SourceGenerators.Generators;

[Generator]
public class ModuleInitializerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
            (c,t)=>c is ClassDeclarationSyntax,
            (c, t) =>
            {
                var symbol = c.SemanticModel.GetDeclaredSymbol(c.Node);
                if (symbol is not INamedTypeSymbol type) return null;
                if (type.TypeKind    != TypeKind.Class) return null;
                while (type.BaseType != null) type = type.BaseType;
                return type;
            });
        var disable = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                typeof(DisableGenerationForAttribute).FullName!,
                (_, _) => true,
                (c, _) => c);
        context.RegisterSourceOutput(provider.Collect().Combine(disable.Collect()), (ctx, objs) =>
        {
            Dictionary<string, (string fileName, string content)> generates = new()
            {
                {
                    "global::" + typeof(ModuleInitializerAttribute).FullName,
                    ("ModuleInitializerAttribute.g.cs",
                        """
                        namespace System.Runtime.CompilerServices
                        {
                            [global::System.AttributeUsage(AttributeTargets.Method, Inherited = false)]
                            internal class ModuleInitializerAttribute : Attribute { }
                        }
                        """)
                }
            };
            
            
            
            var obj = objs.Left.FirstOrDefault(x => x != null);
            if (obj is null) return;
            var version = obj.ContainingAssembly.Identity.Version;
            if (version.CompareTo(new Version(3, 5))    < 0
                || version.CompareTo(new Version(5, 0)) >= 0) return;

            if (objs.Right.Any())
            {
                var disGen = objs.Right
                    .First().Attributes
                    .FirstOrDefault()?
                    .ConstructorArguments.FirstOrDefault();
                if (disGen is not null)
                {
                    foreach (var typeName in from TypedConstant constant in disGen.Value.Values
                        select (constant.Value as INamedTypeSymbol).GetFullyQualifiedName())
                    {
                        generates.Remove(typeName);
                    }
                }
            }

            foreach (var pair in generates)
            {
                ctx.AddSource(pair.Value.fileName, SourceText.From(pair.Value.content, Encoding.UTF8));
            }
        });
    }
}