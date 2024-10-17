using System.Net;
using Microsoft.AspNetCore.Mvc;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Repositories;
using Wireguard.Api.Filters;

namespace Wireguard.Api.Controller;

/// <summary>
/// Controller for managing IP addresses in the Wireguard API.
/// </summary>
[ApiController]
[Route("[controller]")]
[ApiResultFilter]
[ServiceFilter(typeof(ExceptionHandlerFilter))]
public class IpAddressController(IIpAddressRepository ipAddressRepository, IInterfaceRepository interfaceRepository)
    : ControllerBase
{
    /// <summary>
    /// Retrieves a list of IP addresses for a specific interface by name.
    /// </summary>
    /// <param name="interfaceName">The name of the interface.</param>
    /// <returns>An ApiResult containing a list of IP addresses.</returns>
    /// <response code="200">Returns the list of IP addresses.</response>
    /// <response code="204">No IP addresses found for the specified interface.</response>
    /// <response code="400">Bad request if the interface name is invalid.</response>
    /// <response code="404">Interface not found by the specified name.</response>
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