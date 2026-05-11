using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Tests.Common;

public class ResultHttpExtensionsTests
{
    [Fact]
    public void Success_returns_200_with_envelope()
    {
        var result = Result.Success(resultCode: ResultCode.Success);

        var http = result.ToHttp();

        var ok = Assert.IsType<Ok<Result>>(http);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    [Fact]
    public void Success_of_T_returns_200_with_envelope()
    {
        var result = Result.Success("payload", resultCode: ResultCode.Success);

        var http = result.ToHttp();

        var ok = Assert.IsType<Ok<Result<string>>>(http);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    [Fact]
    public void Failure_with_errors_returns_400_validation_problem()
    {
        var errors = new Dictionary<string, string[]> { ["City"] = ["required"] };
        var result = Result.Failure(ResultCode.Failure, errors);

        var http = result.ToHttp();

        var problem = Assert.IsType<ProblemHttpResult>(http);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public void Failure_without_errors_returns_400_problem()
    {
        var result = Result.Failure(ResultCode.Failure, new Dictionary<string, string[]>());

        var http = result.ToHttp();

        var problem = Assert.IsType<ProblemHttpResult>(http);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public void Exception_code_returns_500_problem()
    {
        var result = Result.Failure(ResultCode.Exception, new Dictionary<string, string[]>());

        var http = result.ToHttp();

        var problem = Assert.IsType<ProblemHttpResult>(http);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.StatusCode);
    }
}
