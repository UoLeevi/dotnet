using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DotNetApp.EntityFrameworkCore
{
    public class CSharpEntityTypeCodeEditor : TextEditor<CSharpEntityTypeCodeEditor>
    {
        private string indentationString = null;

        private void InferIndentation()
        {
            if (indentationString != null) return;
            Regex pattern = new Regex($"\r\n([\t ]*)(?:\\w+ +)*class\\W");
            indentationString = pattern.Match(Text).Groups[1].Value;
        }

        private string Indentation(int level = 1)
        {
            InferIndentation();
            return string.Concat(Enumerable.Repeat(indentationString, level));
        }

        public CSharpEntityTypeCodeEditor()
        {
        }

        public virtual CSharpEntityTypeCodeEditor MoveToEndOfClassDefinition(string className)
        {
            MoveToPattern($"\r\n{Indentation()}class {className}\\W");
            MoveToPattern($"\r\n{Indentation()}}}\r\n", TextEditor.SearchScope.AfterCaret);
            
            if (!IsWhiteSpaceLine)
            {
                WriteLine();
            }

            return this;
        }

        public virtual CSharpEntityTypeCodeEditor AddUsingNamespaces(params string[] namespaces)
        {
            MoveToStart();
            MoveToNextEmptyLine();

            foreach (var @namespace in namespaces)
            {
                WriteLine($"using {@namespace};");
            }

            return this;
        }

        public virtual CSharpEntityTypeCodeEditor AddInheritedTypes(IEntityType entityType, params string[] typeNames)
        {
            Select($"(public partial class {entityType.Name})( *:)?");
            var separator = string.IsNullOrWhiteSpace(Match.Groups[2].Value) ? "" : ","; 
            return Write($"{Match.Groups[1].Value} : {string.Join(", ", typeNames)}{separator}");
        }

        public virtual CSharpEntityTypeCodeEditor WriteLinesToEndOfClassDefinition(string className, params string[] lines)
        {
            MoveToEndOfClassDefinition(className);
            WriteIndentedLines(Indentation(2), lines);
            return this;
        }

        public virtual CSharpEntityTypeCodeEditor ReplaceAutoPropertiesAndImplementChangeNotifications(IEntityType entityType)
        {
            WriteLinesToEndOfClassDefinition(entityType.Name,
                "",
                $"#region Private fields",
                "");

            foreach (IProperty scalarProperty in entityType.GetProperties())
            {
                ImplementPropertyGetterAndSetter(scalarProperty);
            }

            foreach (INavigation navigationProperty in entityType.GetNavigations())
            {
                ImplementPropertyGetterAndSetter(navigationProperty);
            }

            WriteLinesToEndOfClassDefinition(entityType.Name,
                "#endregion",
                "",
                "#region Property Change Notifications",
                "",
                "public event PropertyChangingEventHandler PropertyChanging;",
                "public event PropertyChangedEventHandler PropertyChanged;",
                "",
                "private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = \"\")",
                "{",
                $"{Indentation()}if (EqualityComparer<T>.Default.Equals(field, value)) return;",
                $"{Indentation()}PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));",
                $"{Indentation()}field = value;",
                $"{Indentation()}PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));",
                "}",
                "",
                "#endregion");

            return this;

            void ImplementPropertyGetterAndSetter(IPropertyBase property)
            {
                string typeNameString = Regex.Match(Text, $"public (?:virtual )?([^\\s]+) {property.Name} ").Groups[1].Value;

                Select($"(public (?:virtual )?[^\\s]+ {property.Name} ){{ get; set; }}");
                Write($"{Match.Groups[1].Value,-80}{{ get => _{property.Name}; set => SetProperty(ref _{property.Name}, value); }}");

                MoveToEndOfClassDefinition(entityType.Name);
                WriteLine($"{Indentation(2)}private {typeNameString} _{property.Name};");
            }
        }

        public virtual CSharpEntityTypeCodeEditor ImplementNotificationEntities(IEntityType entityType)
        {
            AddUsingNamespaces(
                "System.ComponentModel",
                "System.Runtime.CompilerServices",
                "Microsoft.EntityFrameworkCore.ChangeTracking");

            AddInheritedTypes(entityType,
                "INotifyPropertyChanging",
                "INotifyPropertyChanged");

            ReplaceAutoPropertiesAndImplementChangeNotifications(entityType);

            Text = Text.Replace("HashSet", "ObservableHashSet");
            return this;
        }
    }
}
