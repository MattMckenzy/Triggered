using Triggered.Models;

namespace Triggered.Extensions
{
    /// <summary>
    /// An eqality comparer for <see cref="Utility"/>.
    /// </summary>
    public class UtilityComparer : EqualityComparer<Utility>
    {
        /// <summary>
        /// Static instance of a default memberwise comparer.
        /// </summary>
        public static IEqualityComparer<Utility> MemberwiseComparer { get; } = new UtilityComparer();

        /// <summary>
        /// Tests for equality between two <see cref="Utility"/>s, on a member-by-member comparison.
        /// </summary>
        /// <param name="leftEventTest">First <see cref="Utility"/> to compare.</param>
        /// <param name="rightEventTest">First <see cref="Utility"/> to compare.</param>
        /// <returns>True if the two <see cref="Utility"/>s are equal, false otherwise.</returns>
        public override bool Equals(Utility? leftModule, Utility? rightModule)
        {
            if (leftModule == null)
                return rightModule == null;
            else if (rightModule == null)
                return false; 
            else if (ReferenceEquals(leftModule, rightModule)) 
                return true;

            bool isEqual = leftModule.Id.Equals(rightModule.Id) &&
                leftModule.Name.Equals(rightModule.Name) &&
                leftModule.Code.Equals(rightModule.Code);

            return isEqual;
        }

        /// <summary>
        /// Gets a hash code based on the given <paramref name="utility"/> object. 
        /// </summary>
        /// <param name="utility"></param>
        /// <returns>The generated hash code.</returns>
        /// <exception cref="ArgumentNullException">Returns <see cref="ArgumentNullException"/> if <paramref name="utility"/> is null.</exception>
        public override int GetHashCode(Utility utility)
        {
            if (utility == null) 
                throw new ArgumentNullException(nameof(utility));

            return utility.GetHashCode();
        }
    }
}
