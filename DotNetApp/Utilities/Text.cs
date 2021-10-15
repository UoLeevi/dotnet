using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetApp.Utilities
{
    public static class Text
    {
        [Flags]
        public enum MatchingOptions
        {
            Exact = 0,
            CaseInsensitive = 1,
            Trim = 2,
            Default = CaseInsensitive | Trim
        }

        public static int LevenshteinDistance(string a, string b, MatchingOptions flags = MatchingOptions.Default)
        {
            a = a ?? string.Empty;
            b = b ?? string.Empty;

            if (flags.HasFlag(MatchingOptions.CaseInsensitive))
            {
                a = a.ToLower();
                b = b.ToLower();
            }

            if (flags.HasFlag(MatchingOptions.Trim))
            {
                a = a.Trim();
                b = b.Trim();
            }

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
                    matrix[i,j] = cost;
                }
            }

            return matrix[a.Length, b.Length];
        }
    }
}
