using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace LoDaTek.Cloneable
{
    /// <summary>
    /// Incremental source generator that adds Clone and CloneSafe methods to partial classes marked with [Cloneable]
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public class CloneableGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Only classes carrying [Cloneable] reach the transform; the output is plain strings so results are cached between edits
            var classes = context.SyntaxProvider.ForAttributeWithMetadataName(
                $"{CloneCodeBuilder.CloneableNamespace}.{CloneCodeBuilder.CloneableAttributeString}",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) =>
                {
                    if (ctx.TargetSymbol is not INamedTypeSymbol classSymbol)
                        return null;

                    var source = new CloneCodeBuilder(ctx.SemanticModel.Compilation).Build(classSymbol);
                    if (source is null)
                        return null;

                    return new GeneratedClass(classSymbol.Name, classSymbol.ContainingNamespace.ToDisplayString(), source);
                })
                .Where(static x => x is not null)
                .Select(static (x, _) => x!)
                .Collect();

            context.RegisterSourceOutput(classes, static (spc, generated) => AddSources(spc, generated));
        }

        private static void AddSources(SourceProductionContext context, ImmutableArray<GeneratedClass> generated)
        {
            foreach (var item in generated)
            {
                //Fix issue where two classes have the same name
                var hasDuplicateName = generated.Count(x => x.Name == item.Name) > 1;
                var hintName = $"{(hasDuplicateName ? $"{item.Namespace}." : null)}{item.Name}_cloneable.cs";
                context.AddSource(hintName, SourceText.From(item.Source, Encoding.UTF8));
            }
        }

        private sealed class GeneratedClass : System.IEquatable<GeneratedClass>
        {
            public GeneratedClass(string name, string @namespace, string source)
            {
                Name = name;
                Namespace = @namespace;
                Source = source;
            }

            public string Name { get; }
            public string Namespace { get; }
            public string Source { get; }

            public bool Equals(GeneratedClass? other) =>
                other is not null && Name == other.Name && Namespace == other.Namespace && Source == other.Source;

            public override bool Equals(object? obj) => Equals(obj as GeneratedClass);

            public override int GetHashCode() => Source.GetHashCode();
        }
    }
}
