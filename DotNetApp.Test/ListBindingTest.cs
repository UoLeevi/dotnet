﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DotNetApp.Collections;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Xunit;

namespace DotNetApp.Test
{
    public class ListBindingTest
    {
        [Fact]
        public void DoesTrackChangesOnSingleSource()
        {
            ObservableCollection<int> source = new ObservableCollection<int>();

            List<int> projection1 = new List<int>();
            List<int> projection2 = new List<int>();

            ListBinding<int> binding = new ListBinding<int>()
                .AddSource(source);

            source.Add(0);
            source.Add(1);
            source.Add(2);
            source.Add(3);
            source.Add(4);

            binding.AddTarget(projection1);

            source.Add(5);
            source.Add(6);

            binding.AddTarget(projection2);

            source.Add(7);
            source.Add(8);

            Assert.Equal(source, projection1);
            Assert.Equal(source, projection2);

            source.Remove(4);
            source.Remove(5);
            source.Remove(6);

            Assert.Equal(source, projection1);
            Assert.Equal(source, projection2);

            source.Insert(4, 4);
            source.Insert(5, 5);
            source.Insert(6, 6);

            Assert.Equal(source, projection1);
            Assert.Equal(source, projection2);

            source[4] = 0;
            source[5] = 0;
            source[5] = 5;
            source[4] = 4;

            Assert.Equal(source, projection1);
            Assert.Equal(source, projection2);

            source.Clear();

            Assert.Equal(source, projection1);
            Assert.Equal(source, projection2);
        }

        [Fact]
        public void DoesTrackChangesOnMultipleSources()
        {
            ObservableCollection<int> source1 = new ObservableCollection<int>();
            ObservableCollection<int> source2 = new ObservableCollection<int>();
            ObservableCollection<int> source3 = new ObservableCollection<int>();

            List<int> projection1 = new List<int>();
            List<int> projection2 = new List<int>();

            ListBinding<int> binding = new ListBinding<int>()
                .AddSource(source1)
                .AddSource(source2)
                .AddSource(source3);

            source1.Add(0);
            source2.Add(1);
            source3.Add(2);
            source3.Add(3);
            source2.Add(4);

            binding.AddTarget(projection1);

            source1.Add(5);
            source2.Add(6);

            binding.AddTarget(projection2);

            source1.Add(7);
            source3.Add(8);

            Assert.Equal(source1.Concat(source2).Concat(source3), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3), projection2);

            source2.Remove(4);
            source1.Remove(5);
            source2.Remove(6);

            Assert.Equal(source1.Concat(source2).Concat(source3), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3), projection2);

            source1.Insert(1, 4);
            source3.Insert(0, 5);
            source1.Insert(1, 6);

