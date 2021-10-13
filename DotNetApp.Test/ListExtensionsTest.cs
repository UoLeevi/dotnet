using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using DotNetApp.Collections.Extensions;

namespace DotNetApp.Test
{
    public class ListExtensionsTest
    {
        [Fact]
        public void InplaceUpdateSortedWorks()
        {
            List<int> a = new List<int> { 1, 32, 2, 11, 4, 6, 42, 7 };
            List<int> b = new List<int> { -1, 1, 0, 2, 112, 4, 12, 6, 7 };

            a.Sort();
            b.Sort();

            a.InplaceUpdateSorted(b, Comparer<int>.Default);

            Assert.Equal<int>(a, b);
        }
    }
}
