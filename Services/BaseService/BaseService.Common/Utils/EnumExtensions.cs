using System.ComponentModel;
using System.Reflection;

namespace BaseService.Common.Utils;

public static class EnumExtensions
{
    /// <summary>
    /// Get Description attribute value from enum
    /// If Description attribute is not found, returns enum name
    /// </summary>
    /// <param name="value">Enum value</param>
    /// <returns>Description string or enum name</returns>
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        
        if (field == null)
            return value.ToString();
            
        var attribute = field.GetCustomAttribute<DescriptionAttribute>();
        
        return attribute?.Description ?? value.ToString();
    }
}

