using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Code.DotNet.App.EntityFrameworkCore
{
    public class CSharpEntityTypeCodeEditor : TextEditor<CSharpEntityTypeCodeEditor>
    {
        public CSharpEntityTypeCodeEditor()
        {
        }

        public virtual CSharpEntityTypeCodeEditor MoveToEndOfClassDefinition()
        {
            MoveToPattern("\r\n    }\r\n");
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
            Select($"(public partial class {entityType.Name})( *: *)?");
            return Write($"{Match.Groups[1].Value} : {string.Join(", ", typeNames)}");
        }

        public virtual CSharpEntityTypeCodeEditor ReplaceAutoPropertiesAndImplementChangeNotifications(IEntityType entityType)
        {
            MoveToEndOfClassDefinition();
            WriteLines("",
                "",
                "        #region Private fields",
                "");

            foreach (IProperty scalarProperty in entityType.GetProperties())
            {
                ImplementPropertyGetterAndSetter(scalarProperty);
            }

            foreach (INavigation navigationProperty in entityType.GetNavigations())
            {
                ImplementPropertyGetterAndSetter(navigationProperty);
            }

            MoveToEndOfClassDefinition();
            WriteLines("",
                "        #endregion",
                "",
                "        #region Property Change Notifications",
                "",
                "        public event PropertyChangingEventHandler PropertyChanging;",
                "        public event PropertyChangedEventHandler PropertyChanged;",
                "",
                "        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = \"\")",
                "        {",
                "            if (EqualityComparer<T>.Default.Equals(field, value)) return;",
                "            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));",
                "            field = value;",
                "            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));",
                "        }",
                "",
                "        #endregion");

            return this;

            void ImplementPropertyGetterAndSetter(IPropertyBase property)
            {
                string typeNameString = Regex.Match(Text, $"public (?:virtual )?([^\\s]+) {property.Name} ").Groups[1].Value;

                Select($"(public (?:virtual )?[^\\s]+ {property.Name} ){{ get; set; }}");
                Write($"{Match.Groups[1].Value,-80}{{ get => _{property.Name}; set => SetProperty(ref _{property.Name}, value); }}");

                MoveToEndOfClassDefinition();
                WriteLine($"        private {typeNameString} _{property.Name};");
            }
        }

        public virtual CSharpEntityTypeCodeEditor ImplementChangeNotifications(IEntityType entityType)
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
