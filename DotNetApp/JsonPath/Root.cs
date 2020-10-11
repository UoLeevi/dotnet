using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DotNetApp.Collections;

namespace DotNetApp.Expressions
{
    public partial class JsonPathAst
    {
        public class Root : ExpressionNode
        {
            public override NodeType NodeType => JsonPathAst.NodeType.Root;

            internal static Root Parse(ref ReadOnlySpan<char> expr)
            {
                if (expr[0] == '$')
                {
                    expr = expr.Slice(1);
                }
                else
                {
                    // implicit root

                    if (expr[0] == '.') throw new JsonPathInvalidSyntaxException(expr);

                    if (expr[0] != '[')
                    {
                        char[] buffer = new char[expr.Length + 1];
                        buffer[0] = '.';
                        expr.CopyTo(buffer.AsSpan().Slice(1));
                        expr = buffer.AsSpan();
                    }
                }

                return new Root
                {
                    Nodes = ParseNodes(expr.ToArray()).ToCachedEnumerable()
                };
            }

            internal static bool Peek(ReadOnlySpan<char> expr)
                => !expr.IsEmpty;

            public IEnumerable<ExpressionNode> Nodes { get; private set; }

            private static IEnumerable<ExpressionNode> ParseNodes(ReadOnlyMemory<char> exprMem)
            {
                ReadOnlySpan<char> expr = exprMem.Span;

                do
                {
                    foreach ((Peeker Peek, Parser<ExpressionNode> Parse) in AllowedChildren)
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

            private static readonly (Peeker Peek, Parser<ExpressionNode> Parse)[] AllowedChildren = new (Peeker, Parser<ExpressionNode>)[] 
            { 
                (RecursiveDescent.Peek, RecursiveDescent.Parse),
                (PropertySelector.Peek, PropertySelector.Parse),
                (ItemSelector.Peek, ItemSelector.Parse)
            };

            public override Expression ToExpression(Expression expression)
            {
                foreach (ExpressionNode node in Nodes)
                {
                    expression = node.ToExpression(expression);
                }

                return expression;
            }
        }
    }
}
