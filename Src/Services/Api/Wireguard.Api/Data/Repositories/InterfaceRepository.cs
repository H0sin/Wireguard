﻿using Dapper;
using Npgsql;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Enums;
using Wireguard.Api.Helpers;

namespace Wireguard.Api.Data.Repositories;

public class InterfaceRepository(IConfiguration configuration, IIpAddressRepository ipAddressRepository)
    : IInterfaceRepository
{
    public async Task<ICollection<Interface>> GetAllAsync()
    {
        await using var connection = new NpgsqlConnection
            (configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        var interfaces = await connection.QueryAsync<Interface>("SELECT * FROM Interface");

        return interfaces.ToList();
    }

    public async Task<InterfaceDetailDto> GetAsync(string name)
    {
        await using var connection = new NpgsqlConnection
            (configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        string query = $"""
                        SELECT 
                            I.Name AS Name,
                            I.ListenPort,
                            I.PublicKey,
                            I.Status,
                            COALESCE(Sum(P.totalreceivedvolume), 0) as TotoalDataUsed,
                            COALESCE(SUM(P.totalvolume), 0) as TotalData
                            FROM Interface I
                            LEFT JOIN Peer P ON P.InterfaceId = I.Id 
                                 WHERE I.Name = @Name
                                 GROUP BY I.Name, I.ListenPort, I.PublicKey, I.Status
                        """;

        var @interface =
            await connection.QuerySingleOrDefaultAsync<InterfaceDetailDto>(query, new { Name = name });

        return @interface;
    }

    public async Task<Interface?> GetInterfaceByNameAsync(string name)
    {
        await using var connection = new NpgsqlConnection
            (configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        var @interface =
            await connection.QueryFirstOrDefaultAsync<Interface>("SELECT * FROM Interface WHERE Name = @Name",
                new { Name = name });

        return @interface;
    }

    public async Task<bool> InsertAsync(AddInterfaceDto entity)
    {
        if (await GetInterfaceByNameAsync(entity.Name) is not null)
            throw new ApplicationException($"interface by name {entity.Name} already exists");

        await using var connection = new NpgsqlConnection
            (configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();

        try
        {
            bool exist = await ipAddressRepository.ExistIpAddressAsync(entity.IpAddress);

            if (exist) throw new ApplicationException("this address has already been added.");

            string command = """
                             INSERT INTO Interface (
                             Name,
                             Status,                       
                             Address,
                             EndPoint,
                             SaveConfig,
                             PreUp, 
                             PostUp,
                             PreDown,
                             PostDown,
                             ListenPort,
                             PrivateKey,
                             IpAddress,
                             PublicKey)
                             VALUES (@Name,@Status,@Address,@EndPoint,@SaveConfig,@PreUp,@PostUp,@PreDown,@PostDown,@ListenPort,@PrivateKey,@IpAddress,@PublicKey)
                             RETURNING Id;
                             """;

            var id = await connection.ExecuteScalarAsync<int>(command, entity);

            bool checkout = await ipAddressRepository.AddIpAddressAsync(entity.IpAddress, id, connection, transaction);

            if (!checkout) throw new ApplicationException("failed to add ip address");

            bool file = await WireguardHelpers
                .AddInterfaceFile(entity, configuration.GetValue<string>("Interface_Directory"));

            if (file == false) throw new ApplicationException("failed to add interface file");

            await transaction.CommitAsync();

            return id > 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> ChangeStatusInterfaceAsync(string name, InterfaceStatus status)
    {
        await using var connection = new NpgsqlConnection
            (configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();

        try
        {
            var @interface = await GetInterfaceByNameAsync(name);

            if (@interface == null) throw new ApplicationException($"interface by name \"{name}\" not found");

            var output = await connection.ExecuteAsync("UPDATE Interface SET Status = @Status WHERE Name = @Name",
                new { Name = name, Status = status.ToString() });

            var response = await WireguardHelpers.StatusWireguard(status, name);

            Console.WriteLine(status.ToString());

            if (!response.Item2) throw new ApplicationException($"failed to update interface {response.Item1}");

            await transaction.CommitAsync();

            return output > 0;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string name)
    {
        await using var connection = new NpgsqlConnection
            (configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();

        try
        {
            var @interface = await GetInterfaceByNameAsync(name);

            if (@interface == null) throw new ApplicationException($"interface by name \"{name}\" not found");

            bool change = await ChangeStatusInterfaceAsync(@interface.Name, InterfaceStatus.disabled);

            if (!change) throw new ApplicationException("failed to remove interface");

            string path = configuration.GetValue<string>("Interface_Directory");

            if (!await WireguardHelpers.DeleteInterfaceFile(path + $"/{name}.conf"))
                throw new ApplicationException("failed to delete interface file");
            //
            // // delete peer
            // string command = "DELETE FROM PEER WHERE InterfaceId = @InterfaceId";
            // await connection.ExecuteAsync(command, new { InterfaceId = @interface.Id });
            //
            // // delete ip address
            // string command2 = "DELETE FROM IpAddress WHERE InterfaceId = @InterfaceId";
            // await connection.ExecuteAsync(command2, new { InterfaceId = @interface.Id });

            //delete interface
            string command = "DELETE FROM Interface WHERE Id = @Id";
            int response = await connection.ExecuteAsync(command, new { InterfaceId = @interface.Id });

            await transaction.CommitAsync();

            return response > 0;
        }
        catch (Exception e)
        {
            transaction.RollbackAsync();
            return false;
        }
    }
}