using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DotNetApp.Extensions;
using Xunit;

namespace DotNetApp.Test
{
    public class NotifyPropertyChangedExtensionTest
    {
        public class Dummy : INotifyPropertyChanged
        {
            public Dummy()
            {
                Collection = new ObservableCollection<Dummy>();
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public string Property
            {
                get => this.GetProperty<string>();
                set => this.SetProperty(value);
            }

            public Dummy Other
            {
                get => this.GetProperty<Dummy>();
                set => this.SetProperty(value);
            }

            public IEnumerable<Dummy> Collection
            {
                get => this.GetProperty<IEnumerable<Dummy>>();
                set => this.SetProperty(value);
            }

            [DependsOn("Other.Property")]
            public string ComputedProperty => Other?.Property;

            [DependsOn("Collection[*].Property")]
            public string LongestProperty => Collection?.Select(d => d.Property).OrderByDescending(p => p.Length).FirstOrDefault();
        }

        [Fact]
        public void DoesForwardPropertyChangedEventsToChainedDependentProperties()
        {
            var dummy = new Dummy();
            var otherDummy = new Dummy();

            int notificationCount = 0;

            dummy.SubscribeToPropertyChanged(nameof(Dummy.ComputedProperty), d => ++notificationCount);
            Assert.True(notificationCount == 0);
            
            dummy.Other = otherDummy;
            Assert.True(notificationCount == 1);

            dummy.Property = "should have no effect";
            Assert.True(notificationCount == 1);

            otherDummy.Property = "should notify";
            Assert.True(notificationCount == 2);

            otherDummy.Other = dummy;
            Assert.True(notificationCount == 2);

            otherDummy.Other = otherDummy;
            Assert.True(notificationCount == 2);

            dummy.Other = null;
            Assert.True(notificationCount == 3);

            otherDummy.Property = "should not notify anymore";
            Assert.True(notificationCount == 3);

            dummy.Other = null;
            Assert.True(notificationCount == 3);
        }

        [Fact]
        public void DoesForwardPropertyChangedEventsToChainedDependentPropertiesWithCollection()
        {
            var dummy = new Dummy();
            var item1 = new Dummy();
            var item2 = new Dummy();
            var item3 = new Dummy();
            var collection = new ObservableCollection<Dummy>();

            int notificationCount = 0;

            dummy.SubscribeToPropertyChanged(nameof(Dummy.LongestProperty), d => ++notificationCount);
            Assert.True(notificationCount == 0);

            dummy.Property = "no effect";
            Assert.True(notificationCount == 0);

            item1.Property = "no effect";
            Assert.True(notificationCount == 0);

            collection.Add(item1);
            Assert.True(notificationCount == 0);

            dummy.Collection = collection;
            Assert.True(notificationCount == 1);

            collection.Remove(item1);
            Assert.True(notificationCount == 2);

            collection.Add(item1);
            Assert.True(notificationCount == 3);

            collection.Add(item2);
            Assert.True(notificationCount == 4);

            collection.Add(item3);
            Assert.True(notificationCount == 5);

            item1.Property = "should notify";
            Assert.True(notificationCount == 6);

            item2.Property = "should notify";
            Assert.True(notificationCount == 7);

            collection.Remove(item3);
            Assert.True(notificationCount == 8);

            item3.Property = "no effect";
            Assert.True(notificationCount == 8);

            dummy.Collection = null;
            Assert.True(notificationCount == 9);

            item1.Property = "no effect";
            Assert.True(notificationCount == 9);

            collection.Add(item3);
            Assert.True(notificationCount == 9);
        }
    }
}
