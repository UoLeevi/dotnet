using System;

namespace DotNetApp
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class GeneratePropertyAttribute : Attribute
    {
        public GeneratePropertyAttribute()
        {

        }

        public string PropertyName { get; set; }
        public string AccessModifier { get; set; }
        public string AccessModifierSet { get; set; }
        public string AccessModifierGet { get; set; }
    }
}
