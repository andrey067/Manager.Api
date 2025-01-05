using ErrorOr;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Api.Extensions;

public abstract class BaseApi
{
    protected static IResult Problem(List<Error> errors)
    {
        if (errors.Count is 0)
            return Results.Problem();

        return errors.All(error => error.Type == ErrorType.Validation) ? ValidationProblem(errors) : Problem(errors[0]);
    }

    private static IResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(statusCode: statusCode, title: error.Description);
    }

    private static IResult ValidationProblem(List<Error> errors)
    {
        var modelStateDictionary = new ModelStateDictionary();

        foreach (var error in errors)
            modelStateDictionary.AddModelError(
                error.Code,
                error.Description);

        return Results.ValidationProblem(modelStateDictionary.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()));
    }
}