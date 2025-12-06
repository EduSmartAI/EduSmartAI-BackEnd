using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Course.Application.Syllabus.Commands.UpdateSyllabusSubjects
{
	public class UpdateSyllabusSubjectsHandler(ISyllabusService _syllabusService) : ICommandHandler<UpdateSyllabusSubjectsCommand, UpdateSyllabusSubjectsResponse>
	{
		public async Task<UpdateSyllabusSubjectsResponse> Handle(UpdateSyllabusSubjectsCommand request, CancellationToken cancellationToken)
		{
			return await _syllabusService.UpdateSyllabusSubjectsAsync(request, cancellationToken);
		}
	}
}