            Assert.Equal(source1.Concat(source2).Concat(source3), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3), projection2);

            source1[1] = 0;
            source1[2] = 0;
            source1[0] = 5;
            source3[1] = 4;

            Assert.Equal(source1.Concat(source2).Concat(source3), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3), projection2);

            source1.Clear();

            Assert.Equal(source1.Concat(source2).Concat(source3), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3), projection2);
            
            source3.Clear();
            source2.Clear();

            Assert.Equal(source1.Concat(source2).Concat(source3), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3), projection2);
        }

        [Fact]
        public void DoesSupportTransformation()
        {
            ObservableCollection<int> source1 = new ObservableCollection<int>();
            ObservableCollection<int> source2 = new ObservableCollection<int>();
            ObservableCollection<int> source3 = new ObservableCollection<int>();
            
            List<string> projection1 = new List<string>();
            List<string> projection2 = new List<string>();

            Func<int, string>transformation = i => i.ToString();

            ListBinding<string>binding = new ListBinding<string>()
                .AddSource(source1, transformation)
                .AddSource(source2, transformation)
                .AddSource(source3, transformation);

            source1.Add(0);
            source2.Add(1);
            source3.Add(2);
            source3.Add(3);
            source2.Add(4);

            binding.AddTarget(projection1);

            source1.Add(5);
            source2.Add(6);

            binding.AddTarget(projection2);

            source1.Add(7);
            source3.Add(8);

            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection2);

            source2.Remove(4);
            source1.Remove(5);
            source2.Remove(6);

            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection2);

            source1.Insert(1, 4);
            source3.Insert(0, 5);
            source1.Insert(1, 6);

            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection2);

            source1[1] = 0;
            source1[2] = 0;
            source1[0] = 5;
            source3[1] = 4;

            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection2);

            source1.Clear();

            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection2);

            source3.Clear();
            source2.Clear();

            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection2);
        }

        [Fact]
        public void DoesSupportFiltering()
        {
            List<string> expected;

            ObservableCollection<int> source1 = new ObservableCollection<int>();
            ObservableCollection<int> source2 = new ObservableCollection<int>();
            ObservableCollection<int> source3 = new ObservableCollection<int>();

            List<string> projection1 = new List<string>();
            List<string> projection2 = new List<string>();

            Func<int, string>transformation = i => i.ToString();
            Func<int, bool> filterPredicate = i => i <= 2;

            ListBinding<string>binding = new ListBinding<string>()
                .AddSource(source1, convert: transformation, filter: filterPredicate)
                .AddSource(source2, convert: transformation, filter: filterPredicate)
                .AddSource(source3, convert: transformation, filter: filterPredicate);

            source1.Add(0);
            source2.Add(1);
            source3.Add(2);
            source3.Add(3);
            source2.Add(4);

            binding.AddTarget(projection1);

            source1.Add(5);
            source2.Add(6);

            binding.AddTarget(projection2);

            source1.Add(7);
            source3.Add(8);

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            source2.Remove(4);
            source1.Remove(5);
            source2.Remove(6);

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            source1.Insert(1, 4);
            source3.Insert(0, 5);
            source1.Insert(1, 6);

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            source1[1] = 0;
            source1[2] = 0;
            source1[0] = 5;
            source3[1] = 4;

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            source1.Clear();

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            source3.Clear();
            source2.Clear();

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);
        }

        [Fact]
        public void DoesSupportForking()
        {
            List<string> expected;
            List<int> expectedForked;

            ObservableCollection<int> source1 = new ObservableCollection<int>();
            ObservableCollection<int> source2 = new ObservableCollection<int>();
            ObservableCollection<int> source3 = new ObservableCollection<int>();

            List<string> projection1 = new List<string>();
            List<string> projection2 = new List<string>();
            List<int> projection3 = new List<int>();

            Func<int, string> transformation = i => i.ToString();
            Func<int, bool> filterPredicate = i => i <= 2;

            ListBinding<string> binding = new ListBinding<string>()
                .AddSource(source1, convert: transformation, filter: filterPredicate)
                .AddSource(source2, convert: transformation, filter: filterPredicate)
                .AddSource(source3, convert: transformation, filter: filterPredicate);

            source1.Add(0);
            source2.Add(1);
            source3.Add(2);
            source3.Add(3);
            source2.Add(4);

            binding.AddTarget(projection1);

            source1.Add(5);
            source2.Add(6);

            binding.AddTarget(projection2);

            source1.Add(7);
            source3.Add(8);

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            var bindingForked = binding.Select<int>(item => int.Parse(item));

            source2.Remove(4);
            source1.Remove(5);
            source2.Remove(6);

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            source1.Insert(1, 4);
            source3.Insert(0, 5);
            source1.Insert(1, 6);

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            source1[1] = 0;
            source1[2] = 0;
            source1[0] = 5;
            source3[1] = 4;

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            source1.Clear();

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            source3.Clear();
            source2.Clear();

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            expectedForked = source1.Concat(source2).Concat(source3).Where(filterPredicate).ToList();
            expectedForked.Sort();
            Assert.Equal(expectedForked, projection3);
        }

        [Fact]
        public void DoesSupportReordering()
        {
            List<string> expected;
            List<int> expectedForked;

            ObservableCollection<int> source1 = new ObservableCollection<int>();
            ObservableCollection<int> source2 = new ObservableCollection<int>();
            ObservableCollection<int> source3 = new ObservableCollection<int>();

            List<string> projection1 = new List<string>();
            List<string> projection2 = new List<string>();
            List<int> projection3 = new List<int>();

            Func<int, string> transformation = i => i.ToString();
            Func<int, bool> filterPredicate = i => i <= 2;

            ListBinding<string> binding = new ListBinding<string>()
                .AddSource(source1, convert: transformation, filter: filterPredicate)
                .AddSource(source2, convert: transformation, filter: filterPredicate)
                .AddSource(source3, convert: transformation, filter: filterPredicate);

            source1.Add(0);
            source2.Add(1);
            source3.Add(2);
            source3.Add(3);
            source2.Add(4);

            binding.AddTarget(projection1);

            source1.Add(5);
            source2.Add(6);

            binding.AddTarget(projection2);

            source1.Add(7);
            source3.Add(8);

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            var bindingForked = binding.Select<int>(item => int.Parse(item));

            source2.Remove(4);
            source1.Remove(5);
            source2.Remove(6);

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            source1.Insert(1, 4);
            source3.Insert(0, 5);
            source1.Insert(1, 6);

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            binding.OrderByDescending(x => x);

            source1[1] = 0;
            source1[2] = 0;
            source1[0] = 5;
            source3[1] = 4;

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            expected.Reverse();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            source1.Clear();

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            expected.Reverse();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            source3.Clear();
            source2.Clear();

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            expected.Reverse();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            expectedForked = source1.Concat(source2).Concat(source3).Where(filterPredicate).ToList();
            expectedForked.Sort();
            Assert.Equal(expectedForked, projection3);
        }

        [Fact]
        public void DoesSupportObservableHashSet()
        {
            List<string> expected;
            List<int> expectedForked;

            ObservableHashSet<int> source1 = new ObservableHashSet<int>();
            ObservableHashSet<int> source2 = new ObservableHashSet<int>();
            ObservableHashSet<int> source3 = new ObservableHashSet<int>();

            List<string> projection1 = new List<string>();
            List<string> projection2 = new List<string>();
            List<int> projection3 = new List<int>();

            Func<int, string> transformation = i => i.ToString();
            Func<int, bool> filterPredicate = i => i <= 2;

            ListBinding<string> binding = new ListBinding<string>()
                .AddSource(source1, convert: transformation, filter: filterPredicate)
                .AddSource(source2, convert: transformation, filter: filterPredicate)
                .AddSource(source3, convert: transformation, filter: filterPredicate);

            source1.Add(0);
            source2.Add(1);
            source3.Add(2);
            source3.Add(3);
            source2.Add(4);

            binding.AddTarget(projection1);

            source1.Add(5);
            source2.Add(6);

            binding.AddTarget(projection2);

            source1.Add(7);
            source3.Add(8);

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            var bindingForked = binding.Select<int>(item => int.Parse(item));

            source2.Remove(4);
            source1.Remove(5);
            source2.Remove(6);

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            source1.Add(4);
            source3.Add(5);
            source1.Add(6);

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            source1.Clear();

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            source3.Clear();
            source2.Clear();

            expected = source1.Concat(source2).Concat(source3).Where(filterPredicate).Select(transformation).ToList();
            expected.Sort();
            Assert.Equal(expected, projection1);
            Assert.Equal(expected, projection2);

            expectedForked = source1.Concat(source2).Concat(source3).Where(filterPredicate).ToList();
            expectedForked.Sort();
            Assert.Equal(expectedForked, projection3);
        }

        [Fact]
        public void DoesSupportNullItems()
        {
            ObservableCollection<string> source1 = new ObservableCollection<string>();
            ObservableCollection<string> source2 = new ObservableCollection<string>();
            ObservableCollection<string> source3 = new ObservableCollection<string>();

            List<string> projection1 = new List<string>();
            List<string> projection2 = new List<string>();

            Func<string, string> transformation = i => i?.ToString();

            ListBinding<string> binding = new ListBinding<string>()
                .AddSource(source1, transformation)
                .AddSource(source2, transformation)
                .AddSource(source3, transformation);

            source1.Add("0");
            source2.Add("1");
            source3.Add("2");
            source3.Add("3");
            source2.Add("4");

            binding.AddTarget(projection1);

            source1.Add(null);
            source2.Add(null);

            binding.AddTarget(projection2);

            source1.Add(null);
            source3.Add(null);

            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection2);

            source2.Remove("4");
            source1.Remove("5");
            source2.Remove("6");

            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection2);

            source1.Insert(1, "4");
            source3.Insert(0, "5");
            source1.Insert(1, "6");

            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection2);

            source1[1] = "0";
            source1[2] = "0";
            source1[0] = "5";
            source3[1] = "4";

            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection2);

            source1.Clear();

            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection2);

            source3.Clear();
            source2.Clear();

            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection1);
            Assert.Equal(source1.Concat(source2).Concat(source3).Select(transformation), projection2);
        }
    }
}
