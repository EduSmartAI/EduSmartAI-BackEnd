using System.Globalization;

namespace BaseService.Common.Utils;

/// <summary>
///  StringUtil
/// </summary>
public static class StringUtil
{
    /// <summary>
    ///  Convert the first character of the string to uppercase
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string? ToLowerCase(string? value)
    {
        if (value.Length <= 0) 
            return string.Empty;

        return char.ToLower(value[0]) + value.Substring(1);
    }
    
    
    #region ConvertToMoney
    
    private const string VndFormat = "#,##0";
    
    /// <summary>
    /// Convert to VND
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ConvertToVnd(decimal value)
    {
        return value.ToString(VndFormat) + " VND";
    }
    
    /// <summary>
    /// Convert to VND
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ConvertToVnd(int value)
    {
        return value.ToString("#,##0") + " VND";
    }
    
    /// <summary>
    /// Convert to VND
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ConvertToVnd(long value)
    {
        return value.ToString("#,##0") + " VND";
    }
    
    /// <summary>
    /// Convert to VND
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ConvertToVnd(double value)
    {
        return value.ToString("#,##0") + " VND";
    }

    /// <summary>
    /// Convert to percent
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ConvertToPercent(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture) + "%";
    }
    
    /// <summary>
    /// Convert to percent
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ConvertToPercent(decimal? value)
    {
        return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) + "%" : "";
    }
    
    /// <summary>
    /// Convert to percent
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ConvertToPercent(int value)
    {
        return value.ToString() + "%";
    }
    
    /// <summary>
    /// Convert to percent
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ConvertToPercent(long value)
    {
        return value.ToString() + "%";
    }
    
    /// <summary>
    /// Convert to percent
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ConvertToPercent(double value)
    {
        return value.ToString(CultureInfo.InvariantCulture) + "%";
    }
    
    /// <summary>
    /// Convert to percent
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ConvertToPercent(float value)
    {
        return value.ToString(CultureInfo.InvariantCulture) + "%";
    }
    #endregion

    #region Date
    
    private const string DateFormat = "dd/MM/yyyy";

    /// <summary>
    /// Convert to DD/MM/YYYY
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string ConvertToDateAsDdMmYyyy(DateOnly date)
    {
        return date.ToString(format: DateFormat);
    }
    
    /// <summary>
    /// Convert to DD/MM/YYYY
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string ConvertToDateAsDdMmYyyy(DateOnly? date)
    {
        return date.HasValue ? date.Value.ToString("dd/MM/yyyy") : string.Empty;
    }
    
    /// <summary>
    /// Convert to DD/MM/YYYY
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string ConvertToDateAsDdMmYyyy(TimeOnly? date)
    {
        return date.HasValue ? date.Value.ToString("dd/MM/yyyy") : string.Empty;
    }
    
    /// <summary>
    /// Convert to DD/MM/YYYY
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string ConvertToDateAsDdMmYyyy(TimeOnly date)
    {
        return date.ToString("dd/MM/yyyy");
    }
    
    /// <summary>
    /// Convert to DD/MM/YYYY
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string ConvertToDateAsDdMmYyyy(DateTime? date)
    {
        return date.HasValue ? date.Value.ToString("dd/MM/yyyy") : string.Empty;
    }
    
    /// <summary>
    /// Convert to MM/DD/YYYY
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string ConvertToDateAsMmDdYyyy(DateOnly date)
    {
        return date.ToString("MM/dd/yyyy");
    }
    
    /// <summary>
    /// Convert to MM/DD/YYYY
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string ConvertToDateAsMmDdYyyy(DateTime date)
    {
        return date.ToString("MM/dd/yyyy");
    }

    #endregion

    #region DateTime

    /// <summary>
    /// Convert to DD/MM/YYYY HH:mm
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string ConvertToHhMm(DateOnly date)
    {
        return date.ToString("HH:mm");
    }
    
    /// <summary>
    /// Convert to DD/MM/YYYY HH:mm
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string ConvertToHhMm(DateOnly? date)
    {
        return date.HasValue ? date.Value.ToString("HH:mm") : string.Empty;
    }
    
    /// <summary>
    /// Convert to DD/MM/YYYY HH:mm
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string ConvertToHhMm(TimeOnly? date)
    {
        return date.HasValue ? date.Value.ToString("HH:mm") : string.Empty;
    }
    
    /// <summary>
    /// Convert to DD/MM/YYYY HH:mm from VietNam timezone
    /// </summary>
    /// <returns></returns>
    public static DateTime ConvertToVietNamTime()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        return TimeZoneInfo.ConvertTimeToUtc(vietnamTime, timeZone);
    }

    #endregion
}