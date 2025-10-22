namespace StudentService.Infrastructure.Helpers;

/// <summary>
/// Helper class để xác định level hierarchy của khóa học
/// Beginner (1) → Intermediate (2) → Advanced (3)
/// </summary>
public static class CourseLevelHelper
{
    private const short BeginnerLevel = 1;
    private const short IntermediateLevel = 2;
    private const short AdvancedLevel = 3;

    /// <summary>
    /// Get level slower than the current level
    /// </summary>
    /// <param name="currentLevel">Level hiện tại</param>
    /// <returns>Level thấp hơn hoặc null nếu không có</returns>
    public static short? GetLowerLevel(short currentLevel)
    {
        return currentLevel switch
        {
            AdvancedLevel => IntermediateLevel,      // Advanced → Intermediate
            IntermediateLevel => BeginnerLevel,      // Intermediate → Beginner
            BeginnerLevel => null,                   // Beginner → Không có level thấp hơn
            _ => null
        };
    }

    /// <summary>
    /// Get level higher than the current level
    /// </summary>
    /// <param name="currentLevel">Level hiện tại</param>
    /// <returns>Level cao hơn hoặc null nếu không có</returns>
    public static short? GetHigherLevel(short currentLevel)
    {
        return currentLevel switch
        {
            BeginnerLevel => IntermediateLevel,      // Beginner → Intermediate
            IntermediateLevel => AdvancedLevel,      // Intermediate → Advanced
            AdvancedLevel => null,                   // Advanced → Không có level cao hơn
            _ => null
        };
    }

    /// <summary>
    /// Kiểm tra xem level hiện tại có thể gợi ý level thấp hơn không
    /// </summary>
    /// <param name="currentLevel">Level hiện tại</param>
    /// <returns>True nếu có thể gợi ý level thấp hơn</returns>
    public static bool CanSuggestLowerLevel(short currentLevel)
    {
        return GetLowerLevel(currentLevel).HasValue;
    }

    /// <summary>
    /// Kiểm tra xem level hiện tại có thể gợi ý level cao hơn không
    /// </summary>
    /// <param name="currentLevel">Level hiện tại</param>
    /// <returns>True nếu có thể gợi ý level cao hơn</returns>
    public static bool CanSuggestHigherLevel(short currentLevel)
    {
        return GetHigherLevel(currentLevel).HasValue;
    }

    /// <summary>
    /// Lấy tên level
    /// </summary>
    /// <param name="level">Level</param>
    /// <returns>Tên level</returns>
    public static string GetLevelName(short level)
    {
        return level switch
        {
            BeginnerLevel => "Beginner",
            IntermediateLevel => "Intermediate",
            AdvancedLevel => "Advanced",
            _ => "Unknown"
        };
    }
}

