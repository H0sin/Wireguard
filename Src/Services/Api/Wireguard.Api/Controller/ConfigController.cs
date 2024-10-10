using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Wireguard.Api.Filters;

namespace Wireguard.Api.Controller;

[ApiController]
[Route("[controller]")]
[ApiResultFilter]
[ServiceFilter(typeof(ExceptionHandlerFilter))]
public class ConfigController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType(typeof(ApiResult<string>))]
    public async Task<ApiResult<string>> Get()
    {
        string content = await System.IO.File.ReadAllTextAsync("/usr/local/etc/v2ray/config.json");
        return Ok("Hello World!");
    }
}