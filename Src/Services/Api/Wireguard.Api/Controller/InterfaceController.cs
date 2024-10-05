using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Enums;
using Wireguard.Api.Data.Repositories;
using Wireguard.Api.Filters;

namespace Wireguard.Api.Controller;

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
        bool response = await interfaceRepository.InsertAsync(@interface);
        if (!response) throw new ApplicationException("failed to add interface");
        return Ok();
    }

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

    [HttpDelete("{name}")]
    public async Task<ApiResult> Delete(string name)
    {
        bool response = await interfaceRepository.DeleteAsync(name);
        if (!response) throw new ApplicationException("failed to delete interface");
        return Ok();
    }
}