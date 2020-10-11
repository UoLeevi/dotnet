using System.Linq.Expressions;

namespace DotNetApp.Expressions
{
    public partial class JsonPathAst
    {
        public abstract class ExpressionNode : Node
        {
            public abstract Expression ToExpression(Expression expression);
        }
    }
}
