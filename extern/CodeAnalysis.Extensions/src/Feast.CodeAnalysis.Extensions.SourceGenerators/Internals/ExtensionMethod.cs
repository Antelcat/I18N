using System.Linq;
using System.Text;

namespace Feast.CodeAnalysis.Extensions.Internals;

public record ExtensionMethod(string Name,
    string ReturnType,
    (string Type, string Name) This,
    params (string Type, string Name)[]? Parameters)
{
    public string? Annotation { get; init; }

    public string? Body { get; init; }
    
    public override string ToString()
    {
        return $$"""
                    {{Annotation}}
                     public static {{ReturnType}} {{Name}}(this {{This.Type}} {{This.Name}}{{Parameters?.Aggregate(new StringBuilder(), (b, c) =>
                         b.Append(", ")
                             .Append(c.Type)
                             .Append(' ')
                             .Append(c.Name))}})
                     {
                         {{Body?.Replace("\n", "\n\t\t")}}
                     }
                 
                 """;
    }
}