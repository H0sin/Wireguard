using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Wireguard.Api.Data.Enums;

namespace Wireguard.Api.Filters;

public class ExceptionHandlerFilter : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        context.Result =
            new JsonResult(new ApiResult(false, ApiResultStatusCode.BadRequest,  context.Exception.Message));
        
        await Task.CompletedTask;
    }
}