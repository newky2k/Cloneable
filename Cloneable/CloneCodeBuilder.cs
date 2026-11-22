using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LoDaTek.Cloneable
{
    /// <summary>
    /// Builds the source of the Clone and CloneSafe methods for a [Cloneable] class
    /// </summary>
    internal sealed class CloneCodeBuilder
    {
        private const string PreventDeepCopyKeyString = "PreventDeepCopy";
        private const string ExplicitDeclarationKeyString = "ExplicitDeclaration";

        internal const string CloneableNamespace = "LoDaTek.Cloneable.Core";
        internal const string CloneableAttributeString = "CloneableAttribute";
        private const string CloneAttributeString = "CloneAttribute";
        private const string IgnoreCloneAttributeString = "IgnoreCloneAttribute";

        //TODO: Add CustomCloneAttribute?
        private readonly INamedTypeSymbol? cloneableAttribute;
        private readonly INamedTypeSymbol? ignoreCloneAttribute;
        private readonly INamedTypeSymbol? cloneAttribute;

        public CloneCodeBuilder(Compilation compilation)
        {
            cloneableAttribute = compilation.GetTypeByMetadataName($"{CloneableNamespace}.{CloneableAttributeString}");
            cloneAttribute = compilation.GetTypeByMetadataName($"{CloneableNamespace}.{CloneAttributeString}");
            ignoreCloneAttribute = compilation.GetTypeByMetadataName($"{CloneableNamespace}.{IgnoreCloneAttributeString}");
        }

        /// <summary>
        /// Returns the generated source for the class, or null if it is not a [Cloneable] class
        /// </summary>
        public string? Build(INamedTypeSymbol classSymbol)
        {
            if (cloneableAttribute is null || cloneAttribute is null || ignoreCloneAttribute is null)
                return null;

            if (!classSymbol.TryGetAttribute(cloneableAttribute, out var attributes))
                return null;

            var attribute = attributes.Single();
            var isExplicit = (bool?)attribute.NamedArguments.FirstOrDefault(e => e.Key.Equals(ExplicitDeclarationKeyString)).Value.Value ?? false;

            return CreateCloneableCode(classSymbol, isExplicit);
        }

        private string CreateCloneableCode(INamedTypeSymbol classSymbol, bool isExplicit)
        {
            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var fieldAssignmentsCode = GenerateFieldAssignmentsCode(classSymbol, isExplicit);
            var fieldAssignmentsCodeSafe = fieldAssignmentsCode.Select(x =>
            {
                if (x.isEnumerable)
                    return x.line.Replace("#CLONE#", "CloneSafe(referenceChain)");
                if (x.isCloneable)
                    return x.line + "Safe(referenceChain)";
                return x.line;
            });
            var fieldAssignmentsCodeFast = fieldAssignmentsCode.Select(x =>
            {
                if (x.isEnumerable)
                    return x.line.Replace("#CLONE#", "Clone()");
                if (x.isCloneable)
                    return x.line + "()";
                return x.line;
            });

            return $@"using System.Collections.Generic;
using System.Linq;
namespace {namespaceName}
{{
    {GetAccessModifier(classSymbol)} partial class {classSymbol.Name}
    {{
        /// <summary>
        /// Creates a copy of {classSymbol.Name} with NO circular reference checking. This method should be used if performance matters.
        /// 
        /// <exception cref=""StackOverflowException"">Will occur on any object that has circular references in the hierarchy.</exception>
        /// </summary>
        public {classSymbol.ToFQF()} Clone()
        {{
            return new {classSymbol.ToFQF()}
            {{
{string.Join(",\n", fieldAssignmentsCodeFast)}
            }};
        }}

        /// <summary>
        /// Creates a copy of {classSymbol.Name} with circular reference checking. If a circular reference was detected, only a reference of the leaf object is passed instead of cloning it.
        /// </summary>
        /// <param name=""referenceChain"">Should only be provided if specific objects should not be cloned but passed by reference instead.</param>
        public {classSymbol.ToFQF()} CloneSafe(Stack<object> referenceChain = null)
        {{
            if(referenceChain?.Contains(this) == true) 
                return this;
            referenceChain ??= new Stack<object>();
            referenceChain.Push(this);
            var result = new {classSymbol.ToFQF()}
            {{
{string.Join(",\n", fieldAssignmentsCodeSafe)}
            }};
            referenceChain.Pop();
            return result;
        }}
    }}
}}";
        }

        private IEnumerable<(string line, bool isCloneable, bool isEnumerable)> GenerateFieldAssignmentsCode(INamedTypeSymbol classSymbol, bool isExplicit )
        {
            var fieldNames = GetCloneableProperties(classSymbol, isExplicit);

            var fieldAssignments = fieldNames.Select(field => IsFieldCloneable(field, classSymbol)).
                OrderBy(x => x.isCloneable).
                Select(x => (GenerateAssignmentCode(x.item, x.isCloneable, x.isEnumerable), x.isCloneable, x.isEnumerable));
            return fieldAssignments;
        }

        private string GenerateAssignmentCode(IPropertySymbol symbol, bool isCloneable, bool isEnumerable)
        {
            var name = symbol.Name;
            if (isEnumerable)
            {
                return $@"                {name} = {GenerateEnumerableConversionCode($"this.{name}", symbol.Type)}";
            }
            if (isCloneable)
            {
                return $@"                {name} = this.{name}{(symbol.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "")}.Clone";
            }

            return $@"                {name} = this.{name}";
        }

        private string GenerateEnumerableConversionCode(string name, ITypeSymbol type, int depth = 1)
        {
            var code = GenerateEnumerableConversionCoreCode(name, type, depth);

            //Nothing is converted (value is passed by reference), so a null value is already copied as null
            if (code == name) return code;

            //A null collection is copied as null instead of throwing when it is enumerated
            return $"({name} == null ? null : {code})";
        }

        private string GenerateEnumerableConversionCoreCode(string name, ITypeSymbol type, int depth)
        {
            var arguments = type.GetIDictionaryTypeArguments() ?? type.GetIEnumerableTypeArguments();
            if (arguments == null) return name;
            var argumentName = new string('x', depth);
            if (type is IArrayTypeSymbol arraySymbol)
            {
                return $"{name}.Select({argumentName} => {GenerateEnumerableTypeCloneCode(argumentName, arguments.Value[0], depth)}).ToArray()";
            }
            var typeAsEnumerable = $"global::System.Collections.Generic.IEnumerable<{arguments.Value.ElementAtOrDefault(0)?.ToFQF()}>";
            var argumentsAsKeyValuePair = $"global::System.Collections.Generic.KeyValuePair<{arguments.Value.ElementAtOrDefault(0)?.ToFQF()}, {arguments.Value.ElementAtOrDefault(1)?.ToFQF()}>";
            var typeAsKeyValuePair = $"global::System.Collections.Generic.IEnumerable<{argumentsAsKeyValuePair}>";
            var isConstructableWithSelf = ((INamedTypeSymbol)type).Constructors.Any(constructors =>
                constructors.Parameters.Any(param => param.Type.ToFQF() == type.ToKnownInterfaceFQF())
            );
            var isConstructableWithEnumerable = ((INamedTypeSymbol)type).Constructors.Any(constructors => 
                constructors.Parameters.Any(param => param.Type.ToFQF() == typeAsEnumerable)
            );
            var isConstructableWithKeyValuePair = ((INamedTypeSymbol)type).Constructors.Any(constructors =>
                constructors.Parameters.Any(param => param.Type.ToFQF() == typeAsKeyValuePair)
            );
            if (arguments.Value.Any(x => !x.IsValueType))
            {
                //Note: Should support "most" of the commonly used collections https://learn.microsoft.com/en-us/dotnet/standard/collections/commonly-used-collection-types
                //Does not really support: Hashtable (Depricated), ArrayList (Depricated), SortedList (Does currently only support value types. 
                if (isConstructableWithEnumerable)
                {
                    return $"new {type.ToNullableFQF()}({name}.Select({argumentName} => {GenerateEnumerableTypeCloneCode(argumentName, arguments.Value[0], depth)}))";
                }
                else if (isConstructableWithKeyValuePair)
                {
                    return $"new {type.ToNullableFQF()}({name}.Select({argumentName} => new {argumentsAsKeyValuePair}({GenerateEnumerableTypeCloneCode($"{argumentName}.Key", arguments.Value[0], depth)}, {GenerateEnumerableTypeCloneCode($"{argumentName}.Value", arguments.Value[1], depth)})))";
                }
                else if (type.ToFQF() == typeAsEnumerable)
                {
                    return $"{name}.Select({argumentName} => {GenerateEnumerableTypeCloneCode(argumentName, arguments.Value[0], depth)})";
                }
                return name;
            }
            if (isConstructableWithSelf)
            {
                return $"new {type.ToNullableFQF()}({name})";
            }
            else if (isConstructableWithEnumerable)
            {
                return $"new {type.ToNullableFQF()}({name}.Select({argumentName} => {argumentName}))";
            }
            else if (isConstructableWithKeyValuePair)
            {
                return $"new {type.ToNullableFQF()}({name}.Select({argumentName} => new {argumentsAsKeyValuePair}({argumentName}.Key, {argumentName}.Value)))";
            }
            else if (type.ToFQF() == typeAsEnumerable)
            {
                return $"{name}.Select({argumentName} => {argumentName})";
            }
            return name;
        }

        private string GenerateEnumerableTypeCloneCode(string name, ITypeSymbol type, int depth)
        {
            if (type.IsPossibleEnumerable()) //If it is a nested enumerable, repeat the process
            {
                return GenerateEnumerableConversionCode(name, type, depth + 1);
            }
            if (!type.TryGetAttribute(cloneableAttribute!, out var attributes))
            {
                return name;
            }
            var preventDeepCopy = (bool?)attributes.Single().NamedArguments.FirstOrDefault(e => e.Key.Equals(PreventDeepCopyKeyString)).Value.Value ?? false;
            if (preventDeepCopy) return name;
            return $"{name}.#CLONE#";
        }

        private (IPropertySymbol item, bool isCloneable, bool isEnumerable) IsFieldCloneable(IPropertySymbol x, INamedTypeSymbol classSymbol)
        {
            if (SymbolEqualityComparer.Default.Equals(x.Type, classSymbol))
            {
                return (x, false, false);
            }

            if (x.Type.IsPossibleEnumerable())
            {
                return (x, false, true);
            }

            if (!x.Type.TryGetAttribute(cloneableAttribute!, out var attributes))
            {
                return (x, false, false);
            }

            var preventDeepCopy = (bool?)attributes.Single().NamedArguments.FirstOrDefault(e => e.Key.Equals(PreventDeepCopyKeyString)).Value.Value ?? false;
            return (item: x, !preventDeepCopy, false);
        }

        private string GetAccessModifier(INamedTypeSymbol classSymbol)
        {
            return classSymbol.DeclaredAccessibility.ToString().ToLowerInvariant();
        }

        private IEnumerable<IPropertySymbol> GetCloneableProperties(ITypeSymbol classSymbol, bool isExplicit)
        {
            var targetSymbolMembers = classSymbol.GetMembers().OfType<IPropertySymbol>()
                .Where(x => x.SetMethod is not null &&
                            x.CanBeReferencedByName);
            if (isExplicit)
            {
                return targetSymbolMembers.Where(x => x.HasAttribute(cloneAttribute!));
            }
            else
            {
                return targetSymbolMembers.Where(x => !x.HasAttribute(ignoreCloneAttribute!));
            }
        }
    }
}
