using Triggered.Models;

namespace Triggered.Extensions
{
    public class UtilityComparer : EqualityComparer<Utility>
    {
        public static IEqualityComparer<Utility> MemberwiseComparer { get; } = new UtilityComparer();

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
                leftModule.Code.Equals(rightModule.Code) &&
                leftModule.IsEnabled.Equals(rightModule.IsEnabled);

            return isEqual;
        }
        public override int GetHashCode(Utility module)
        {
            if (module == null) 
                throw new ArgumentNullException(nameof(module));

            return module.GetHashCode();
        }
    }
}
