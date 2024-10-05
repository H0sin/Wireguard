using Newtonsoft.Json.Serialization;

namespace Wireguard.Api.Helpers;

public class LowercaseContractResolver : DefaultContractResolver
{
    protected override string ResolvePropertyName(string propertyName)
    {
        return propertyName.ToLower();
    }
}