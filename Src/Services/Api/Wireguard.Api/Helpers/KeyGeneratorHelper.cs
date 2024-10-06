using System.Security.Cryptography;
using NaCl;

namespace Wireguard.Api.Helpers;

public class KeyPair
{
    public string? PrivateKey { get; set; }
    public string? PublicKey { get; set; }
    public string? PresharedKey { get; set; }
}

public static class KeyGeneratorHelper
{
    public static KeyPair GenerateKeys()
    {
        using var rng = RandomNumberGenerator.Create();
        Curve25519XSalsa20Poly1305.KeyPair(out var SecretKey, out var PublicKey);
        var privateKey = Convert.ToBase64String(SecretKey);
        var publicKey = Convert.ToBase64String(PublicKey);
        
        byte[] presharedKeyBytes = new byte[32];
        rng.GetBytes(presharedKeyBytes);

        return new KeyPair
        {
            PrivateKey = privateKey,
            PublicKey = publicKey,
            PresharedKey = Convert.ToBase64String(presharedKeyBytes)
        };
    }
}