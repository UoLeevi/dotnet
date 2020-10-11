namespace DotNetApp.Expressions
{
    public partial class JsonPathAst
    {
        public abstract class Node
        {
            public abstract NodeType NodeType { get; }
        }
    }
}
