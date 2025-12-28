using BuildingBlocks.CQRS;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Accounts.Commands.ForgotPassword;
public class ForgotPasswordCommand : ICommand<ForgotPasswordResponse>
{
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; init; } = null!;
}

