using TownBulletin.Models;

namespace TownBulletin.Extensions
{
    public class ModuleComparer : EqualityComparer<Module>
    {
        public static IEqualityComparer<Module> MemberwiseComparer { get; } = new ModuleComparer();

        public override bool Equals(Module? leftModule, Module? rightModule)
        {
            if (leftModule == null)
                return rightModule == null;
            else if (rightModule == null)
                return false; 
            else if (ReferenceEquals(leftModule, rightModule)) 
                return true;

            bool isEqual = leftModule.Id.Equals(rightModule.Id) &&
                leftModule.Name.Equals(rightModule.Name) &&
                leftModule.Event.Equals(rightModule.Event) &&
                leftModule.Code.Equals(rightModule.Code) &&
                leftModule.EntryMethod.Equals(rightModule.EntryMethod) &&
                leftModule.ExecutionOrder.Equals(rightModule.ExecutionOrder) &&
                leftModule.StopEventExecution.Equals(rightModule.StopEventExecution) &&
                leftModule.IsEnabled.Equals(rightModule.IsEnabled);

            return isEqual;
        }
        public override int GetHashCode(Module module)
        {
            if (module == null) 
                throw new ArgumentNullException(nameof(module));

            return module.GetHashCode();
        }
    }
}
