using Xunit;

namespace DotNetApp.Test
{
    public class DynamicTest
    {
        [Fact]
        public void DoesGetPropertyValue()
        {
            var dummy = new Dummy();

            var value = Dynamic.GetPropertyOrFieldValue<Dummy, string>(dummy, nameof(Dummy.Property));
            Assert.True(value == dummy.Property);

            dummy.Property = "asdf";
            value = Dynamic.GetPropertyOrFieldValue<Dummy, string>(dummy, nameof(Dummy.Property));
            Assert.True(value == dummy.Property);
        }

        [Fact]
        public void DoesSubscribeToEvent()
        {
            var dummy = new Dummy();
            int counter = 0;

            var unsubscribe = Dynamic.SubscribeToEvent(dummy, nameof(Dummy.PropertyChanged), () => ++counter);
            Assert.True(counter == 0);

            dummy.Property = "asdf";
            Assert.True(counter == 1);

            dummy.Property = "123";
            Assert.True(counter == 2);

            unsubscribe();
            Assert.True(counter == 2);

            dummy.Property = "qwe";
            Assert.True(counter == 2);
        }
    }
}
