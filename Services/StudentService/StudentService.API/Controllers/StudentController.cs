using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using StudentService.Application.Applications.UserBehaviours.Commands;

namespace StudentService.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class StudentController(ISender sender) : ControllerBase
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    
}