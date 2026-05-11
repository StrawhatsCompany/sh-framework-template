using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Weather.GetForecastsByCity;

public sealed class GetForecastsByCityQuery: Request<GetForecastsByCityResponse>
{ 
    public string City { get; set; } = string.Empty;
}