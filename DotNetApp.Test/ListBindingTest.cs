using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DotNetApp.Collections;
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

            ListBinding<int, int> binding = new ListBinding<int>(source);

            source.Add(0);
            source.Add(1);
            source.Add(2);
            source.Add(3);
            source.Add(4);

            binding.AddListTarget(projection1);

            source.Add(5);
            source.Add(6);

            binding.AddListTarget(projection2);

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

            ListBinding<int, int> binding = new ListBinding<int>(source1, source2, source3);

            source1.Add(0);
            source2.Add(1);
            source3.Add(2);
            source3.Add(3);
            source2.Add(4);

            binding.AddListTarget(projection1);

            source1.Add(5);
            source2.Add(6);

            binding.AddListTarget(projection2);

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

            Func<int, string> transformation = i => i.ToString();

            ListBinding<int, string> binding = new ListBinding<int, string>(transformation, source1, source2, source3);

            source1.Add(0);
            source2.Add(1);
            source3.Add(2);
            source3.Add(3);
            source2.Add(4);

            binding.AddListTarget(projection1);

            source1.Add(5);
            source2.Add(6);

            binding.AddListTarget(projection2);

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

            Func<int, string> transformation = i => i.ToString();
            Func<int, bool> filterPredicate = i => i <= 2;

            ListBinding<int, string> binding = new ListBinding<int, string>(transformation, filterPredicate, null, source1, source2, source3);

            source1.Add(0);
            source2.Add(1);
            source3.Add(2);
            source3.Add(3);
            source2.Add(4);

            binding.AddListTarget(projection1);

            source1.Add(5);
            source2.Add(6);

            binding.AddListTarget(projection2);

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
    }
}
