using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Enums;
using Wireguard.Api.Data.Repositories;
using Wireguard.Api.Filters;

namespace Wireguard.Api.Controller;

/// <summary>
/// Controller for managing interfaces in the Wireguard API.
/// </summary>
[ApiController]
[Route("[controller]")]
[ApiResultFilter]
[ServiceFilter(typeof(ExceptionHandlerFilter))]
public class InterfaceController(
    IInterfaceRepository interfaceRepository,
    IIpAddressRepository ipAddressRepository,
    IConfiguration configuration)
    : ControllerBase
{
    /// <summary>
    /// Retrieves the details of a specific interface by name.
    /// </summary>
    /// <param name="name">The name of the interface.</param>
    /// <returns>An ApiResult containing the InterfaceDetailDto object.</returns>
    [HttpGet("{name}")]
    [ProducesResponseType(typeof(ApiResult<InterfaceDetailDto>), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult<InterfaceDetailDto>> Get(string name)
    {
        if (await interfaceRepository.GetInterfaceByNameAsync(name) is null)
            throw new Exception($"Interface with name {name} not found");
        
        return Ok(await interfaceRepository.GetAsync(name));
    }

    /// <summary>
    /// Retrieves a list of all interfaces.
    /// </summary>
    /// <returns>An ApiResult containing a list of Interface objects.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<List<Interface>>), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult<List<Interface>>> Get()
    {
        return Ok(await interfaceRepository.GetAllAsync());
    }

    /// <summary>
    /// Adds a new interface.
    /// </summary>
    /// <param name="interface">The interface data to add.</param>
    /// <returns>An ApiResult indicating the success of the operation.</returns>
    /// <exception cref="ApplicationException">Thrown when the interface could not be added.</exception>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult> Post([FromBody] AddInterfaceDto @interface)
    {
        bool response = await interfaceRepository.InsertAsync(@interface);
        if (!response) throw new ApplicationException("failed to add interface");
        return Ok();
    }

    /// <summary>
    /// Changes the status of a specific interface.
    /// </summary>
    /// <param name="name">The name of the interface.</param>
    /// <param name="status">The new status of the interface.</param>
    /// <returns>An ApiResult indicating the success of the operation.</returns>
    /// <exception cref="ApplicationException">Thrown when the status could not be changed.</exception>
    [HttpPut(Name = "ChangeStatus")]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult> Put(string name, InterfaceStatus status)
    {
        bool response = await interfaceRepository.ChangeStatusInterfaceAsync(name, status);
        if (!response) throw new ApplicationException("failed to change status interface");
        return Ok();
    }

    /// <summary>
    /// Deletes a specific interface by name.
    /// </summary>
    /// <param name="name">The name of the interface to delete.</param>
    /// <returns>An ApiResult indicating the success of the operation.</returns>
    /// <exception cref="ApplicationException">Thrown when the interface could not be deleted.</exception>
    [HttpDelete("{name}")]
    public async Task<ApiResult> Delete(string name)
    {
        bool response = await interfaceRepository.DeleteAsync(name);
        if (!response) throw new ApplicationException("failed to delete interface");
        return Ok();
    }
}