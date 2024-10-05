using Dapper;
using Npgsql;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Helpers;

namespace Wireguard.Api.Data.Repositories;

public class PeerRepository(IConfiguration configuration, IInterfaceRepository interfaceRepository) : IPeerRepository
{
    public async Task<bool> InsertAsync(AddPeerDto peer, string interfaceName,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync(cancellationToken);
        var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var @interface = await interfaceRepository
                .GetInterfaceByNameAsync(interfaceName);

            if (@interface == null) throw new ApplicationException($"not found interface by name {interfaceName}");

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
                peer.AllowedIPs,
                interfaceId = @interface.Id
            });

            var filepaht = configuration.GetValue<string>("Interface_Directory") + $"/{@interface.Name}.conf";

            if (response > 0 && !await WireguardHelpers.CreatePeer(peer, @interface, filepaht))
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