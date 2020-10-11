using System;
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
        [Theory]
        [InlineData("Property", "Property")]
        [InlineData("$.Property", "Property")]
        [InlineData("$['Property']", "Property")]
        [InlineData("['Property']", "Property")]
        public void CanParseSinglePropertySelector(string jsonpathExpression, string expectedPropertyName)
        {
            JsonPath jsonPath = JsonPath.Parse(jsonpathExpression);

            Assert.True(jsonPath.Root.Nodes.Single() is JsonPathAst.PropertySelector selector
                && selector.PropertyName == expectedPropertyName);
        }

        [Theory]
        [InlineData("Property1.Property2.Property3", new string[] { "Property1", "Property2", "Property3" })]
        [InlineData("$.Property1.Property2.Property3", new string[] { "Property1", "Property2", "Property3" })]
        [InlineData("$['Property1']['Property2']['Property3']", new string[] { "Property1", "Property2", "Property3" })]
        [InlineData("['Property1'].Property2['Property3']", new string[] { "Property1", "Property2", "Property3" })]
        public void CanParseChainedPropertySelectors(string jsonpathExpression, string[] expectedPropertyNames)
        {
            JsonPath jsonPath = JsonPath.Parse(jsonpathExpression);

            Assert.Equal(jsonPath.Root.Nodes
                .Cast<JsonPathAst.PropertySelector>()
                .Select(s => s.PropertyName).ToArray(),
                expectedPropertyNames);
        }

        [Theory]
        [InlineData("Property1[*].Property2", new[] { nameof(JsonPathAst.PropertySelector), nameof(JsonPathAst.ItemSelector), nameof(JsonPathAst.PropertySelector) })]
        [InlineData("Property1.Property2", new[] { nameof(JsonPathAst.PropertySelector), nameof(JsonPathAst.PropertySelector) })]
        public void CanParseRootNodeTypesCorrectly(string jsonpathExpression, object[] expectedNodeTypeNames)
        {
            JsonPath jsonPath = JsonPath.Parse(jsonpathExpression);

            Assert.Equal(jsonPath.Root.Nodes
                .Select(n => n.NodeType.Name).ToArray(),
                expectedNodeTypeNames);
        }

        [Fact]
        public void CanCreateExpression()
        {
            string jsonpathExpression = "$.Property1.Property1.Property2";
            JsonPath jsonPath = JsonPath.Parse(jsonpathExpression);
            Expression<Func<JsonPathTest, bool>> expression = jsonPath.ToExpression(typeof(JsonPathTest)) as Expression<Func<JsonPathTest, bool>>;

            Assert.True(expression.Compile().Invoke(this));
        }

        // Dummy property for tests
        public JsonPathTest Property1 => this;
        public bool Property2 => true;
    }
}
