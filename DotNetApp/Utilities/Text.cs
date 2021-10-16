using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotNetApp.Utilities
{
    public static class Text
    {
        [Flags]
        public enum NormalizationOptions
        {
            Exact = 0,
            Lower = 1,
            Trim = 2,
            Default = Lower | Trim
        }

        public static string Normalize(string a, NormalizationOptions flags = NormalizationOptions.Default)
        {
            a = a ?? string.Empty;

            if (flags.HasFlag(NormalizationOptions.Trim))
            {
                bool prevIsWhiteSpace = false;
                bool currIsWhiteSpace;

                var span = a.AsSpan();

                // Remove leading white space
                for (int i = 0; i < span.Length; ++i)
                {
                    if (!char.IsWhiteSpace(span[i]))
                    {
                        span = span.Slice(i);
                        break;
                    }
                }

                // Remove trailing white space
                for (int i = span.Length; i <= 0; --i)
                {
                    if (!char.IsWhiteSpace(span[i]))
                    {
                        span = span.Slice(0, i + 1);
                        break;
                    }
                }

                var sb = new StringBuilder(span.Length);

                // Remove consecutive white space and replace white space with space characters
                for (int i = 0; i < span.Length; ++i)
                {
                    char c = span[i];
                    currIsWhiteSpace = char.IsWhiteSpace(c);

                    if (!prevIsWhiteSpace || !currIsWhiteSpace)
                    {
                        if (currIsWhiteSpace)
                        {
                            sb.Append(' ');
                        }
                        else
                        {
                            sb.Append(c);
                        }
                    }

                    prevIsWhiteSpace = currIsWhiteSpace;
                }

                a = sb.ToString();
            }

            if (flags.HasFlag(NormalizationOptions.Lower))
            {
                a = a.ToLower();
            }

            return a;
        }

        public static double[,] LevenshteinDistanceScore(IEnumerable<string> a, IEnumerable<string> b, NormalizationOptions flags = NormalizationOptions.Default)
        {
            var aArray = a.Select(text => Normalize(text, flags)).ToArray();
            var bArray = b.Select(text => Normalize(text, flags)).ToArray();
            var scores = new double[aArray.Length, bArray.Length];

            for (int i = 0; i < aArray.Length; ++i)
            {
                for (int j = 0; j < bArray.Length; ++j)
                {
                    scores[i, j] = LevenshteinDistanceScore(aArray[i].AsSpan(), bArray[j].AsSpan());
                }
            }

            return scores;
        }

        public static double LevenshteinDistanceScore(string a, string b, NormalizationOptions flags = NormalizationOptions.Default)
        {
            a = Normalize(a, flags);
            b = Normalize(b, flags);
            return LevenshteinDistanceScore(a.AsSpan(), b.AsSpan());
        }

        private static double LevenshteinDistanceScore(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
        {
            double ld = LevenshteinDistance(a, b);
            double maxlen = a.Length > b.Length ? a.Length : b.Length;
            if (maxlen == 0) return 0;
            return 1 - ld / maxlen;
        }

        public static int LevenshteinDistance(string a, string b, NormalizationOptions flags = NormalizationOptions.Default)
        {
            a = Normalize(a, flags);
            b = Normalize(b, flags);

            return LevenshteinDistance(a.AsSpan(), b.AsSpan());
        }

        private static int LevenshteinDistance(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
        {
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;

            int[,] matrix = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; ++i)
            {
                matrix[i, 0] = i;
            }

            for (int j = 1; j <= b.Length; ++j)
            {
                matrix[0, j] = j;
            }

            for (int i = 1; i <= a.Length; ++i)
            {
                for (int j = 1; j <= b.Length; ++j)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    cost += matrix[i - 1, j - 1];
                    int cost_i = 1 + matrix[i - 1, j];
                    int cost_j = 1 + matrix[i, j - 1];
                    cost = cost < cost_i ? cost : cost_i;
                    cost = cost < cost_j ? cost : cost_j;
                    matrix[i, j] = cost;
                }
            }

            return matrix[a.Length, b.Length];
        }
    }
}
