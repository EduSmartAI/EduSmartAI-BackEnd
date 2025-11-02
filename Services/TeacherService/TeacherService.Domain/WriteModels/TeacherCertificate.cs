using System;
using System.Collections.Generic;

namespace TeacherService.Domain.WriteModels;

public partial class TeacherCertificate
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

    public virtual Teacher Teacher { get; set; } = null!;
}
