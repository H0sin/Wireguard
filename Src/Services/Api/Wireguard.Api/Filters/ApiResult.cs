﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Wireguard.Api.Data.Enums;
using Wireguard.Api.Extensions;

namespace Wireguard.Api.Filters;

public class ApiResult //<TData> where TData : class
{
    public bool IsSuccess { get; set; }
    public ApiResultStatusCode StatusCode { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Message { get; set; }
    public string JsonValidationMessage { get; set; }

    public ApiResult(bool isSuccess, ApiResultStatusCode statusCode, string? message = null,
        string? jsonValidationMessage = null)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Message = message ?? statusCode.GetAttribute<DisplayAttribute>().Name;
        JsonValidationMessage = jsonValidationMessage;
    }

    #region Implicit Operators

    public static implicit operator ApiResult(OkResult result)
    {
        return new ApiResult(true, ApiResultStatusCode.Success);
    }

    public static implicit operator ApiResult(JsonResult result)
    {
        return new ApiResult(true, ApiResultStatusCode.Success);
    }

    public static implicit operator ApiResult(BadRequestResult result)
    {
        return new ApiResult(false, ApiResultStatusCode.BadRequest);
    }

    public static implicit operator ApiResult(BadRequestObjectResult result)
    {
        var message = result.Value?.ToString();
        if (result.Value is SerializableError errors)
        {
            var errorMessages = errors.SelectMany(p => (string[])p.Value).Distinct();
            message = string.Join(" | ", errorMessages);
        }

        return new ApiResult(false, ApiResultStatusCode.BadRequest, message);
    }

    public static implicit operator ApiResult(ContentResult result)
    {
        return new ApiResult(true, ApiResultStatusCode.Success, result.Content);
    }

    public static implicit operator ApiResult(NotFoundResult result)
    {
        return new ApiResult(false, ApiResultStatusCode.NotFound);
    }

    #endregion
}

public class ApiResult<TData> : ApiResult where TData : class
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] // if data is null for show result hide
    public TData Data { get; set; }

    public ApiResult(bool isSuccess, ApiResultStatusCode statusCode, TData data, string? message = null,
        string? jsonValidationMessage = null)
        : base(isSuccess, statusCode, message, jsonValidationMessage)
    {
        Data = data;
    }

    #region Implicit Operators

    // object 
    public static implicit operator ApiResult<TData>(TData data)
    {
        return new ApiResult<TData>(true, ApiResultStatusCode.Success, data);
    }

    public static implicit operator ApiResult<TData>(OkResult result)
    {
        return new ApiResult<TData>(true, ApiResultStatusCode.Success, null);
    }

    public static implicit operator ApiResult<TData>(JsonResult result)
    {
        return new ApiResult<TData>(true, ApiResultStatusCode.Success, null);
    }

    public static implicit operator ApiResult<TData>(OkObjectResult result)
    {
        return new ApiResult<TData>(true, ApiResultStatusCode.Success, (TData)result.Value);
    }

    public static implicit operator ApiResult<TData>(BadRequestResult result)
    {
        return new ApiResult<TData>(false, ApiResultStatusCode.BadRequest, null);
    }

    public static implicit operator ApiResult<TData>(UnauthorizedResult result)
    {
        return new ApiResult<TData>(false, ApiResultStatusCode.BadRequest, null);
    }

    public static implicit operator ApiResult<TData>(BadRequestObjectResult result)
    {
        var message = result.Value?.ToString();
        if (result.Value is SerializableError errors)
        {
            var errorMessages = errors.SelectMany(p => (string[])p.Value).Distinct();
            message = string.Join(" | ", errorMessages);
        }

        return new ApiResult<TData>(false, ApiResultStatusCode.BadRequest, null, message);
    }

    public static implicit operator ApiResult<TData>(ContentResult result)
    {
        return new ApiResult<TData>(true, ApiResultStatusCode.Success, null, result.Content);
    }

    public static implicit operator ApiResult<TData>(NotFoundResult result)
    {
        return new ApiResult<TData>(false, ApiResultStatusCode.NotFound, null);
    }

    public static implicit operator ApiResult<TData>(NotFoundObjectResult result)
    {
        return new ApiResult<TData>(false, ApiResultStatusCode.NotFound, (TData)result.Value);
    }

    #endregion
}