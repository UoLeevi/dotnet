using System;
using DotNetApp.Expressions;

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
            PropertyDependencyPaths = new JsonPath[propertyDependencies.Length];

            for (int i = 0; i < propertyDependencies.Length; ++i)
            {
                PropertyDependencyPaths[i] = new JsonPath(propertyDependencies[i]);
            }
        }

        internal JsonPath[] PropertyDependencyPaths { get; }
    }
}
