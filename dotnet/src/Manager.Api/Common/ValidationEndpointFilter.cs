using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Manager.Api.Common;

public sealed class ValidationEndpointFilter<TCommand> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var command = context.Arguments.OfType<TCommand>().FirstOrDefault();
        if (command is null)
            return await next(context);

        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<TCommand>>();
        var validationResult = await validator.ValidateAsync(command, context.HttpContext.RequestAborted);

        if (validationResult.IsValid)
            return await next(context);

        var description = string.Join(
            "; ",
            validationResult.Errors.Select(e => e.ErrorMessage));

        return CustomResults.Problem(Error.Validation("Validation.Error", description));
    }
}
