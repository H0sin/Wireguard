using System.ComponentModel.DataAnnotations;

namespace Wireguard.Api.Data.Enums;

public enum ApiResultStatusCode
{
    [Display(Name = "success")] Success = 0,

    [Display(Name = "server error")] ServerError = 1,

    [Display(Name = "bad request")] BadRequest = 2,

    [Display(Name = "not found")] NotFound = 3,

    [Display(Name = "empty")] ListEmpty = 4,

    [Display(Name = "loaded ex")] LogicError = 5,

    [Display(Name = "un authrized")] UnAuthorized = 6,
}