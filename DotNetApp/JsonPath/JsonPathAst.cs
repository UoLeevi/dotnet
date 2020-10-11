using System;

namespace DotNetApp.Expressions
{
    public partial class JsonPathAst
    {
        internal delegate TNode Parser<out TNode>(ref ReadOnlySpan<char> expr)
            where TNode : Node;

        internal delegate bool Peeker(ReadOnlySpan<char> expr);
    }
}
