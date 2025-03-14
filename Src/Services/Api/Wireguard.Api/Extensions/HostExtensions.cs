﻿using Dapper;
using Npgsql;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Enums;
using Wireguard.Api.Helpers;

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
                    logger.LogInformation("Migrating PostgreSQL database...");

                    using var connection =
                        new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

                    connection.Open();

                    using var command = new NpgsqlCommand
                    {
                        Connection = connection
                    };

                    // Check if 'Interface' table exists
                    command.CommandText =
                        "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'interface')";

                    var interfaceExists = (bool)command.ExecuteScalar();

                    if (!interfaceExists)
                    {
                        // Create 'Interface' table if it doesn't exist
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
                                                  UpdateDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                                  Status VARCHAR(30)
                                              );
                                              """;
                        command.ExecuteNonQuery();
                    }
                    else
                    {
                        // Modify 'Interface' table if it exists
                        command.CommandText = """
                                              ALTER TABLE Interface
                                              ADD COLUMN IF NOT EXISTS UploadPercent double precision DEFAULT '1.0',
                                              ADD COLUMN IF NOT EXISTS DownloadPercent double precision DEFAULT '1.0'
                                              """;

                        command.ExecuteNonQuery();
                        
                        
                    }

                    // Check if 'IpAddress' table exists
                    command.CommandText =
                        "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'ipaddress')";
                    var ipAddressExists = (bool)command.ExecuteScalar();

                    if (!ipAddressExists)
                    {
                        // Create 'IpAddress' table if it doesn't exist
                        command.CommandText = """
                                              CREATE TABLE IpAddress(
                                                  Id BIGSERIAL PRIMARY KEY,
                                                  InterfaceId BIGINT,
                                                  Ip VARCHAR(50) NOT NULL,
                                                  Available BOOLEAN DEFAULT FALSE,
                                                  CreateDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                                  UpdateDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                                  FOREIGN KEY (InterfaceId) REFERENCES Interface(Id) ON DELETE CASCADE
                                              );
                                              """;
                        command.ExecuteNonQuery();
                    }
                    else
                    {
                        // Modify 'IpAddress' table if it exists
                    }

                    // Check if 'Peer' table exists
                    command.CommandText =
                        "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'peer')";

                    var peerExists = (bool)command.ExecuteScalar();

                    if (!peerExists)
                    {
                        // Create 'Peer' table if it doesn't exist
                        command.CommandText = """
                                              CREATE TABLE Peer(
                                                  Id BIGSERIAL PRIMARY KEY,
                                                  InterfaceId BIGINT,
                                                  Name VARCHAR(100),
                                                  PublicKey VARCHAR(255),
                                                  PresharedKey VARCHAR(255),
                                                  EndpointAllowedIPs VARCHAR(255),
                                                  Dns VARCHAR(255),
                                                  Mtu INT,
                                                  PersistentKeepalive INT,
                                                  PrivateKey VARCHAR(255),
                                                  AllowedIPs VARCHAR(700),
                                                  EndPoint VARCHAR(255),
                                                  CreateDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                                  UpdateDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                                  TotalReceivedVolume BIGINT DEFAULT 0,
                                                  LastTotalReceivedVolume BIGINT DEFAULT 0,
                                                  DownloadVolume BIGINT DEFAULT 0,
                                                  UploadVolume BIGINT DEFAULT 0,
                                                  StartTime BIGINT DEFAULT 0,
                                                  ExpireTime BIGINT,
                                                  TotalVolume BIGINT DEFAULT 0,
                                                  LastDownloadVolume BIGINT DEFAULT 0,
                                                  LastUploadVolume BIGINT DEFAULT 0,
                                                  Status VARCHAR(20),
                                                  OnHoldExpireDurection BIGINT DEFAULT 0,
                                                  FOREIGN KEY (InterfaceId) REFERENCES Interface(Id) ON DELETE CASCADE
                                              );
                                              """;
                        command.ExecuteNonQuery();
                    }
                    else
                    {
                        command.CommandText = """
                                              SELECT EXISTS (
                                                  SELECT 1 
                                                  FROM information_schema.columns 
                                                  WHERE table_name = 'peer' 
                                                  AND column_name = 'TotalVolume')
                                              """;


                        var columnExists = (bool)command.ExecuteScalar();

                        if (columnExists)
                        {
                            command.CommandText = "ALTER TABLE Peer ADD COLUMN TotalVolume BIGINT;";
                            command.ExecuteNonQuery();
                        }
                    }

                    logger.LogInformation("Migration has been completed!");


                    logger.LogInformation("on interfaces started");

                    var interfaces = connection.Query<Interface>("SELECT * FROM INTERFACE");

                    foreach (var @interface in interfaces)
                    {
                        WireguardHelpers.StatusWireguard(InterfaceStatus.active, @interface.Name);
                        logger.LogInformation($"interface {@interface.Name} Oned");
                    }

                    logger.LogInformation("on interfaces ended");
                }
                catch (NpgsqlException ex)
                {
                    logger.LogError("An error occurred while migrating the PostgreSQL database : {ex}", ex);

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