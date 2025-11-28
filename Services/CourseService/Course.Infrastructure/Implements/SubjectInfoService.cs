using BuildingBlocks.Messaging.Events.AIService.SubjectInfoEvent;
using Microsoft.Extensions.Logging;

namespace Course.Infrastructure.Implements;

public class SubjectInfoService(
    ICommandRepository<VMajorSemesterSubjectPrereqs> repository,
    ILogger<SubjectInfoService> logger) : ISubjectInfoService
{
    public async Task<IReadOnlyList<SubjectInfoItem>> GetByMajorAsync(string majorCode, CancellationToken cancellationToken)
    {
        var normalizedMajor = NormalizeCode(majorCode);
        if (string.IsNullOrWhiteSpace(normalizedMajor))
        {
            return Array.Empty<SubjectInfoItem>();
        }

        try
        {
            var records = await repository
                .Find(x => x.MajorCode == normalizedMajor, isTracking: false, cancellationToken)
                .ToListAsync(cancellationToken);

            return Map(records);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load subject info for major {MajorCode}", normalizedMajor);
            throw;
        }
    }

    public async Task<IReadOnlyList<SubjectInfoItem>> GetBySubjectCodesAsync(
        IReadOnlyCollection<string> subjectCodes,
        CancellationToken cancellationToken)
    {
        if (subjectCodes == null || subjectCodes.Count == 0)
        {
            return Array.Empty<SubjectInfoItem>();
        }

        var normalizedCodes = subjectCodes
            .Select(NormalizeCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedCodes.Count == 0)
        {
            return Array.Empty<SubjectInfoItem>();
        }

        try
        {
            var records = await repository
                .Find(
                    x => x.SubjectCode != null && normalizedCodes.Contains(x.SubjectCode.ToUpper()),
                    isTracking: false,
                    cancellationToken)
                .ToListAsync(cancellationToken);

            var mapped = Map(records);
            var normalizedSet = normalizedCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return mapped.Where(item => normalizedSet.Contains(item.SubjectCode)).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load subject info for subject codes");
            throw;
        }
    }

    private static IReadOnlyList<SubjectInfoItem> Map(IEnumerable<VMajorSemesterSubjectPrereqs> records)
    {
        return records
            .Where(record => record is not null)
            .Select(record =>
            {
                var subjectCode = NormalizeCode(record.SubjectCode);
                if (string.IsNullOrWhiteSpace(subjectCode))
                {
                    return null;
                }

                var prereqs = (record.PrereqSubjectCodes ?? new List<string>())
                    .Select(NormalizeCode)
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new SubjectInfoItem
                {
                    MajorCode = NormalizeCode(record.MajorCode),
                    SubjectCode = subjectCode,
                    SubjectName = string.IsNullOrWhiteSpace(record.SubjectName)
                        ? subjectCode
                        : record.SubjectName.Trim(),
                    SemesterIndex = record.SemesterIndex,
                    SubjectIndex = record.SubjectIndex,
                    PrereqSubjectCodes = prereqs
                };
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .GroupBy(item => item.SubjectCode, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static string NormalizeCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToUpperInvariant();
}

