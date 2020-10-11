namespace DotNetApp.Expressions
{
    public partial class JsonPathAst
    {
        public class NodeType
        {
            internal static NodeType Root = new NodeType
            {
                Name = nameof(Root),
                Parse = JsonPathAst.Root.Parse,
                Peek = JsonPathAst.Root.Peek
            };
            internal static NodeType ItemSelector = new NodeType
            {
                Name = nameof(ItemSelector),
                Parse = JsonPathAst.ItemSelector.Parse,
                Peek = JsonPathAst.ItemSelector.Peek
            };
            internal static NodeType PropertySelector = new NodeType
            {
                Name = nameof(PropertySelector),
                Parse = JsonPathAst.PropertySelector.Parse,
                Peek = JsonPathAst.PropertySelector.Peek
            };


            internal static NodeType NthIndex = new NodeType
            {
                Name = nameof(NthIndex),
                //Parse = JsonPathAst.NthIndex.Parse,
                //Peek = JsonPathAst.NthIndex.Peek
            };
            internal static NodeType MultipleIndexes = new NodeType
            {
                Name = nameof(MultipleIndexes),
                //Parse = JsonPathAst.MultipleIndexes.Parse,
                //Peek = JsonPathAst.MultipleIndexes.Peek
            };
            internal static NodeType RecursiveDescent = new NodeType
            {
                Name = nameof(RecursiveDescent),
                Parse = JsonPathAst.RecursiveDescent.Parse,
                Peek = JsonPathAst.RecursiveDescent.Peek
            };
            internal static NodeType Wildcard = new NodeType
            {
                Name = nameof(Wildcard),
                //Parse = JsonPathAst.Wildcard.Parse,
                //Peek = JsonPathAst.Wildcard.Peek
            };
            internal static NodeType Range = new NodeType
            {
                Name = nameof(Range),
                //Parse = JsonPathAst.Range.Parse,
                //Peek = JsonPathAst.Range.Peek
            };
            internal static NodeType FilterExpression = new NodeType
            {
                Name = nameof(FilterExpression),
                //Parse = JsonPathAst.FilterExpression.Parse,
                //Peek = JsonPathAst.FilterExpression.Peek
            };
            internal static NodeType ScriptExpression = new NodeType
            {
                Name = nameof(ScriptExpression),
                //Parse = JsonPathAst.ScriptExpression.Parse,
                //Peek = JsonPathAst.ScriptExpression.Peek
            };
            internal static NodeType Current = new NodeType
            {
                Name = nameof(Current),
                //Parse = JsonPathAst.Current.Parse,
                //Peek = JsonPathAst.Current.Peek
            };

            public string Name;
            internal Peeker Peek;
            internal Parser<Node> Parse;
        }
    }
}
