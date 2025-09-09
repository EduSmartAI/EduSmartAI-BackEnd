using BaseService.API.BaseControllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NLog;
using StudentService.Application.Interfaces;
using StudentService.Application.Majors.Queries;
using Swashbuckle.AspNetCore.Annotations;

namespace StudentService.API.Controllers;

/// <summary>
/// SelectMajorsController - Select Majors
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class SelectMajorsController : ControllerBase, IApiAsyncController<MajorsSelectQuery, MajorsSelectResponse>
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IMediator _mediator;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator"></param>
    public SelectMajorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Incoming Get
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Lấy danh sách chuyên ngành",
        Description = "Trả về danh sách tất cả chuyên ngành đang hoạt động. Ai cũng có thể xem"
    )]
    public async Task<MajorsSelectResponse> ProcessRequest([FromQuery] MajorsSelectQuery request)
    {
        return await ApiControllerHelperNotAuth.HandleRequest<MajorsSelectQuery, MajorsSelectResponse, List<MajorsSelectResponseEntity>>(
            request,
                _logger,
                new MajorsSelectResponse(),
            ModelState,
            async () => await _mediator.Send(request)
            );
    }
}