using AiService.Domain.Models;
using BaseService.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiService.API.Controller;

[Route("api/[controller]")]
[ApiController]
public class TestCodeController : ControllerBase
{
    private readonly ICommandRepository<MajorEmbedding> _majorEmbeddingRepository;

    public TestCodeController(ICommandRepository<MajorEmbedding> majorEmbeddingRepository)
    {
        _majorEmbeddingRepository = majorEmbeddingRepository;
    }

    // csharp
    [HttpGet("test-major")]
    public async Task<IActionResult> TestMajorEmbedding()
    {
        var items = await _majorEmbeddingRepository.Find().ToListAsync();
        return Ok(items);
    }
}