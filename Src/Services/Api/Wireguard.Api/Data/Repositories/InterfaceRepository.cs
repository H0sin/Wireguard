using Dapper;
using Npgsql;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;
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

    public async Task<Interface?> GetInterfaceByNameAsync(string name)
    {
        await using var connection = new NpgsqlConnection
            (configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        var @interface =
            await connection.QueryFirstOrDefaultAsync<Interface>("SELECT * FROM Interface WHERE Name = @name",
                new { name });

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
                             VALUES (@Name,@Address,@EndPoint,@SaveConfig,@PreUp,@PostUp,@PreDown,@PostDown,@ListenPort,@PrivateKey,@IpAddress,@PublicKey
                             )

                             RETURNING Id;
                             """;
            var id = await connection.ExecuteScalarAsync<int>(command, entity);

            bool checkout = await ipAddressRepository.AddIpAddressAsync(entity.IpAddress, id);

            if (!checkout) throw new ApplicationException("failed to add ip address");

            bool file = await Process
                .AddInterfaceFile(entity, configuration.GetValue<string>("Interface_Directory"));

            if (file == false) throw new ApplicationException("failed to add interface file");

            await transaction.CommitAsync();

            return id > 0;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return false;
        }
    }
}