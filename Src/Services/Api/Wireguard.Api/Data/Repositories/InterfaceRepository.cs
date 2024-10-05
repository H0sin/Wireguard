using Dapper;
using Npgsql;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;

namespace Wireguard.Api.Data.Repositories;

public class InterfaceRepository(IConfiguration configuration) : IInterfaceRepository
{
    public async Task<ICollection<Interface>> GetAllAsync()
    {
        await using var connection = new NpgsqlConnection
            (configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        var interfaces = await connection.QueryAsync<Interface>("SELECT * FROM Interface");

        return interfaces.ToList();
    }

    public async Task<bool> InsertAsync(AddInterfaceDto entity)
    {
        await using var connection = new NpgsqlConnection
            (configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        string command = """
                         INSERT INTO Interface (
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
                         VALUES (@Address,@EndPoint,@SaveConfig,@PreUp,@PostUp,@PreDown,@PostDown,@ListenPort,@PrivateKey,@IpAddress,@PublicKey
                         )
                         """;

        var affected = await connection.ExecuteAsync(command, entity);
        return affected > 0;
    }
}