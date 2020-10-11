using System;

namespace DotNetApp.Expressions
{
    public class JsonPathInvalidSyntaxException : Exception
    {
        internal JsonPathInvalidSyntaxException(ReadOnlySpan<char> excerpt = default) : base(
            excerpt == default
                ? $"Invalid jsonpath syntax."
                : $"Invalid jsonpath syntax near `{excerpt.ToString()}`.")
        {
        }
    }
}
