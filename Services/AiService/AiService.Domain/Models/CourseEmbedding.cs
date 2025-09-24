using Pgvector;

namespace AiService.Domain.Models;

public partial class CourseEmbedding
{
    public long Id { get; set; }

    public string? DocId { get; set; }

    public string Content { get; set; } = null!;

    public string? Metadata { get; set; }

    public Vector Embedding { get; set; } = null!;
}
