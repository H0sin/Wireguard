using Microsoft.AspNetCore.Mvc;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Repositories;
using Wireguard.Api.Filters;

namespace Wireguard.Api.Controller;

[ApiController]
[Route("[controller]")]
[ApiResultFilter]
[ServiceFilter(typeof(ExceptionHandlerFilter))]
public class InterfaceController(IInterfaceRepository interfaceRepository, IIpAddressRepository ipAddressRepository)
    : ControllerBase
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult> Post([FromBody] AddInterfaceDto @interface)
    {
        await ipAddressRepository.ExistIpAddressAsync(@interface.IpAddress);
        bool checkout = await ipAddressRepository.AddIpAddressAsync(@interface.IpAddress);

        if (checkout) await interfaceRepository.InsertAsync(@interface);
        else return BadRequest("ip address already exist");

        return Ok();
    }
}