using Triggered.Models;

namespace Triggered.Extensions
{
    /// <summary>
    /// An eqality comparer for <see cref="Setting"/>.
    /// </summary>
    public class SettingComparer : EqualityComparer<Setting>
    {
        /// <summary>
        /// Static instance of a default memberwise comparer.
        /// </summary>
        public static IEqualityComparer<Setting> MemberwiseComparer { get; } = new SettingComparer();

        /// <summary>
        /// Tests for equality between two <see cref="Setting"/>s, on a member-by-member comparison.
        /// </summary>
        /// <param name="leftEventTest">First <see cref="Setting"/> to compare.</param>
        /// <param name="rightEventTest">First <see cref="Setting"/> to compare.</param>
        /// <returns>True if the two <see cref="Setting"/>s are equal, false otherwise.</returns>
        public override bool Equals(Setting? leftSetting, Setting? rightSetting)
        {
            if (leftSetting == null)
                return rightSetting == null;
            else if (rightSetting == null)
                return false;
            else if (ReferenceEquals(leftSetting, rightSetting))
                return true;

            bool isEqual = leftSetting.Key.Equals(rightSetting.Key, StringComparison.InvariantCultureIgnoreCase) &&
                leftSetting.Value.Equals(rightSetting.Value);

            return isEqual;
        }

        /// <summary>
        /// Gets a hash code based on the given <paramref name="setting"/> object. 
        /// </summary>
        /// <param name="setting"></param>
        /// <returns>The generated hash code.</returns>
        /// <exception cref="ArgumentNullException">Returns <see cref="ArgumentNullException"/> if <paramref name="setting"/> is null.</exception>
        public override int GetHashCode(Setting setting)
        {
            if (setting == null)
                throw new ArgumentNullException(nameof(setting));

            return setting.GetHashCode();
        }
    }
}