using Course.Application.DTOs.ModulesDTO;

namespace Course.Application.Courses.Commands.UpdateModule
{
	public record UpdateModuleCommand(Guid ModuleId, UpdateModuleDto UpdateModuleDto) : ICommand<UpdateModuleResponse>;

	public record UpdateModuleResponse : AbstractApiResponse<string>
	{
		public override string Response { get; set; } = default!;
	}
}
