using BuildingBlocks.CQRS;
using FluentValidation;
using MediatR;

namespace BuildingBlocks.Behaviors
{
	public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
		where TRequest : ICommand<TResponse>
		where TResponse : notnull
	{
		public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
		{
			var context = new ValidationContext<TRequest>(request);
			var validationResults = await Task.WhenAll(
				validators.Select(v => v.ValidateAsync(context, cancellationToken)));

			var failures = validationResults
							.Where(x => x.Errors.Any())
							.SelectMany(x => x.Errors)
							.Where(f => f != null)
							.ToList();

			if (failures.Count > 0)
			{
				throw new ValidationException(failures);
			}

			return await next();
		}
	}
}
