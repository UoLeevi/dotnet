using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DotNetApp.Extensions;
using Xunit;

namespace DotNetApp.Test
{
    public partial class Dummy : INotifyPropertyChanged
    {
        public Dummy()
        {
            Collection = new ObservableCollection<Dummy>();
            this.InitializeChangeNotifications();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [GenerateProperty(AccessModifierSet = "internal")]
        private string property;

        [GenerateProperty]
        private Dummy other;

        [GenerateProperty]
        private IEnumerable<Dummy> collection;

        [DependsOn("Other.Property")]
        public string ComputedProperty => Other?.Property;

        [DependsOn("ComputedProperty")]
        public int ComputedPropertyLength => ComputedProperty?.Length ?? 0;

        [DependsOn("Collection[*].Property")]
        public string LongestProperty => Collection?.Select(d => d.Property).OrderByDescending(p => p?.Length).FirstOrDefault();
    }

    public class NotifyPropertyChangedExtensionTest
    {

        [Fact]
        public void DoesForwardPropertyChangedEventsToChainedDependentProperties()
        {
            var dummy = new Dummy();
            var otherDummy = new Dummy();

            int notificationCount = 0;

            dummy.Bind(d => d.ComputedProperty, _ => ++notificationCount);
            Assert.True(notificationCount == 1);

            dummy.Other = otherDummy;
            Assert.True(notificationCount == 1);

            otherDummy.Property = "this should notify";
            Assert.True(notificationCount == 2);

            dummy.Property = "should have no effect";
            Assert.True(notificationCount == 2);

            otherDummy.Property = "should notify";
            Assert.True(notificationCount == 3);

            otherDummy.Other = dummy;
            Assert.True(notificationCount == 3);

            otherDummy.Other = otherDummy;
            Assert.True(notificationCount == 3);

            dummy.Other = null;
            Assert.True(notificationCount == 4);

            otherDummy.Property = "should not notify anymore";
            Assert.True(notificationCount == 4);

            dummy.Other = null;
            Assert.True(notificationCount == 4);
        }

        [Fact]
        public void DoesForwardPropertyChangedEventsToChainedDependentPropertiesWithValueType()
        {
            var dummy = new Dummy();
            var otherDummy = new Dummy();

            int notificationCount = 0;

            dummy.Bind(d => d.ComputedPropertyLength, _ => ++notificationCount);
            Assert.True(notificationCount == 1);

            dummy.Other = otherDummy;
            Assert.True(notificationCount == 1);

            otherDummy.Property = "this should notify";
            Assert.True(notificationCount == 2);

            dummy.Property = "should have no effect";
            Assert.True(notificationCount == 2);

            otherDummy.Property = "should notify";
            Assert.True(notificationCount == 3);

            otherDummy.Other = dummy;
            Assert.True(notificationCount == 3);

            otherDummy.Other = otherDummy;
            Assert.True(notificationCount == 3);

            dummy.Other = null;
            Assert.True(notificationCount == 4);

            otherDummy.Property = "should not notify anymore";
            Assert.True(notificationCount == 4);

            dummy.Other = null;
            Assert.True(notificationCount == 4);
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

            dummy.Bind(d => d.LongestProperty, _ => ++notificationCount);
            Assert.True(notificationCount == 1);

            dummy.Property = "no effect";
            Assert.True(notificationCount == 1);

            item1.Property = "no effect";
            Assert.True(notificationCount == 1);

            collection.Add(item1);
            Assert.True(notificationCount == 1);

            dummy.Collection = collection;
            Assert.True(notificationCount == 2);

            collection.Remove(item1);
            Assert.True(notificationCount == 3);

            collection.Add(item1);
            Assert.True(notificationCount == 4);

            collection.Add(item2);
            Assert.True(notificationCount == 4);

            item3.Property = "should notify";
            collection.Add(item3);
            Assert.True(notificationCount == 5);

            item1.Property = "should notify again";
            Assert.True(notificationCount == 6);

            item2.Property = "should notify once only";
            item2.Property = "should notify once only";
            Assert.True(notificationCount == 7);

            collection.Remove(item3);
            Assert.True(notificationCount == 7);

            item3.Property = "no effect";
            Assert.True(notificationCount == 7);

            dummy.Collection = null;
            Assert.True(notificationCount == 8);

            item1.Property = "no effect";
            Assert.True(notificationCount == 8);

            collection.Add(item3);
            Assert.True(notificationCount == 8);
        }
    }
}
