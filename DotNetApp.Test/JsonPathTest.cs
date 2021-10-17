using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DotNetApp.Expressions;
using Xunit;

namespace DotNetApp.Test
{
    // https://support.smartbear.com/alertsite/docs/monitors/api/endpoint/jsonpath.html
    // https://jsonpath.com/
    public class JsonPathTest
    {
        public class Dummy
        {
            public Dummy()
            {
                Collection = new List<Dummy>();
            }

            public string Property { get; set; }

            public Dummy Other { get; set; }

            public IEnumerable<Dummy> Collection { get; set; }

            public string ComputedProperty => Other?.Property;

            public string LongestProperty => Collection?.Select(d => d.Property).OrderByDescending(p => p.Length).FirstOrDefault();
        }

        [Theory]
        [InlineData("Property", "Property")]
        [InlineData("$.Property", "Property")]
        [InlineData("$['Property']", "Property")]
        [InlineData("['Property']", "Property")]
        public void CanParseSinglePropertySelector(string jsonpathExpression, string expectedPropertyName)
        {
            JsonPath jsonPath = new JsonPath(typeof(Dummy), jsonpathExpression);

            Assert.True(jsonPath.Root.Nodes.Single() is JsonPathPropertySelectorNode propertySelectorNode
                && propertySelectorNode.PropertyName == expectedPropertyName);

            string value;

            var dummy1 = new Dummy();
            value = (string)JsonPath.Evaluate(dummy1, jsonpathExpression);
            Assert.True(value == dummy1.Property);

            dummy1.Property = "dummy1 value";
            value = (string)JsonPath.Evaluate(dummy1, jsonpathExpression);
            Assert.True(value == dummy1.Property);

            var dummy2 = new Dummy();
            value = (string)JsonPath.Evaluate(dummy2, jsonpathExpression);
            Assert.True(value == dummy2.Property);

            dummy2.Property = "dummy2 value";
            value = (string)JsonPath.Evaluate(dummy2, jsonpathExpression);
            Assert.True(value == dummy2.Property);
        }

        [Theory]
        [InlineData("Other.Property", new string[] { "Other", "Property" })]
        [InlineData("$.Other.Property", new string[] { "Other", "Property" })]
        [InlineData("$['Other']['Property']", new string[] { "Other", "Property" })]
        [InlineData("['Other'].Property", new string[] { "Other", "Property" })]
        public void CanParseChainedPropertySelectors(string jsonpathExpression, string[] expectedPropertyNames)
        {
            JsonPath jsonPath = new JsonPath(typeof(Dummy), jsonpathExpression);

            Assert.Equal(jsonPath.Root.Nodes
                .Cast<JsonPathPropertySelectorNode>()
                .Select(s => s.PropertyName).ToArray(),
                expectedPropertyNames);

            string value;

            var dummy1 = new Dummy();
            var dummy2 = new Dummy { Other = dummy1 };
            dummy1.Property = "dummy1 value";

            value = (string)JsonPath.Evaluate(dummy2, jsonpathExpression);
            Assert.True(value == dummy2.Other.Property);

            dummy2.Property = "dummy2 value";
            value = (string)JsonPath.Evaluate(dummy2, jsonpathExpression);
            Assert.True(value == dummy2.Other.Property);
        }

        [Fact]
        public void CanCreateExpression()
        {
            string jsonpathExpression = "$.Property1.Property1.Property2";
            JsonPath jsonPath = JsonPath.Create<JsonPathTest>(jsonpathExpression);
            Expression<Func<JsonPathTest, bool>> expression = jsonPath.Expression as Expression<Func<JsonPathTest, bool>>;

            Assert.True(expression.Compile().Invoke(this));
        }

        // Dummy property for tests
        public JsonPathTest Property1 => this;
        public bool Property2 => true;
    }
}
