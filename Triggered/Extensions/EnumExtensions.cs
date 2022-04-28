using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Triggered.Extensions
{
    /// <summary>
    /// An enumeration extension to help retrieve one's display name.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Retrieve the given <paramref name="enumType"/>'s display name, or it's normal name if it doesn't have a <see cref="DisplayAttribute"/>.
        /// </summary>
        /// <param name="enumType">From extension, the <see cref="Enum"/>.</param>
        /// <returns>The enumeration's display name, or it's normal name if it doesn't have a <see cref="DisplayAttribute"/>.</returns>
        public static string GetEnumDisplayName(this Enum enumType)
        {
            return enumType.GetType()?.GetMember(enumType.ToString())?.First()?.GetCustomAttribute<DisplayAttribute>()?.Name ?? enumType.ToString();                           
        }
    }
}
