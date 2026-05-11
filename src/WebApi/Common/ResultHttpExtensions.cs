using Microsoft.AspNetCore.Http;
using SH.Framework.Library.Cqrs.Implementation;

namespace WebApi.Common;

public static class ResultHttpExtensions
{
    public static IResult ToHttp(this Result result) =>
        result.IsSuccess
            ? Results.Ok(result)
            : Failure(result);

    public static IResult ToHttp<T>(this Result<T> result) =>
        result.IsSuccess
            ? Results.Ok(result)
            : Failure(result);

    private static IResult Failure(Result result)
    {
        if (result.Errors is { Count: > 0 })
        {
            return Results.ValidationProblem(
                errors: result.Errors,
                detail: result.Description,
                statusCode: StatusCodes.Status400BadRequest);
        }

        var status = result.Code == ResultCode.Exception.Code
            ? StatusCodes.Status500InternalServerError
            : StatusCodes.Status400BadRequest;

        return Results.Problem(
            detail: result.Description,
            statusCode: status,
            title: result.CategorizedCode);
    }
}
