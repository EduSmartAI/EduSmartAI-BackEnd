using AuthService.Application.Accounts.Commands.Inserts;
using BaseService.API.BaseControllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace AuthService.API.Controllers.Accounts;

/// <summary>
/// InsertStudentController - Insert student account
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class InsertStudentController : ControllerBase, IApiAsyncController<StudentInsertCommand, StudentInsertResponse>
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IMediator _mediator;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator"></param>
    public InsertStudentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Incoming Post
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<StudentInsertResponse> ProcessRequest(StudentInsertCommand request)
    {
        return await ApiControllerHelperNotAuth.HandleRequest<StudentInsertCommand, StudentInsertResponse, string>(
            request,
            _logger,
            new StudentInsertResponse(),
            ModelState,
            async () => await _mediator.Send(request)
        );
    }
}