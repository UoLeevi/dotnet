using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetApp.SourceGenerator
{
    [Generator]
    public class PropertyGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
            => context.RegisterForSyntaxNotifications(() => new FieldWithAttributesSyntaxReceiver());

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxReceiver is FieldWithAttributesSyntaxReceiver receiver))
                return;

            Compilation compilation = context.Compilation;
            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("DotNetApp.GeneratePropertyAttribute");
            INamedTypeSymbol notifyChangingSymbol = compilation.GetTypeByMetadataName(typeof(System.ComponentModel.INotifyPropertyChanging).FullName);
            INamedTypeSymbol notifyChangedSymbol = compilation.GetTypeByMetadataName(typeof(System.ComponentModel.INotifyPropertyChanged).FullName);

            // loop over the candidate fields, and keep the ones that are actually annotated
            List<IFieldSymbol> fieldSymbols = new List<IFieldSymbol>();

            foreach (FieldDeclarationSyntax field in receiver.CandidateFields)
            {
                SemanticModel model = compilation.GetSemanticModel(field.SyntaxTree);

                foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                {
                    // Get the symbol being decleared by the field, and keep it if its annotated
                    IFieldSymbol fieldSymbol = model.GetDeclaredSymbol(variable) as IFieldSymbol;
                    if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                    {
                        fieldSymbols.Add(fieldSymbol);
                    }
                }
            }

            // group the fields by class, and generate the source
            foreach (IGrouping<INamedTypeSymbol, IFieldSymbol> group in fieldSymbols.GroupBy(f => f.ContainingType))
            {
                string classSource = GenerateClassSource(group.Key, group.ToList());
                context.AddSource($"{group.Key.Name}_GenerateProperty.cs", classSource);
            }

            string GenerateClassSource(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields)
            {
                if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
                {
                    return null; //TODO: issue a diagnostic that it must be top level
                }

                string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

                // begin building the generated source
                StringBuilder source = new StringBuilder($@"
namespace {namespaceName}
{{
    public partial class {classSymbol.Name} : {notifyChangedSymbol.ToDisplayString()}, {notifyChangingSymbol.ToDisplayString()}
    {{
");

                if (!classSymbol.Interfaces.Contains(notifyChangingSymbol))
                {
                    source.Append($"        public event {typeof(System.ComponentModel.PropertyChangingEventHandler).FullName} PropertyChanging;");
                }
                
                if (!classSymbol.Interfaces.Contains(notifyChangedSymbol))
                {
                    source.Append($"        public event {typeof(System.ComponentModel.PropertyChangedEventHandler).FullName} PropertyChanged;");
                }

                // create properties for each field 
                foreach (IFieldSymbol fieldSymbol in fields)
                {
                    ProcessField(source, fieldSymbol, attributeSymbol);
                }

                source.Append("    }\n}");
                return source.ToString();
            }
        }

        private void ProcessField(StringBuilder source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
        {
            // get the name and type of the field
            string fieldName = fieldSymbol.Name;
            ITypeSymbol fieldType = fieldSymbol.Type;

            // get the GenerateProperty attribute from the field, and any associated data
            AttributeData attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            TypedConstant overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

            string propertyName = GeneratePropertyName(fieldName, overridenNameOpt);
            if (propertyName.Length == 0 || propertyName == fieldName)
            {
                //TODO: issue a diagnostic that we can't process this field
                return;
            }

            source.Append($@"
        public {fieldType} {propertyName} 
        {{
            get 
            {{
                return this.{fieldName};
            }}

            set
            {{
                var oldValue = this.{fieldName};
                if (oldValue == value) return;
                this.PropertyChanging?.Invoke(this, new DotNetApp.Extensions.PropertyChangingExtendedEventArgs<{fieldType}>(nameof({propertyName}), oldValue, value));
                this.{fieldName} = value;
                this.PropertyChanged?.Invoke(this, new DotNetApp.Extensions.PropertyChangedExtendedEventArgs<{fieldType}>(nameof({propertyName}), oldValue, value));
            }}
        }}

");
        }

        string GeneratePropertyName(string fieldName, TypedConstant overridenNameOpt)
        {
            if (!overridenNameOpt.IsNull)
            {
                return overridenNameOpt.Value.ToString();
            }

            fieldName = fieldName.TrimStart('_');
            if (fieldName.Length == 0)
                return string.Empty;

            if (fieldName.Length == 1)
                return fieldName.ToUpper();

            return fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
        }
    }

    internal class FieldWithAttributesSyntaxReceiver : ISyntaxReceiver
    {
        public List<FieldDeclarationSyntax> CandidateFields { get; } = new List<FieldDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // any field with at least one attribute is a candidate for property generation
            if (syntaxNode is FieldDeclarationSyntax fieldDeclarationSyntax
                && fieldDeclarationSyntax.AttributeLists.Count > 0)
            {
                CandidateFields.Add(fieldDeclarationSyntax);
            }
        }
    }
}
