using Microsoft.AspNetCore.Mvc;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this PaginatedResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(new
            {
                result.Items,
                result.PageNumber,
                result.PageSize,
                result.TotalCount,
                result.TotalPages,
                result.HasPreviousPage,
                result.HasNextPage
            });
        }

        return MapError(result.Error!);
    }

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return MapError(result.Error!);
    }

    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        return MapError(result.Error!);
    }

    private static IResult MapError(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        if (error.Type == ErrorType.Validation && error.ValidationErrors is not null)
        {
            return Results.ValidationProblem(error.ValidationErrors);
        }

        var problem = new ProblemDetails
        {
            Title = error.Message,
            Status = status,
            Type = $"https://httpstatuses.com/{status}"
        };

        problem.Extensions["errorCode"] = error.ErrorCode.ToString();

        return Results.Problem(problem);
    }
}