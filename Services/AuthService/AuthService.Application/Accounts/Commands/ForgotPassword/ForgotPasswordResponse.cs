using BaseService.Common.ApiEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Accounts.Commands.ForgotPassword
{
    public record ForgotPasswordResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; }
    }
}
