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
        [InlineData("test", "test", 0)]
        [InlineData("test2", "test", 1)]
        [InlineData("kitten", "sitting", 3)]
        [InlineData("saturday", "sunday", 3)]
        public void CanCalculateLevenshteinDistance(string a, string b, int expected)
        {
            Assert.Equal(Utilities.Text.LevenshteinDistance(a, b), expected);
        }
    }
}
