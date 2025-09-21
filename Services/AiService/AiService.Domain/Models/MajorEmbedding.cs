using Pgvector;

namespace AiService.Domain.Models;

public partial class MajorEmbedding
{
    public string MajorCode { get; set; } = null!;

    public string MajorName { get; set; } = null!;

    public string Content { get; set; } = null!;

    public Vector Embedding { get; set; } = null!;
}
