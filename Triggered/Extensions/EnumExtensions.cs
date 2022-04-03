using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Triggered.Extensions
{
    public static class EnumExtensions
    {
        public static string GetEnumDisplayName(this Enum enumType)
        {
            return enumType.GetType()?.GetMember(enumType.ToString())?.First()?.GetCustomAttribute<DisplayAttribute>()?.Name ?? enumType.ToString();                           
        }
    }
}
