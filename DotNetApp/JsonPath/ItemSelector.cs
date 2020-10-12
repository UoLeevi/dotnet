using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DotNetApp.Collections;
using DotNetApp.Collections.Extensions;

namespace DotNetApp.Expressions
{
    public partial class JsonPathAst
    {
        public class ItemSelector : ExpressionNode
        {
            private static MethodInfo methodDefinitionSelect;
            private static MethodInfo methodDefinitionWhere;

            static ItemSelector()
            {
                methodDefinitionSelect = typeof(Enumerable)
                    .GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .Where(x => x.Name == nameof(Enumerable.Select))
                    .Where(x => x.GetParameters() is var parameters
                        && parameters.Length == 2
                        && parameters[0].ParameterType.IsGenericType
                        && parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                        && parameters[1].ParameterType.IsGenericType
                        && parameters[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
                    .Single();

                methodDefinitionWhere = typeof(Enumerable)
                    .GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .Where(x => x.Name == nameof(Enumerable.Where))
                    .Where(x => x.GetParameters() is var parameters
                        && parameters.Length == 2
                        && parameters[0].ParameterType.IsGenericType
                        && parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                        && parameters[1].ParameterType.IsGenericType
                        && parameters[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
                    .Single();
            }

            public override NodeType NodeType => JsonPathAst.NodeType.ItemSelector;

            internal static ItemSelector Parse(ref ReadOnlySpan<char> expr)
            {
                expr = expr.Slice(1);

                int nesting = 1;
                int i = 0;
                for (; i < expr.Length && nesting != 0; ++i)
                {
                    if (expr[i] == '[') ++nesting;
                    else if (expr[i] == ']') --nesting;
                }

                if (nesting != 0) throw new JsonPathInvalidSyntaxException(expr);

                ReadOnlyMemory<char> nodeExprMem = expr.Slice(0, i).ToArray();
                expr = expr.Slice(i);

                return new ItemSelector
                {
                    Nodes = ParseNodes(expr.ToArray()).ToCachedEnumerable(),
                    filterNode = new Lazy<Node>(() =>
                    {
                        var nodeExpr = nodeExprMem.Span;

                        foreach (NodeType nodeType in AllowedChildren)
                        {
                            if (nodeType.Peek(nodeExpr))
                            {
                                return nodeType.Parse(ref nodeExpr);
                            }
                        }

                        throw new JsonPathInvalidSyntaxException(nodeExpr);
                    })
                };
            }

            internal static bool Peek(ReadOnlySpan<char> expr)
                => expr.Length > 2
                && expr[0] == '['
                && expr[1] != '\'';

            private Lazy<Node> filterNode;
            public Node FilterNode => filterNode.Value;
            
            public IEnumerable<ExpressionNode> Nodes { get; private set; }

            private static IEnumerable<ExpressionNode> ParseNodes(ReadOnlyMemory<char> exprMem)
            {
                ReadOnlySpan<char> expr = exprMem.Span;

                do
                {
                    foreach ((Peeker Peek, Parser<ExpressionNode> Parse) in AllowedNodes)
                    {
                        expr = exprMem.Span;
                        if (Peek(expr))
                        {
                            ExpressionNode node = Parse(ref expr);
                            exprMem = expr.ToArray();
                            yield return node;
                            goto next;
                        }
                    }

                    throw new JsonPathInvalidSyntaxException(exprMem.Span);
                    next:;
                } while (!exprMem.IsEmpty);
            }

            public override Expression ToExpression(Expression expression)
            {

                Type collectionType = expression.Type;
                ParameterExpression collectionParameter = Expression.Parameter(collectionType);
                Type itemType = EnumerableExtensions.GetItemType(collectionType);
                ParameterExpression itemParameter = Expression.Parameter(itemType);

                Expression itemExpression = null; // TODO

                LambdaExpression selectorLambda = Expression.Lambda(itemExpression, itemParameter);

                MethodInfo methodSelect = methodDefinitionSelect.MakeGenericMethod(itemType, itemExpression.Type);
                return Expression.Call(methodSelect, collectionParameter, selectorLambda);
            }

            private static readonly NodeType[] AllowedChildren = new[] { NodeType.Wildcard, NodeType.NthIndex, NodeType.MultipleIndexes, NodeType.Range, NodeType.FilterExpression, NodeType.ScriptExpression };

            private static readonly (Peeker Peek, Parser<ExpressionNode> Parse)[] AllowedNodes = new (Peeker, Parser<ExpressionNode>)[]
            {
                (RecursiveDescent.Peek, RecursiveDescent.Parse),
                (PropertySelector.Peek, PropertySelector.Parse),
                (ItemSelector.Peek, ItemSelector.Parse)
            };
        }
    }
}
