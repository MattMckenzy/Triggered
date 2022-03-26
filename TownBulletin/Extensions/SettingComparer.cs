using TownBulletin.Models;

namespace TownBulletin.Extensions
{
    public class SettingComparer : EqualityComparer<Setting>
    {
        public static IEqualityComparer<Setting> MemberwiseComparer { get; } = new SettingComparer();

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
        public override int GetHashCode(Setting setting)
        {
            if (setting == null)
                throw new ArgumentNullException(nameof(setting));

            return setting.GetHashCode();
        }
    }
}