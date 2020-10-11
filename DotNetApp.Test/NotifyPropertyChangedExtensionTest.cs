using System.ComponentModel;
using DotNetApp.Extensions;
using Xunit;

namespace DotNetApp.Test
{
    public class NotifyPropertyChangedExtensionTest
    {
        public class Dummy : INotifyPropertyChanged
        {
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

            [DependsOn(nameof(Property))]
            public string ComputedProperty => Property;

            [DependsOn("Other.Property")]
            public string OtherComputedProperty => Other?.Property;
        }

        [Fact]
        public void DoesForwardPropertyChangedEventsToChainedDependentProperties()
        {
            var dummy = new Dummy();
            var otherDummy = new Dummy();

            int notificationCount = 0;

            dummy.SubscribeToPropertyChanged(nameof(Dummy.OtherComputedProperty), d => ++notificationCount);
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
    }
}
