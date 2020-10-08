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

            private string property;
            public string Property
            {
                get => property;
                set
                {
                    if (value == property) return;
                    property = value;
                    this.RaisePropertyChanged();
                }
            }

            [DependsOn(nameof(Property))]
            public string ComputedProperty => Property;
        }

        [Fact]
        public void DoesForwardPropertyChangedEventsToDependentProperties()
        {
            var dummy = new Dummy();
            bool didNotify = false;
            dummy.SubscribeToPropertyChanged(nameof(Dummy.ComputedProperty), d => didNotify = true);
            Assert.False(didNotify);
            dummy.Property = "anything but current value";
            Assert.True(didNotify);
        }
    }
}
