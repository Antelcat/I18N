using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        context.RegisterSourceOutput(provider.Collect(), (ctx, objs) =>
        {
            var obj = objs.FirstOrDefault(x => x !=null);
            if (obj is null) return;
            var version = obj.ContainingAssembly.Identity.Version;
            if (version.CompareTo(new Version(3, 5))    < 0
                || version.CompareTo(new Version(5, 0)) >= 0) return;
            ctx.AddSource("ModuleInitializerAttribute.g.cs",SourceText.From(
                """
                namespace System.Runtime.CompilerServices
                {
                    [global::System.AttributeUsage(AttributeTargets.Method, Inherited = false)]
                    internal class ModuleInitializerAttribute : Attribute { }
                }
                """
                ,Encoding.UTF8));

        });
    }
}