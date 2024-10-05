using Microsoft.AspNetCore.Mvc;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Repositories;
using Wireguard.Api.Filters;

namespace Wireguard.Api.Controller;

[ApiController]
[Route("api/[controller]")]
[ApiResultFilter]
[ServiceFilter(typeof(ExceptionHandlerFilter))]
public class InterfaceController(IInterfaceRepository interfaceRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<List<Interface>>), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult<List<Interface>>> Get()
    {
        return Ok(await interfaceRepository.GetAllAsync());
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult> Post([FromBody] Interface @interface)
    {
        await interfaceRepository.InsertAsync(@interface);
        return Ok();
    }
}