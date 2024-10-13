namespace Wireguard.Api.Data.Enums;

[Flags]
public enum PeerStatus : byte
{
    active,
    disabled,
    expired,
    onhold,
    limited
}