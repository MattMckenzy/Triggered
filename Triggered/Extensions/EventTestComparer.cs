using Triggered.Models;

namespace Triggered.Extensions
{
    public class EventTestComparer : EqualityComparer<EventTest>
    {
        public static IEqualityComparer<EventTest> MemberwiseComparer { get; } = new EventTestComparer();

        public override bool Equals(EventTest? leftEventTest, EventTest? rightEventTest)
        {
            if (leftEventTest == null)
                return rightEventTest == null;
            else if (rightEventTest == null)
                return false;
            else if (ReferenceEquals(leftEventTest, rightEventTest))
                return true;

            bool isEqual = leftEventTest.Id.Equals(rightEventTest.Id) &&
                leftEventTest.Name.Equals(rightEventTest.Name) &&
                leftEventTest.Event.Equals(rightEventTest.Event) &&
                (leftEventTest.JsonData?.Equals(rightEventTest.JsonData) ?? rightEventTest.JsonData == null);

            return isEqual;
        }
        public override int GetHashCode(EventTest eventTest)
        {
            if (eventTest == null)
                throw new ArgumentNullException(nameof(eventTest));

            return eventTest.GetHashCode();
        }
    }
}