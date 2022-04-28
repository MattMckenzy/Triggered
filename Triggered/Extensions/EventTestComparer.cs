using Triggered.Models;

namespace Triggered.Extensions
{
    /// <summary>
    /// An eqality comparer for <see cref="EventTest"/>.
    /// </summary>
    public class EventTestComparer : EqualityComparer<EventTest>
    {
        /// <summary>
        /// Static instance of a default memberwise comparer.
        /// </summary>
        public static IEqualityComparer <EventTest> MemberwiseComparer { get; } = new EventTestComparer();

        /// <summary>
        /// Tests for equality between two <see cref="EventTest"/>s, on a member-by-member comparison.
        /// </summary>
        /// <param name="leftEventTest">First <see cref="EventTest"/> to compare.</param>
        /// <param name="rightEventTest">First <see cref="EventTest"/> to compare.</param>
        /// <returns>True if the two <see cref="EventTest"/>s are equal, false otherwise.</returns>
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

        /// <summary>
        /// Gets a hash code based on the given <paramref name="eventTest"/> object. 
        /// </summary>
        /// <param name="eventTest"></param>
        /// <returns>The generated hash code.</returns>
        /// <exception cref="ArgumentNullException">Returns <see cref="ArgumentNullException"/> if <paramref name="eventTest"/> is null.</exception>
        public override int GetHashCode(EventTest eventTest)
        {
            if (eventTest == null)
                throw new ArgumentNullException(nameof(eventTest));

            return eventTest.GetHashCode();
        }
    }
}