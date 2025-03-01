﻿using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Antelcat.I18N.Abstractions;
using Antelcat.I18N.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using GeneratorAttributeSyntaxContext = Microsoft.CodeAnalysis.GeneratorAttributeSyntaxContext;

namespace Antelcat.I18N.WPF.SourceGenerators.Generators;

[Generator(LanguageNames.CSharp)]
internal class ResourceKeysGenerator : AttributeDetectBaseGenerator
{
    private static readonly string Attribute         = $"{typeof(ResourceKeysOfAttribute).FullName}";
    private static readonly string CultureInfo       = $"global::{typeof(CultureInfo).FullName}";
    private static readonly string ResourceProvider  = $"global::{typeof(ResourceProvider).FullName}";
    private static readonly string ModuleInitializer = $"global::{typeof(ModuleInitializerAttribute).FullName}";
    private static readonly string IEnumerable       = $"global::{nameof(System)}.Collections.Generic.IEnumerable<string>";
    
    private static readonly string[] Exceptions =
    [
        "ResourceManager",
        "Culture"
    ];

    protected override string AttributeName => Attribute;

    protected override void GenerateCode(SourceProductionContext context,
        ImmutableArray<(GeneratorAttributeSyntaxContext, TypeSyntax)> targets)
    {
        foreach (var (generateCtx, type) in targets)
        {
            var targetSymbol   = generateCtx.SemanticModel.GetSymbolInfo(type).Symbol as INamedTypeSymbol;
            var targetFullName = targetSymbol.GetFullyQualifiedName();
            var names = targetSymbol!
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Select(x => x.Name)
                .Except(Exceptions).ToList();
            var nameSpace      = generateCtx.TargetSymbol.ContainingNamespace.GetFullyQualifiedName();
            var className      = $"__{targetSymbol.Name}Provider";
            var syntaxTriviaList = TriviaList(
                Comment("// <auto-generated/> By Antelcat.I18N.SourceGenerators"),
                Trivia(PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), true)),
                Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)));
            var unit = CompilationUnit()
                .AddMembers(
                    NamespaceDeclaration(IdentifierName(nameSpace.Replace("global::", "")))
                        .WithLeadingTrivia(syntaxTriviaList)
                        .AddMembers(
                            ClassDeclaration(generateCtx.TargetSymbol.Name)
                                .AddModifiers(Token(SyntaxKind.PartialKeyword))
                                .AddMembers(
                                    names.Select(x =>
                                            ParseMemberDeclaration(
                                                $"""
                                                 /// <summary>
                                                 /// {x}
                                                 /// </summary>
                                                 public static string {x} => nameof({x});
                                                 """
                                            )!)
                                        .ToArray())
                                .AddMembers(
                                    ClassDeclaration(className)
                                        .AddModifiers(SyntaxKind.InternalKeyword)
                                        .AddBaseListTypes(ResourceProvider)
                                        .AddMembers(
                                            ParseMemberDeclaration(
                                                $$"""
                                                  public override string this[string key] {
                                                      get {
                                                          switch(key){
                                                              {{string.Concat(names.Select(static x=>$"case nameof({x}): return this.{x}; \n"))}}
                                                          }
                                                          return key;
                                                      }
                                                  }
                                                  """
                                            )!)
                                        .AddMembers(
                                            ParseMemberDeclaration(
                                                $$"""
                                                  public override {{IEnumerable}} Keys(){
                                                      {{string.Concat(names.Select(static x=>$"yield return nameof({x});\n"))}}
                                                  }
                                                  """
                                            )!)
                                        .AddMembers(
                                            $$"""
                                              public override {{CultureInfo}}? Culture
                                              {
                                                  get => {{targetFullName}}.Culture;
                                                  set
                                                  {
                                                      if (value == null) return;
                                                      if (Equals({{targetFullName}}.Culture?.EnglishName, value.EnglishName)) return;
                                                      {{targetFullName}}.Culture = value;
                                                      UpdateSource();
                                                      OnChangeCompleted();
                                                  }
                                              }
                                              """,
                                            $$"""
                                              private void UpdateSource()
                                              {
                                                  foreach(var key in this.Keys()){
                                                      OnPropertyChanged(key);
                                                  }
                                              }
                                              """,
                                            $$"""
                                              [{{ModuleInitializer}}]
                                              public static void Initialize()
                                              {
                                                  RegisterProvider(new {{className}}());
                                              }
                                              """
                                        )
                                        .AddMembers(names.Select(x =>
                                                $"public string {x} => {targetFullName}.{x};")
                                            .ToArray())
                                )
                        )
                ).NormalizeWhitespace();

            context.AddSource($"{generateCtx.TargetSymbol.GetFullyQualifiedName().Replace("global::", "")}.g.cs",
                unit.GetText(Encoding.UTF8));

        }
    }
}