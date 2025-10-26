using TeacherService.Domain.WriteModels;

namespace TeacherService.Domain.ReadModels;

public class TeacherCertificateCollection
{
    public Guid CertificateId { get; set; }

    public Guid TeacherId { get; set; }

    public string CertName { get; set; } = null!;

    public string? Issuer { get; set; }

    public DateOnly? IssuedDate { get; set; }

    public DateOnly? ExpireDate { get; set; }

    public string? CertUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsActive { get; set; }
    
    public static TeacherCertificateCollection FromWriteModel(TeacherCertificate certificate)
    {
        var collection = new TeacherCertificateCollection
        {
            CertificateId = certificate.CertificateId,
            TeacherId = certificate.TeacherId,
            CertName = certificate.CertName,
            Issuer = certificate.Issuer,
            IssuedDate = certificate.IssuedDate,
            ExpireDate = certificate.ExpireDate,
            CertUrl = certificate.CertUrl,
            CreatedAt = certificate.CreatedAt,
            UpdatedAt = certificate.UpdatedAt,
            CreatedBy = certificate.CreatedBy,
            UpdatedBy = certificate.UpdatedBy,
            IsActive = certificate.IsActive
        };
        return collection;
    }
}