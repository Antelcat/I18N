using System;
using System.Linq;
#nullable enable
namespace Microsoft.CodeAnalysis
{
    internal static class ITypeSymbolExtensions
    {
        /// <summary>
        /// Checks whether or not a given type symbol has a specified fully qualified metadata name.
        /// </summary>
        /// <param name="symbol">The input <see cref="ITypeSymbol"/> instance to check.</param>
        /// <param name="name">The full name to check.</param>
        /// <returns>Whether <paramref name="symbol"/> has a full name equals to <paramref name="name"/>.</returns>
        public static global::System.Boolean HasFullyQualifiedMetadataName(this global::Microsoft.CodeAnalysis.ITypeSymbol symbol, global::System.String name)
        {
            using global::Microsoft.CodeAnalysis.ImmutableArrayBuilder<global::System.Char> builder = global::Microsoft.CodeAnalysis.ImmutableArrayBuilder<global::System.Char>.Rent();
        
            symbol.AppendFullyQualifiedMetadataName(in builder);
        
            return builder.WrittenSpan.SequenceEqual(name.AsSpan());
        }

	    internal const string HasFullyQualifiedMetadataNameText =
        """
        
        /// <summary>
        /// Checks whether or not a given type symbol has a specified fully qualified metadata name.
        /// </summary>
        /// <param name="symbol">The input <see cref="ITypeSymbol"/> instance to check.</param>
        /// <param name="name">The full name to check.</param>
        /// <returns>Whether <paramref name="symbol"/> has a full name equals to <paramref name="name"/>.</returns>
        public static global::System.Boolean HasFullyQualifiedMetadataName(this global::Microsoft.CodeAnalysis.ITypeSymbol symbol, global::System.String name)
        {
            using global::Microsoft.CodeAnalysis.ImmutableArrayBuilder<global::System.Char> builder = global::Microsoft.CodeAnalysis.ImmutableArrayBuilder<global::System.Char>.Rent();
        
            symbol.AppendFullyQualifiedMetadataName(in builder);
        
            return builder.WrittenSpan.SequenceEqual(name.AsSpan());
        }
        """;

        /// <summary>
        /// Appends the fully qualified metadata name for a given symbol to a target builder.
        /// </summary>
        /// <param name="symbol">The input <see cref="ITypeSymbol"/> instance.</param>
        /// <param name="builder">The target <see cref="ImmutableArrayBuilder{T}"/> instance.</param>
        private static void AppendFullyQualifiedMetadataName(this global::Microsoft.CodeAnalysis.ITypeSymbol symbol, 
            in global::Microsoft.CodeAnalysis.ImmutableArrayBuilder<global::System.Char> builder)
        {
            static void BuildFrom(global::Microsoft.CodeAnalysis.ISymbol? symbol, in global::Microsoft.CodeAnalysis.ImmutableArrayBuilder<global::System.Char> builder)
            {
                switch (symbol)
                {
                    // Namespaces that are nested also append a leading '.'
                    case INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: false }:
                        BuildFrom(symbol.ContainingNamespace, in builder);
                        builder.Add('.');
                        builder.AddRange(symbol.MetadataName.AsSpan());
                        break;
        
                    // Other namespaces (ie. the one right before global) skip the leading '.'
                    case INamespaceSymbol { IsGlobalNamespace: false }:
                        builder.AddRange(symbol.MetadataName.AsSpan());
                        break;
        
                    // Types with no namespace just have their metadata name directly written
                    case ITypeSymbol { ContainingSymbol: INamespaceSymbol { IsGlobalNamespace: true } }:
                        builder.AddRange(symbol.MetadataName.AsSpan());
                        break;
        
                    // Types with a containing non-global namespace also append a leading '.'
                    case ITypeSymbol { ContainingSymbol: INamespaceSymbol namespaceSymbol }:
                        BuildFrom(namespaceSymbol, in builder);
                        builder.Add('.');
                        builder.AddRange(symbol.MetadataName.AsSpan());
                        break;
        
                    // Nested types append a leading '+'
                    case ITypeSymbol { ContainingSymbol: ITypeSymbol typeSymbol }:
                        BuildFrom(typeSymbol, in builder);
                        builder.Add('+');
                        builder.AddRange(symbol.MetadataName.AsSpan());
                        break;
                    default:
                        break;
                }
            }
        
            BuildFrom(symbol, in builder);
        }

	    internal const string AppendFullyQualifiedMetadataNameText =
        """
        
        /// <summary>
        /// Appends the fully qualified metadata name for a given symbol to a target builder.
        /// </summary>
        /// <param name="symbol">The input <see cref="ITypeSymbol"/> instance.</param>
        /// <param name="builder">The target <see cref="ImmutableArrayBuilder{T}"/> instance.</param>
        private static void AppendFullyQualifiedMetadataName(this global::Microsoft.CodeAnalysis.ITypeSymbol symbol, 
            in global::Microsoft.CodeAnalysis.ImmutableArrayBuilder<global::System.Char> builder)
        {
            static void BuildFrom(global::Microsoft.CodeAnalysis.ISymbol? symbol, in global::Microsoft.CodeAnalysis.ImmutableArrayBuilder<global::System.Char> builder)
            {
                switch (symbol)
                {
                    // Namespaces that are nested also append a leading '.'
                    case INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: false }:
                        BuildFrom(symbol.ContainingNamespace, in builder);
                        builder.Add('.');
                        builder.AddRange(symbol.MetadataName.AsSpan());
                        break;
        
                    // Other namespaces (ie. the one right before global) skip the leading '.'
                    case INamespaceSymbol { IsGlobalNamespace: false }:
                        builder.AddRange(symbol.MetadataName.AsSpan());
                        break;
        
                    // Types with no namespace just have their metadata name directly written
                    case ITypeSymbol { ContainingSymbol: INamespaceSymbol { IsGlobalNamespace: true } }:
                        builder.AddRange(symbol.MetadataName.AsSpan());
                        break;
        
                    // Types with a containing non-global namespace also append a leading '.'
                    case ITypeSymbol { ContainingSymbol: INamespaceSymbol namespaceSymbol }:
                        BuildFrom(namespaceSymbol, in builder);
                        builder.Add('.');
                        builder.AddRange(symbol.MetadataName.AsSpan());
                        break;
        
                    // Nested types append a leading '+'
                    case ITypeSymbol { ContainingSymbol: ITypeSymbol typeSymbol }:
                        BuildFrom(typeSymbol, in builder);
                        builder.Add('+');
                        builder.AddRange(symbol.MetadataName.AsSpan());
                        break;
                    default:
                        break;
                }
            }
        
            BuildFrom(symbol, in builder);
        }
        """;

    }
}