using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;
using Dapper;
using MySqlConnector;
using Microsoft.Extensions.Logging;
/*
dotnet add package Dapper
dotnet add package MySqlConnector
dotnet add package Microsoft.Extensions.Logging
*/
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Admin;
namespace SRStatsTracker;

public class Database
{
    public readonly ILogger<Database> _logger;
    private readonly string _connectionString;
    private readonly DbConfig _config;

    public Database(DbConfig config)
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<Database>();

        _config = config;
        _connectionString = BuildDatabaseConnectionString();
    }

    private string BuildDatabaseConnectionString()
    {
        if (string.IsNullOrWhiteSpace(_config.DatabaseHost) ||
            string.IsNullOrWhiteSpace(_config.DatabaseUser) ||
            string.IsNullOrWhiteSpace(_config.DatabasePassword) ||
            string.IsNullOrWhiteSpace(_config.DatabaseName) ||
            _config.DatabasePort == 0)
        {
            throw new InvalidOperationException("Database is not set in the configuration file");
        }

        MySqlConnectionStringBuilder builder = new()
        {
            Server = _config.DatabaseHost,
            Port = (uint)_config.DatabasePort,
            UserID = _config.DatabaseUser,
            Password = _config.DatabasePassword,
            Database = _config.DatabaseName,
            Pooling = true,
        };

        return builder.ConnectionString;
    }

    private async Task<MySqlConnection> GetOpenConnectionAsync()
    {
        try
        {
            var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while opening database connection");
            throw;
        }
    }

    public async Task TestAndCheckDataBaseTableAsync()
    {
        try
        {
            await using var connection = await GetOpenConnectionAsync();

            _logger.LogInformation("Database connection successful!");

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                string[] tables = { "SRPlayer" };
                foreach (var table in tables)
                {
                    var tableExists = await connection.QueryFirstOrDefaultAsync<string>(
                        $"SHOW TABLES LIKE @table;", new { table }, transaction: transaction) != null;

                    if (!tableExists)
                    {
                        string createTableQuery = table switch
                        {
                            "SRPlayer" => @"
                                CREATE TABLE IF NOT EXISTS SRPlayer (
                                    Id INT AUTO_INCREMENT PRIMARY KEY,
                                    PlayerName VARCHAR(255) NOT NULL,
                                    TotalPlaytime DOUBLE NOT NULL DEFAULT 0.0,
                                    TerroristPlaytime DOUBLE NOT NULL DEFAULT 0.0,
                                    CounterTerroristPlaytime DOUBLE NOT NULL DEFAULT 0.0,
                                    SpectatorPlaytime DOUBLE NOT NULL DEFAULT 0.0,
                                    AlivePlaytime DOUBLE NOT NULL DEFAULT 0.0,
                                    DeadPlaytime DOUBLE NOT NULL DEFAULT 0.0,
                                    LastPlayDate VARCHAR(255) NOT NULL,
                                    TodayPlaytime DOUBLE NOT NULL DEFAULT 0.0,
                                    LastUpdateTime BIGINT NOT NULL,
                                    PlayerId VARCHAR(255) NOT NULL
                            )",
                            _ => throw new InvalidOperationException($"Unknown table: {table}")
                        };
                        await connection.ExecuteAsync(createTableQuery, transaction: transaction);
                    }
                }
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error while checking database table");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection failed");
            throw;
        }
    }
    public async Task<SRPlayer?> LoadJogadorAsync(CCSPlayerController player)
    {
        SRPlayer? jogador = new SRPlayer(player);
        string SteamID = player.SteamID.ToString();
        try
        {
            using var connection = await GetOpenConnectionAsync();
            string query = "SELECT * FROM SRPlayer WHERE PlayerId = @Id";
            var playerData = await connection.QueryFirstOrDefaultAsync(query, new { Id = SteamID });
            if (playerData != null)
            {
                jogador.timeStats.TotalPlaytime = playerData.TotalPlaytime;
                jogador.timeStats.TerroristPlaytime = playerData.TerroristPlaytime;
                jogador.timeStats.CounterTerroristPlaytime = playerData.CounterTerroristPlaytime;
                jogador.timeStats.SpectatorPlaytime = playerData.SpectatorPlaytime;
                jogador.timeStats.AlivePlaytime = playerData.AlivePlaytime;
                jogador.timeStats.DeadPlaytime = playerData.DeadPlaytime;
                jogador.timeStats.LastPlayDate = playerData.LastPlayDate;
                jogador.timeStats.TodayPlaytime = playerData.TodayPlaytime;
                jogador.timeStats.LastUpdateTime = playerData.LastUpdateTime;
                return jogador;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while loading player from database");
            throw;
        }
    }
    public void SaveJogador(SRPlayer jogador)
    {
        double TotalPlaytime = jogador.timeStats.TotalPlaytime;
        double TerroristPlaytime = jogador.timeStats.TerroristPlaytime;
        double CounterTerroristPlaytime = jogador.timeStats.CounterTerroristPlaytime;
        double SpectatorPlaytime = jogador.timeStats.SpectatorPlaytime;
        double AlivePlaytime = jogador.timeStats.AlivePlaytime;
        double DeadPlaytime = jogador.timeStats.DeadPlaytime;
        string LastPlayDate = jogador.timeStats.LastPlayDate;
        double TodayPlaytime = jogador.timeStats.TodayPlaytime;
        long LastUpdateTime = jogador.timeStats.LastUpdateTime;

        string steamid = jogador.controller!.SteamID.ToString()!;
        string name = jogador.controller!.PlayerName!;

        try
        {
            bool exists = false;
            Server.NextFrame(async () =>
            {
                exists = await JogadorExistsAsync(steamid);
                string query;
                if (exists)
                {
                    query = @"UPDATE SRPlayer SET PlayerName = @name, TotalPlaytime = @TotalPlaytime, TerroristPlaytime = @TerroristPlaytime, CounterTerroristPlaytime = @CounterTerroristPlaytime, SpectatorPlaytime = @SpectatorPlaytime, AlivePlaytime = @AlivePlaytime, DeadPlaytime = @DeadPlaytime, LastPlayDate = @LastPlayDate, TodayPlaytime = @TodayPlaytime, LastUpdateTime = @LastUpdateTime 
                    WHERE PlayerId = @steamid";
                }
                else
                {
                    query = @"INSERT INTO SRPlayer (PlayerId, PlayerName, TotalPlaytime, TerroristPlaytime, CounterTerroristPlaytime, SpectatorPlaytime, AlivePlaytime, DeadPlaytime, LastPlayDate, TodayPlaytime, LastUpdateTime) 
                                VALUES (@steamid, @name, @TotalPlaytime, @TerroristPlaytime, @CounterTerroristPlaytime, @SpectatorPlaytime, @AlivePlaytime, @DeadPlaytime, @LastPlayDate, @TodayPlaytime, @LastUpdateTime)";
                }
                ExecuteAsync(query, new
                {
                    steamid,
                    name,
                    TotalPlaytime,
                    TerroristPlaytime,
                    CounterTerroristPlaytime,
                    SpectatorPlaytime,
                    AlivePlaytime,
                    DeadPlaytime,
                    LastPlayDate,
                    TodayPlaytime,
                    LastUpdateTime
                });
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while saving player to database");
            Server.PrintToConsole("Erro ao dar save a um jogador!");
            throw;
        }
        SRStatsTracker.playerList.Remove(jogador);
    }
    private async Task<bool> JogadorExistsAsync(string steamid)
    {
        try
        {
            using MySqlConnection connection = await GetOpenConnectionAsync();
            string query = "SELECT COUNT(1) FROM SRPlayer WHERE PlayerId = @steamid";
            var result = await connection.ExecuteScalarAsync<bool>(query, new { steamid });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking if player exists in database");
            throw;
        }
    }
    public void ExecuteAsync(string query, object? parameters)
    {
        Task.Run(async () =>
        {
            using MySqlConnection connection = await GetOpenConnectionAsync();
            await connection.ExecuteAsync(query, parameters);
        });
    }
}

public class DbConfig : BasePluginConfig
{
    public override int Version { get; set; } = 1;
    [JsonPropertyName("DatabaseHost")]
    public string DatabaseHost { get; set; } = "";
    [JsonPropertyName("DatabasePort")]
    public int DatabasePort { get; set; } = 3306;
    [JsonPropertyName("DatabaseUser")]
    public string DatabaseUser { get; set; } = "";
    [JsonPropertyName("DatabasePassword")]
    public string DatabasePassword { get; set; } = "";
    [JsonPropertyName("DatabaseName")]
    public string DatabaseName { get; set; } = "";
}
