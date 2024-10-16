using System.Text;
using Dapper;
using Npgsql;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Enums;
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

                    peer.Name = Guid.NewGuid().ToString("N");
                    peer.PublicKey = keyPair.PublicKey;
                    peer.PresharedKey = keyPair.PresharedKey;
                    peer.AllowedIPs = new List<string> { availableIp.Ip };

                    string command = """
                                        INSERT INTO PEER (InterfaceId,
                                                          Name,
                                                          PublicKey,
                                                          PrivateKey,
                                                          PresharedKey,
                                                          AllowedIPs,
                                                          Mtu,
                                                          EndpointAllowedIPs,
                                                          Dns,
                                                          PersistentKeepalive,
                                                          OnHoldExpireDurection,
                                                          Status,
                                                          TotalVolume,
                                                          ExpireTime
                                                          ) Values (@InterfaceId,
                                                                    @Name,
                                                                    @PublicKey,
                                                                    @PrivateKey,
                                                                    @PresharedKey,
                                                                    @AllowedIPs,
                                                                    @Mtu,
                                                                    @EndpointAllowedIPs,
                                                                    @Dns,
                                                                    @PersistentKeepalive,
                                                                    @OnHoldExpireDurection,
                                                                    @Status,
                                                                    @TotalVolume,
                                                                    @ExpireTime)
                                     """;

                    int response = await connection.ExecuteAsync(command, new
                    {
                        interfaceId = @interface.Id,
                        peer.Name,
                        keyPair.PublicKey,
                        keyPair.PrivateKey,
                        keyPair.PresharedKey,
                        AllowedIPs = string.Join(",", peer.AllowedIPs),
                        Mtu = peer.Mtu,
                        EndpointAllowedIPs = peer.EndpointAllowedIPs,
                        Dns = peer.Dns,
                        PersistentKeepalive = peer.PersistentKeepalive,
                        OnHoldExpireDurection = peer.OnHoldExpireDurection,
                        Status = peer.Status,
                        TotalVolume = peer.TotalVolume,
                        ExpireTime = peer.ExpireTime
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
                string query = """  
                                SELECT COUNT(*) FROM PEER
                                WHERE Name = @Name
                               """;

                int exist = await connection.QueryFirstOrDefaultAsync<int>(query, new { Name = peer.Name });

                if (exist > 0) throw new ApplicationException("peer by name is exist");

                KeyPair keyPair = KeyGeneratorHelper.GenerateKeys();

                string command = """
                                    INSERT INTO PEER (InterfaceId,
                                                      Name,
                                                      PublicKey,
                                                      PrivateKey,
                                                      PresharedKey,
                                                      AllowedIPs,
                                                      Mtu,
                                                      EndpointAllowedIPs,
                                                      Dns,
                                                      PersistentKeepalive,
                                                      OnHoldExpireDurection,
                                                      Status,
                                                      TotalVolume,
                                                      ExpireTime
                                                      ) Values (@InterfaceId,@Name,@PublicKey,@PrivateKey,@PresharedKey,@AllowedIPs,@Mtu,@EndpointAllowedIPs,@Dns,
                                                               @PersistentKeepalive,
                                                               @OnHoldExpireDurection,
                                                               @Status,
                                                               @TotalVolume,
                                                               @ExpireTime)
                                 """;

                var availableIp = ipAddresses.FirstOrDefault(x => x.Available);

                peer.AllowedIPs = peer.AllowedIPs.Count == 0 ? new List<string> { availableIp.Ip } : peer.AllowedIPs;
                peer.PublicKey ??= keyPair.PublicKey;

                int response = await connection.ExecuteAsync(command, new
                {
                    interfaceId = @interface.Id,
                    Name = peer.Name,
                    PrivateKey = keyPair.PrivateKey,
                    PublicKey = peer.PublicKey,
                    PresharedKey = peer.PresharedKey,
                    AllowedIPs = string.Join(", ", peer.AllowedIPs),
                    Mtu = peer.Mtu,
                    EndpointAllowedIPs = peer.EndpointAllowedIPs,
                    Dns = peer.Dns,
                    PersistentKeepalive = peer.PersistentKeepalive,
                    OnHoldExpireDurection = peer.OnHoldExpireDurection,
                    Status = peer.Status,
                    TotalVolume = peer.TotalVolume,
                    ExpireTime = peer.ExpireTime
                });

                if (response > 0 && !await WireguardHelpers.CreatePeer(peer, @interface))
                    throw new ApplicationException("failed to create peer");

                var ids = peer.AllowedIPs.FirstOrDefault();

                await ipAddressRepository.OutOfReachIpAddressAsync(new List<int>() { availableIp.Id }, connection,
                    transaction);

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

    public async Task<FilterPeerDto> FilterPeerAsync(FilterPeerDto filter,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync(cancellationToken);

        var sqlBuilder = new StringBuilder();
        sqlBuilder.AppendLine("""
                                  SELECT P.*, I.*
                                  FROM PEER P
                                  JOIN Interface I ON P.InterfaceId = I.Id
                                  WHERE I.Name = @InterfaceName
                              """);

        // Add the condition for Name only if it's not null or empty
        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            sqlBuilder.AppendLine("AND P.Name ILIKE '%' || @Name || '%'");
        }

        sqlBuilder.AppendLine("ORDER BY P.Id LIMIT @Take OFFSET @Skip;");

        var sql = sqlBuilder.ToString();

        // Execute the query to retrieve data
        var peers = await connection.QueryAsync<Peer, Interface, Peer>(
            sql,
            (peer, @interface) =>
            {
                peer.InterfaceId = @interface.Id;
                return peer;
            },
            new
            {
                Name = string.IsNullOrWhiteSpace(filter.Name) ? null : filter.Name, // Pass null if Name is empty
                filter.InterfaceName,
                filter.Take,
                filter.Skip
            },
            splitOn: "Id"
        );

        // Dynamically building the SQL query for counting records
        var countSqlBuilder = new StringBuilder();
        countSqlBuilder.AppendLine("""
                                       SELECT COUNT(*)
                                       FROM PEER P
                                       JOIN Interface I ON P.InterfaceId = I.Id
                                       WHERE I.Name = @InterfaceName
                                   """);

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            countSqlBuilder.AppendLine("AND P.Name ILIKE '%' || @Name || '%'");
        }

        var countSql = countSqlBuilder.ToString();

        // Execute the query to count records
        var countPeer = await connection.ExecuteScalarAsync<int>(countSql, new
        {
            Name = string.IsNullOrWhiteSpace(filter.Name) ? null : filter.Name, // Pass null if Name is empty
            filter.InterfaceName
        });

        return new FilterPeerDto
        {
            Take = filter.Take,
            Skip = filter.Skip,
            Name = filter.Name,
            InterfaceName = filter.InterfaceName,
            PublicKey = filter.PublicKey,
            CountPeer = countPeer,
            Peers = peers.ToList()
        };
    }

    public async Task<string> GeneratePeerContentConfigAsync(string name)
    {
        await using var connection =
            new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync();

        var query = """
                    SELECT * FROM PEER P
                    JOIN Interface I ON P.InterfaceId = I.Id
                    WHERE P.Name = @Name
                    """;

        var content = await connection.QueryAsync<Peer, Interface, string>(query, (peer, @interface) =>
        {
            if (@interface is null) throw new ApplicationException("interface not found");
            return peer switch
            {
                null => throw new ApplicationException("peer by name is null"),
                _ => $"""
                      [Interface]
                      PrivateKey = {peer.PrivateKey}
                      Address = {peer.AllowedIPs} 
                      MTU = {peer.Mtu}
                      DNS = {peer.Dns}

                      [Peer]
                      PublicKey = {@interface.PublicKey}
                      AllowedIPs = {peer.EndpointAllowedIPs}
                      Endpoint = {@interface.EndPoint}:{@interface.ListenPort}
                      PersistentKeepalive = {peer.PersistentKeepalive} 

                      """
            };
        }, new { Name = name });

        return content.FirstOrDefault();
    }

    public async Task<Peer?> UpdatePeerAsync(UpdatePeerDto peer, string name)
    {
        var getPeer = await GetPeerByNameAsync(name);
        if (getPeer is null) throw new ApplicationException("peer not found");

        await using var connection =
            new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync();

        var @interface = await connection.QueryFirstOrDefaultAsync<Interface>("SELECT * FROM INTERFACE WHERE Id = @Id",
            new { Id = getPeer.InterfaceId });

        if (@interface.Status != "active")
            throw new ApplicationException(
                $"interface is not active please befor active interface by name {@interface.Name}");

        string status = "active";

        var command = """
                        UPDATE PEER SET
                           EndPoint = @EndPoint,
                           Dns = @Dns,
                           Mtu = @Mtu,
                           PersistentKeepalive = @PersistentKeepalive,
                           EndpointAllowedIPs = @EndpointAllowedIPs,
                           ExpireTime = @ExpireTime,
                           TotalVolume = @TotalVolume,
                           Status = @Status
                           WHERE Name = @Name             
                      """;

        var result = await connection.ExecuteAsync(command, new
        {
            EndPoint = peer.EndPoint,
            Dns = peer.Dns,
            Mtu = peer.Mtu,
            PersistentKeepalive = peer.PersistentKeepalive,
            EndpointAllowedIPs = peer.EndpointAllowedIPs,
            ExpireTime = peer.ExpireTime,
            TotalVolume = peer.TotalVolume,
            Status = "active",
            Name = name
        });

        if (result is 0) throw new ApplicationException("peer not found");

        switch (getPeer.Status)
        {
            case "limited":
                if (getPeer.TotalReceivedVolume < peer.TotalVolume)
                    await WireguardHelpers.CreatePeer(new AddPeerDto(getPeer), @interface);
                break;
            case "expired":
                if (getPeer.ExpireTime < peer.ExpireTime)
                    WireguardHelpers.CreatePeer(new AddPeerDto(getPeer), @interface);
                break;
            case "onhold":
                break;
        }

        return await GetPeerByNameAsync(name);
    }

    public async Task<Peer?> GetPeerByNameAsync(string name)
    {
        await using var connection =
            new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync();

        var query = """
                    SELECT * FROM PEER P
                    WHERE P.Name = @Name
                    """;

        return await connection
            .QuerySingleOrDefaultAsync<Peer>(query, new { Name = name });
    }


    public async Task<Peer?> ReastPeerAsync(ReastPeerDto peer, string name)
    {
        await using var connection =
            new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();

        try
        {
            Peer? getPeer = await GetPeerByNameAsync(name);

            if (getPeer is null) throw new ApplicationException("peer not found");

            getPeer.ExpireTime = peer.ExpireTime;
            getPeer.TotalVolume = peer.TotalValue;
            getPeer.TotalReceivedVolume = 0;
            getPeer.DownloadVolume = 0;
            getPeer.UploadVolume = 0;
            getPeer.LastTotalReceivedVolume = 0;
            getPeer.LastDownloadVolume = 0;
            getPeer.LastUploadVolume = 0;

            var command = """
                          UPDATE PEER SET 
                            TotalReceivedVolume = 0,
                            DownloadVolume = 0,
                            UploadVolume = 0,
                            ExpireTime = @ExpireTime,
                            TotalVolume = @TotalVolume,
                            LastTotalReceivedVolume = 0,
                            LastDownloadVolume = 0,
                            LastUploadVolume = 0,
                            Status = 'active'
                          WHERE Name = @Name
                          """;

            await connection.QuerySingleOrDefaultAsync(command, new
            {
                ExpireTime = peer.ExpireTime,
                TotalVolume = peer.TotalValue,
                Name = name
            });

            var @interface = await connection.QuerySingleOrDefaultAsync<Interface>(
                "SELECT * FROM INTERFACE WHERE Id = @Id",
                new { Id = getPeer.InterfaceId });

            var newpeer = new AddPeerDto(getPeer);

            if (getPeer.Status != "active")
                if (!await WireguardHelpers.CreatePeer(newpeer, @interface))
                    throw new Exception("failed to create peer");

            await transaction.CommitAsync();

            return getPeer;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw e;
        }
    }

    public async Task<Peer?> DisabledPeerAsync(string name)
    {
        await using var connection =
            new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();

        try
        {
            Peer? getPeer = await GetPeerByNameAsync(name);

            if (getPeer is null) throw new ApplicationException("peer not found");

            if (getPeer.Status == PeerStatus.active.ToString())
            {
                var query = await connection.QuerySingleOrDefaultAsync(
                    "UPDATE FROM PEER SET Status = 'disabled' WHERE Name = @Name", new { Name = name });

                var @interface = await connection.QuerySingleOrDefaultAsync<Interface>(
                    "SELECT * FROM INTERFACE WHERE Id = @Id",
                    new { Id = getPeer.InterfaceId });

                var newpeer = new AddPeerDto(getPeer);

                await WireguardHelpers.RemovePeer(@interface.Name, getPeer.PublicKey);
                await WireguardHelpers.Save(@interface.Name);
            }

            await transaction.CommitAsync();

            return getPeer;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw e;
        }
    }

    public async Task<Peer?> ActivePeerAsync(string name)
    {
        await using var connection =
            new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();

        try
        {
            Peer? getPeer = await GetPeerByNameAsync(name);

            if (getPeer is null) throw new ApplicationException("peer not found");

            if (getPeer.Status == PeerStatus.disabled.ToString())
            {
                var query = await connection.QuerySingleOrDefaultAsync(
                    "UPDATE FROM PEER SET Status = 'active' WHERE Name = @Name", new { Name = name });

                var @interface = await connection.QuerySingleOrDefaultAsync<Interface>(
                    "SELECT * FROM INTERFACE WHERE Id = @Id",
                    new { Id = getPeer.InterfaceId });

                var newpeer = new AddPeerDto(getPeer);

                await WireguardHelpers.RemovePeer(@interface.Name, getPeer.PublicKey);
                await WireguardHelpers.Save(@interface.Name);
            }

            await transaction.CommitAsync();

            return getPeer;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw e;
        }
    }
}