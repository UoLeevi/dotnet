using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using DotNetApp.Collections.Extensions;

namespace DotNetApp.Test
{
    public class TextUtilitiesTest
    {
        [Theory]
        [InlineData("te  st", "te st")]
        [InlineData("\tt\t\test2", "t est2")]
        [InlineData("", "")]
        [InlineData("\t\t", "")]
        [InlineData("\t\ta", "a")]
        [InlineData("a\t\t", "a")]
        [InlineData("\ta\t", "a")]
        public void CanNormalizeText(string a, string expected)
        {
            var normalized = Utilities.Text.Normalize(a);
            Assert.Equal(normalized, expected);
        }

        [Theory]
        [InlineData("test", "test", 0)]
        [InlineData("test2", "test", 1)]
        [InlineData("kitten", "sitting", 3)]
        [InlineData("saturday", "sunday", 3)]
        public void CanCalculateLevenshteinDistance(string a, string b, int expected)
        {
            Assert.Equal(Utilities.Text.LevenshteinDistance(a, b), expected);
        }

        [Theory]
        [InlineData("test", "test", 4.0 / 4.0)]
        [InlineData("test2", "test", 4.0 / 5.0)]
        [InlineData("kitten", "sitting", 4.0 / 7.0)]
        [InlineData("saturday", "sunday", 5.0 / 8.0)]
        public void CanCalculateLevenshteinDistanceScore(string a, string b, double expected)
        {
            Assert.Equal(Utilities.Text.LevenshteinDistanceScore(a, b), expected);
        }
    }
}
