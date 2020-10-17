using System;

namespace DotNetApp.Extensions
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class DependsOnAttribute : Attribute
    {
        /// <summary>
        /// Indicates that the property value depends on other properties and that PropertyChanged events should be forwarded.
        /// </summary>
        /// <param name="propertyDependencies">An array of property names or jsonpath expressions which describe the dependencies of the property.</param>
        public DependsOnAttribute(params string[] propertyDependencies)
        {
            PropertyDependencies = propertyDependencies;
        }

        internal string[] PropertyDependencies { get; }
    }
}
