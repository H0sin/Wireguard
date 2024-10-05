using Microsoft.AspNetCore.Mvc;
using Wireguard.Api.Data.Repositories;
using Wireguard.Api.Filters;

namespace Wireguard.Api.Controller;

[ApiController]
[Route("[controller]")]
[ApiResultFilter]
[ServiceFilter(typeof(ExceptionHandlerFilter))]
public class IpAddressController(IIpAddressRepository ipAddressRepository) : ControllerBase
{
    
}