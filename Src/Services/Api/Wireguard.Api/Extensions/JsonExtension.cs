﻿using Newtonsoft.Json;
using Wireguard.Api.Helpers;

namespace Wireguard.Api.Extensions;

public static class JsonExtension
{
    public static string SerializeModelToJsonObject(this Dictionary<string, string> model)
    {
        return JsonConvert.SerializeObject(model);
    }

    public static string LowercaseContractResolver(this object obj)
    {
        var settings = new JsonSerializerSettings();
        settings.ContractResolver = new LowercaseContractResolver();
        return JsonConvert.SerializeObject(obj, Formatting.Indented, settings);
    }
}