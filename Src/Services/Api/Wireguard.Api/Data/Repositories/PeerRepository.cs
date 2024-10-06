using Dapper;
using Npgsql;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Helpers;

namespace Wireguard.Api.Data.Repositories;

public class PeerRepository(
    IConfiguration configuration,
    IInterfaceRepository interfaceRepository,
    IIpAddressRepository ipAddressRepository) : IPeerRepository
{
    public async Task<bool> InsertAsync(AddPeerDto peer, string interfaceName,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync(cancellationToken);

        var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var @interface = await interfaceRepository
            .GetInterfaceByNameAsync(interfaceName);

        if (@interface == null) throw new ApplicationException($"not found interface by name {interfaceName}");

        List<IpAddress> ipAddresses = await ipAddressRepository.GetIpAddressByInterfaceIdAsync(@interface.Id);

        if (peer.Count > ipAddresses.Count(x => x.Available == true)) throw new ApplicationException("peer is full");

        if (peer.Bulk)
        {
            try
            {
                List<int> idsIp = new List<int>();

                while (0 < peer.Count)
                {
                    var availableIp = ipAddresses.FirstOrDefault(x => x.Available);

                    ipAddresses.RemoveAt(ipAddresses.IndexOf(availableIp));

                    idsIp.Add(availableIp.Id);

                    if (availableIp == null) throw new ApplicationException("no available ip address");

                    KeyPair keyPair = KeyGeneratorHelper.GenerateKeys();

                    peer.Name ??= Guid.NewGuid().ToString("N");
                    peer.PublicKey ??= keyPair.PublicKey;
                    peer.PresharedKey ??= keyPair.PresharedKey;
                    peer.AllowedIPs ??= new List<string> { availableIp.Ip };
                    
                    string command = """
                                        INSERT INTO PEER (InterfaceId,
                                                          Name,
                                                          PublicKey,
                                                          PrivateKey,
                                                          PresharedKey,
                                                          AllowedIPs
                                                          ) Values (@InterfaceId,@Name,@PublicKey,@PrivateKey,@PresharedKey,@AllowedIPs)
                                     """;

                    int response = await connection.ExecuteAsync(command, new
                    {
                        interfaceId = @interface.Id,
                        peer.Name,
                        keyPair.PublicKey,
                        keyPair.PrivateKey,
                        keyPair.PresharedKey,
                        AllowedIPs = string.Join(",", peer.AllowedIPs),
                    });

                    if (response > 0 && !await WireguardHelpers.CreatePeer(peer, @interface))
                        throw new ApplicationException("failed to create peer");

                    peer.Count--;
                }

                await ipAddressRepository.OutOfReachIpAddressAsync(idsIp, connection, transaction);

                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        else
        {
            try
            {
                string command = """
                                    INSERT INTO PEER (InterfaceId,
                                                      Name,
                                                      PublicKey,
                                                      PresharedKey,
                                                      AllowedIPs,
                                                      EndPoint
                                                      ) Values (@InterfaceId,@Name,@PublicKey,@PresharedKey,@AllowedIPs,@EndPoint)
                                 """;

                int response = await connection.ExecuteAsync(command, new
                {
                    peer.Name,
                    peer.PublicKey,
                    peer.EndPoint,
                    peer.PresharedKey,
                    AllowedIPs = string.Join(",", peer.AllowedIPs),
                    interfaceId = @interface.Id
                });

                var filepaht = configuration.GetValue<string>("Interface_Directory") + $"/{@interface.Name}.conf";

                if (response > 0 && !await WireguardHelpers.CreatePeer(peer, @interface))
                    throw new ApplicationException("failed to create peer");

                await transaction.CommitAsync(cancellationToken);

                return response > 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}