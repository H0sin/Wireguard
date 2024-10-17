using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

public class IpRangeUtility
{
    /// <summary>
    /// Gets the base IP address from the CIDR notation.
    /// </summary>
    public string BaseIp { get; private set; }

    /// <summary>
    /// Gets the prefix length from the CIDR notation.
    /// </summary>
    public int PrefixLength { get; private set; }

    /// <summary>
    /// Gets the starting IP address of the range as a UInt32.
    /// </summary>
    public uint StartIp { get; private set; }

    /// <summary>
    /// Gets the ending IP address of the range as a UInt32.
    /// </summary>
    public uint EndIp { get; private set; }

    /// <summary>
    /// Gets the total number of IP addresses in the range.
    /// </summary>
    public long NumberOfIps { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IpRangeUtility"/> class with the specified CIDR.
    /// </summary>
    /// <param name="cidr">The CIDR notation (e.g., "1.1.1.0/24").</param>
    /// <exception cref="ArgumentException">Thrown when the CIDR format is null or empty.</exception>
    /// <exception cref="FormatException">Thrown when the CIDR format or IP address is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the prefix length is out of the valid range.</exception>
    public IpRangeUtility(string cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr))
            throw new ArgumentException("CIDR format is null or invalid.");

        var parts = cidr.Split('/');
        if (parts.Length != 2)
            throw new FormatException("Invalid CIDR format. Expected format: 'IP/Prefix'.");

        BaseIp = parts[0];
        if (!IPAddress.TryParse(BaseIp, out IPAddress ip))
            throw new FormatException("Invalid IP address format.");

        if (!int.TryParse(parts[1], out int prefixLength))
            throw new FormatException("Invalid prefix length.");

        if (prefixLength < 0 || prefixLength > 32)
            throw new ArgumentOutOfRangeException("Prefix length must be between 0 and 32.");

        PrefixLength = prefixLength; // Ensure PrefixLength is set correctly

        uint ipInt = IpToUInt(ip);
        uint mask = PrefixLength == 0 ? 0 : 0xFFFFFFFF << (32 - PrefixLength);
        StartIp = ipInt & mask;
        EndIp = StartIp | ~mask;

        NumberOfIps = (long)(EndIp - StartIp) + 1;
    }

    /// <summary>
    /// Converts an <see cref="IPAddress"/> to a <see cref="UInt32"/>.
    /// </summary>
    /// <param name="ip">The IP address to convert.</param>
    /// <returns>The IP address as a <see cref="UInt32"/>.</returns>
    private uint IpToUInt(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    /// <summary>
    /// Converts a <see cref="UInt32"/> to an <see cref="IPAddress"/>.
    /// </summary>
    /// <param name="ip">The IP address as a <see cref="UInt32"/>.</param>
    /// <returns>The IP address as an <see cref="IPAddress"/>.</returns>
    private IPAddress UIntToIp(uint ip)
    {
        var bytes = BitConverter.GetBytes(ip);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return new IPAddress(bytes);
    }

    /// <summary>
    /// Validates whether the provided string is a valid IP address.
    /// </summary>
    /// <param name="ip">The IP address string to validate.</param>
    /// <returns><c>true</c> if the IP address is valid; otherwise, <c>false</c>.</returns>
    public static bool IsValidIp(string ip)
    {
        return IPAddress.TryParse(ip, out _);
    }

    /// <summary>
    /// Determines whether a specific IP address is within the defined range.
    /// </summary>
    /// <param name="ip">The IP address to check.</param>
    /// <returns><c>true</c> if the IP address is within the range; otherwise, <c>false</c>.</returns>
    public bool IsIpInRange(string ip)
    {
        if (!IsValidIp(ip))
            return false;

        uint ipInt = IpToUInt(IPAddress.Parse(ip));
        return ipInt >= StartIp && ipInt <= EndIp;
    }

    /// <summary>
    /// Generates a list of all IP addresses within the defined range.
    /// </summary>
    /// <returns>A list of IP address strings.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the IP range is too large to generate.</exception>
    public List<string> GetAllIps()
    {
        var ips = new List<string>();

        // Prevent generating extremely large lists
        if (NumberOfIps > 1000000)
            throw new InvalidOperationException("The IP range is too large to generate.");

        for (uint ip = StartIp + 2; ip <= EndIp; ip++)
        {
            ips.Add(UIntToIp(ip).ToString());
        }

        return ips;
    }
}