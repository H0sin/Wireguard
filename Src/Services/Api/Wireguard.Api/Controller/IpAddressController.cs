using System.Net;
using Microsoft.AspNetCore.Mvc;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Repositories;
using Wireguard.Api.Filters;

namespace Wireguard.Api.Controller;

[ApiController]
[Route("[controller]")]
[ApiResultFilter]
[ServiceFilter(typeof(ExceptionHandlerFilter))]
public class IpAddressController(IIpAddressRepository ipAddressRepository, IInterfaceRepository interfaceRepository)
    : ControllerBase
{
    [HttpGet("{interfaceName}")]
    [ProducesResponseType(typeof(ApiResult<List<IpAddress>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<List<IpAddress>>), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult<List<IpAddress>>> Get(string interfaceName)
    {
        var @interface = await interfaceRepository.GetInterfaceByNameAsync(interfaceName);

        if (@interface == null) return NotFound($"Interface not found by name {interfaceName}");

        return await ipAddressRepository
            .GetIpAddressByInterfaceIdAsync(@interface.Id);
    }
}