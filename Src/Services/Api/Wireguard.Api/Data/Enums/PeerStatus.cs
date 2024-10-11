namespace Wireguard.Api.Data.Enums;

[Flags]
public enum PeerStatus : byte
{
    Active,
    Disabled,
    Expired,
    OnHold,
    Limited
}