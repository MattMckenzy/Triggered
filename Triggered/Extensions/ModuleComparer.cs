using Triggered.Models;

namespace Triggered.Extensions
{
    /// <summary>
    /// An eqality comparer for <see cref="Module"/>.
    /// </summary>
    public class ModuleComparer : EqualityComparer<Module>
    {
        /// <summary>
        /// Static instance of a default memberwise comparer.
        /// </summary>
        public static IEqualityComparer<Module> MemberwiseComparer { get; } = new ModuleComparer();

        /// <summary>
        /// Tests for equality between two <see cref="Module"/>s, on a member-by-member comparison.
        /// </summary>
        /// <param name="leftModule">First <see cref="Module"/> to compare.</param>
        /// <param name="rightModule">First <see cref="Module"/> to compare.</param>
        /// <returns>True if the two <see cref="Module"/>s are equal, false otherwise.</returns>
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

        /// <summary>
        /// Gets a hash code based on the given <paramref name="module"/> object. 
        /// </summary>
        /// <param name="module"></param>
        /// <returns>The generated hash code.</returns>
        /// <exception cref="ArgumentNullException">Returns <see cref="ArgumentNullException"/> if <paramref name="module"/> is null.</exception>
        public override int GetHashCode(Module module)
        {
            if (module == null) 
                throw new ArgumentNullException(nameof(module));

            return module.GetHashCode();
        }
    }
}
