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
    }
}
