using Microsoft.AspNetCore.Mvc;
using Wireguard.Api.Data.Dtos;
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
}