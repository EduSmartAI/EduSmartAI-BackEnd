

namespace Course.Domain.Models;

public partial class CoreSubject
{
    public int CoreSubjectId { get; set; }
    
    public string SubjectCode { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; }

    public string UpdatedBy { get; set; }

    public bool IsActive { get; set; }

    public virtual Subject Subject { get; set; }
}