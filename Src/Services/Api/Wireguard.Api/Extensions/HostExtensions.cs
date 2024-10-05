using Npgsql;

namespace Wireguard.Api.Extensions
{
    public static class HostExtensions
    {
        public static IHost MigrateDatabase<TContext>(this IHost host, int? retry = 0)
        {
            int retryForAvailability = retry.Value;

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var configuration = services.GetRequiredService<IConfiguration>();
                var logger = services.GetRequiredService<ILogger<TContext>>();

                // migrate database
                try
                {
                    logger.LogInformation("migrating posgtresql database");

                    using var connection =
                        new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));
                    
                    connection.Open();
                    using var command = new NpgsqlCommand
                    {
                        Connection = connection
                    };

                                        
                    command.CommandText = "DROP TABLE IF EXISTS IpAddress CASCADE";
                    command.ExecuteNonQuery();
                    
                    command.CommandText = "DROP TABLE IF EXISTS Interface CASCADE";
                    command.ExecuteNonQuery();

                    command.CommandText = """
                                          CREATE TABLE Interface(
                                              Id BIGSERIAL PRIMARY KEY,
                                              Name VARCHAR(60) NOT NULL,
                                              Address VARCHAR(100) NOT NULL,
                                              EndPoint VARCHAR(200) NOT NULL,
                                              SaveConfig BOOLEAN NOT NULL,
                                              PreUp VARCHAR(200) DEFAULT '',
                                              PostUp VARCHAR(200) DEFAULT '',
                                              PreDown VARCHAR(200) DEFAULT '',
                                              PostDown VARCHAR(200) DEFAULT '',
                                              ListenPort VARCHAR(50) DEFAULT '',
                                              PrivateKey VARCHAR(255),
                                              IpAddress VARCHAR(100),
                                              PublicKey VARCHAR(255),
                                              CreateDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                              UpdateDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                                          );
                                          """;

                    command.ExecuteNonQuery();

                    command.CommandText = """
                                          CREATE TABLE IpAddress(
                                              Id BIGSERIAL PRIMARY KEY,
                                              InterfaceId BIGINT,
                                              Ip VARCHAR(50) NOT NULL,
                                              Available BOOLEAN DEFAULT FALSE,
                                              CreateDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                              UpdateDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                              FOREIGN KEY (InterfaceId) REFERENCES Interface(Id)
                                          )
                                          """;
                    command.ExecuteNonQuery();

                    // seed data

                    logger.LogInformation("migration has been completed!!!");
                }
                catch (NpgsqlException ex)
                {
                    logger.LogError("an error has been occured");

                    if (retryForAvailability < 50)
                    {
                        retryForAvailability++;
                        Thread.Sleep(2000);
                        MigrateDatabase<TContext>(host, retryForAvailability);
                    }
                }
            }

            return host;
        }
    }
}