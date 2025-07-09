using System.ComponentModel;
using System.Reflection;

namespace Longhl104.PawfectMatch.Extensions;

/// <summary>
/// Extension methods for enums
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets the AmbientValue attribute value for an enum value
    /// </summary>
    /// <typeparam name="T">The type to return</typeparam>
    /// <param name="enumVal">The enum value</param>
    /// <returns>The ambient value or default if not found</returns>
    public static T GetAmbientValue<T>(this Enum enumVal)
    {
        Type type = enumVal.GetType();
        MemberInfo[] memInfo = type.GetMember(enumVal.ToString());
        object[] attributes = memInfo[0].GetCustomAttributes(typeof(AmbientValueAttribute), false);

        if (attributes == null || attributes.Length == 0)
            return default!;

        return (T)((AmbientValueAttribute)attributes[0]).Value!;
    }
}
