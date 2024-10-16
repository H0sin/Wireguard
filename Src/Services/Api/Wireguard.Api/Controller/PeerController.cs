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
public class PeerController(IPeerRepository peerRepository) : ControllerBase
{
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

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<FilterPeerDto>), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult<FilterPeerDto>> Get([FromQuery] FilterPeerDto filterPeer)
    {
        var peers = await peerRepository.FilterPeerAsync(filterPeer, new CancellationToken(default));
        return Ok(peers);
    }

    [HttpGet("GetPeerConfig/{name}")]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult<string>> Get(string name)
    {
        var config = await peerRepository.GeneratePeerContentConfigAsync(name);
        return Ok(config);
    }

    [HttpPut("{name}")]
    [ProducesResponseType(typeof(ApiResult<Peer>), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult<Peer>> Put(string name, [FromBody] UpdatePeerDto peer)
    {
        return Ok(await peerRepository.UpdatePeerAsync(peer, name));
    }

    [HttpGet("{peername}")]
    [ProducesResponseType(typeof(ApiResult<Peer>), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ApiResult<Peer>> GetPeerByName(string peername)
    {
        return Ok(await peerRepository.GetPeerByNameAsync(peername));
    }
}