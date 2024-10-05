namespace Wireguard.Api.Data.Enums;

[Flags]
public enum InterfaceStatus:byte
{
    active,
    disabled,
}