using System.Reflection;

namespace Wireguard.Api.Extensions;

public static class AttributeExtension
{
    public static TAttribute? GetAttribute<TAttribute>(this Enum enumValue)
        where TAttribute : Attribute
    {
        return enumValue.GetType()
            .GetMember(enumValue.ToString())
            .First()
            .GetCustomAttribute<TAttribute>();
    }
}