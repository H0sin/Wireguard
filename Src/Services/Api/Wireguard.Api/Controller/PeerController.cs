using Microsoft.AspNetCore.Mvc;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Repositories;
using Wireguard.Api.Filters;

namespace Wireguard.Api.Controller;

/// <summary>
/// Controller for managing peers in the Wireguard API.
/// </summary>
[ApiController]
[Route("[controller]")]
[ApiResultFilter]
[ServiceFilter(typeof(ExceptionHandlerFilter))]
public class PeerController(IPeerRepository peerRepository) : ControllerBase
{
    /// <summary>
    /// Adds a new peer to the specified interface.
    /// </summary>
    /// <param name="interfacename">The name of the interface.</param>
    /// <param name="peer">The peer data to add.</param>
    /// <returns>An ApiResult indicating the success of the operation.</returns>
    /// <exception cref="ApplicationException">Thrown when the peer could not be added.</exception>
    [HttpPost("{interfacename:maxlength(50)}")]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult> Post(string? interfacename, [FromBody] AddPeerDto peer)
    {
        bool response = await peerRepository.InsertAsync(peer, interfacename);
        if (!response) throw new ApplicationException("failed to add peer");
        return Ok();
    }

    /// <summary>
    /// Retrieves a list of peers based on the specified filter criteria.
    /// </summary>
    /// <param name="filterPeer">The filter criteria for retrieving peers.</param>
    /// <returns>An ApiResult containing the filtered list of peers.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<FilterPeerDto>), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult<FilterPeerDto>> Get([FromQuery] FilterPeerDto filterPeer)
    {
        var peers = await peerRepository.FilterPeerAsync(filterPeer, new CancellationToken(default));
        return Ok(peers);
    }

    /// <summary>
    /// Retrieves the configuration content for a specific peer by name.
    /// </summary>
    /// <param name="name">The name of the peer.</param>
    /// <returns>An ApiResult containing the configuration content as a string.</returns>
    [HttpGet("GetPeerConfig/{name}")]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult<string>> Get(string name)
    {
        var config = await peerRepository.GeneratePeerContentConfigAsync(name);
        return Ok(config);
    }

    /// <summary>
    /// Updates the details of a specific peer by name.
    /// </summary>
    /// <param name="name">The name of the peer to update.</param>
    /// <param name="peer">The updated peer details.</param>
    /// <returns>An ApiResult containing the updated Peer object.</returns>
    [HttpPut("{name}")]
    [ProducesResponseType(typeof(ApiResult<Peer>), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult<Peer>> Put(string name, [FromBody] UpdatePeerDto peer)
    {
        return Ok(await peerRepository.UpdatePeerAsync(peer, name));
    }

    /// <summary>
    /// Resets the specified peer's data and updates its status to active.
    /// </summary>
    /// <param name="name">The name of the peer to reset.</param>
    /// <param name="peer">The peer data to reset.</param>
    /// <returns>An ApiResult containing the updated Peer object.</returns>
    [HttpPost("Reset/{name}")]
    [ProducesResponseType(typeof(ApiResult<Peer>), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult<Peer>> ResetVolume(string name, [FromBody] ResetPeerDto peer)
    {
        return Ok(await peerRepository.ResetPeerAsync(peer, name));
    }

    /// <summary>
    /// Disables a specific peer by name.
    /// </summary>
    /// <param name="name">The name of the peer to disable.</param>
    /// <returns>An ApiResult containing the updated Peer object.</returns>
    [HttpPost("Disabled/{name}")]
    [ProducesResponseType(typeof(ApiResult<Peer>), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult<Peer>> Disabled(string name)
    {
        return Ok(await peerRepository.DisabledPeerAsync(name));
    }

    /// <summary>
    /// Activates a specific peer by name.
    /// </summary>
    /// <param name="name">The name of the peer to activate.</param>
    /// <returns>An ApiResult containing the updated Peer object.</returns>
    [HttpPost("Active/{name}")]
    [ProducesResponseType(typeof(ApiResult<Peer>), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult<Peer>> Active(string name)
    {
        return Ok(await peerRepository.ActivePeerAsync(name));
    }

    /// <summary>
    /// Retrieves the details of a specific peer by name.
    /// </summary>
    /// <param name="peername">The name of the peer.</param>
    /// <returns>An ApiResult containing the Peer object.</returns>
    [HttpGet("{peername}")]
    [ProducesResponseType(typeof(ApiResult<Peer>), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult<Peer>> GetPeerByName(string peername)
    {
        return Ok(await peerRepository.GetPeerByNameAsync(peername));
    }

    /// <summary>
    /// Deletes a specific peer by name.
    /// </summary>
    /// <param name="name">The name of the peer to delete.</param>
    /// <returns>An ApiResult indicating the success of the operation.</returns>
    [HttpDelete("{name}")]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult> Delete(string name)
    {
        await peerRepository.DeletePeerAsync(name);
        return Ok();
    }
    
    [HttpPost("{FixedActive/@interfacename}")]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult> FixedActive(string @interfacename)
    {
        await peerRepository.FixedPeerAsync(interfacename);
        return Ok();
    }
}