using System.Collections.Generic;
using System.Linq;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Providers;

public class ProviderResult
{
    public int Code { get; set; }
    public string? Description { get; set; }
    private readonly Dictionary<string, string[]> _errors = [];
    public IReadOnlyDictionary<string, string[]> Errors => _errors;
    public ProviderPayload? RequestPayload { get; set; }
    public ProviderPayload? ResponsePayload { get; set; }
    public bool IsSuccess => Code == ResultCode.Success.Code;

    protected ProviderResult() { }

    public static ProviderResult Success(ResultCode? resultCode = null) => new()
    {
        Code = resultCode?.Code ?? ResultCode.Success.Code,
        Description = resultCode?.Description ?? ResultCode.Success.Description,
    };

    public static ProviderResult<TResponse> Success<TResponse>(TResponse result, ResultCode? resultCode = null) => new()
    {
        Code = resultCode?.Code ?? ResultCode.Success.Code,
        Description = resultCode?.Description ?? ResultCode.Success.Description,
        Response = result
    };

    public static ProviderResult Failure(ResultCode? resultCode = null) => new()
    {
        Code = resultCode?.Code ?? ResultCode.Failure.Code,
        Description = resultCode?.Description ?? ResultCode.Failure.Description,
    };

    public static ProviderResult<TResponse> Failure<TResponse>(ResultCode? resultCode = null, TResponse? response = default) => new()
    {
        Code = resultCode?.Code ?? ResultCode.Failure.Code,
        Description = resultCode?.Description ?? ResultCode.Failure.Description,
        Response = response
    };

    public virtual ProviderResult WithRequestJson(string payload, string type = "JSON")
    {
        RequestPayload = new ProviderPayload(payload, type);
        return this;
    }

    public virtual ProviderResult WithResponseJson(string payload, string type = "JSON")
    {
        ResponsePayload = new ProviderPayload(payload, type);
        return this;
    }

    public virtual ProviderResult WithError(string key, string value)
    {
        if (_errors.TryGetValue(key, out var currentErrors))
        {
            if (Array.IndexOf(currentErrors, value) == -1) 
            {
                _errors[key] = [.. currentErrors, value];
            }
        }
        else
        {
            _errors[key] = [value];
        }

        return this;
    }
}

public class ProviderResult<TResponse> : ProviderResult
{
    public TResponse? Response { get; set; }

    public override ProviderResult<TResponse> WithRequestJson(string payload, string type = "JSON")
    {
        base.WithRequestJson(payload, type);
        return this;
    }

    public override ProviderResult<TResponse> WithResponseJson(string payload, string type = "JSON")
    {
        base.WithResponseJson(payload, type);
        return this;
    }

    public override ProviderResult<TResponse> WithError(string key, string value)
    {
        base.WithError(key, value);
        return this;
    }
}
