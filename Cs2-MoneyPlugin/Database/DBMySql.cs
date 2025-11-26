using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Dapper;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Transactions;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Cs2_MoneyPlugin;

public class DBMySql
{
    private readonly string _dbConnectionString;
    private readonly MoneyBase.BaseConfig Config;


    public DBMySql(string dbConnectionString, MoneyBase.BaseConfig config)
    {
        _dbConnectionString = dbConnectionString;
        Config = config;
    }

    public async Task<MySqlConnection> GetConnectionAsync()
    {
        try
        {
            var connection = new MySqlConnection(_dbConnectionString);
            await connection.OpenAsync();
            return connection;
        }
        catch (Exception ex)
        {
            throw new DatabaseException("[MoneyPlugin] Failed to establish a database connection.", ex);
        }
    }

    public async Task CreatePlayerFieldIfNotExist(string steamId)
    {
        try
        {
            using (var connection = await GetConnectionAsync())
            {
                var playerExistsInMoneyTableQuery = $"SELECT COUNT(*) FROM `{Config.DBConfig.moneyTableName}` WHERE steamid = @SteamID";
                var playerExistsInStatisticsTableQuery = $"SELECT COUNT(*) FROM `{Config.DBConfig.playerStatistics}` WHERE steamid = @SteamID";

                var playerCountInMoney = await connection.ExecuteScalarAsync<int>(playerExistsInMoneyTableQuery, new { SteamID = steamId });
                var playerCountInStatistics = await connection.ExecuteScalarAsync<int>(playerExistsInStatisticsTableQuery, new { SteamID = steamId });

                if (playerCountInMoney == 0)
                {
                    var insertMoneyQuery = $"INSERT INTO `{Config.DBConfig.moneyTableName}` (steamid, balance) VALUES (@SteamID, @Balance)";
                    await connection.ExecuteAsync(insertMoneyQuery, new { SteamID = steamId, Balance = 0 });
                }

                if (playerCountInStatistics == 0)
                {
                    var insertStatsQuery = $"INSERT INTO `{Config.DBConfig.playerStatistics}` (steamid, today) VALUES (@SteamID, @Today)";
                    await connection.ExecuteAsync(insertStatsQuery, new { SteamID = steamId, Today = 0 });
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[MoneyPlugin] An error occurred while checking if player exists and creating if not: {ex.Message}");
            Console.ResetColor();
        }
    }

    public async Task GetPlayerBalance(string steamId)
    {
        try
        {
            using (var connection = await GetConnectionAsync())
            {
                var selectQuery = $"SELECT `balance` FROM `{Config.DBConfig.moneyTableName}` WHERE `steamid` = @SteamId";
                var balance = await connection.ExecuteScalarAsync<int>(selectQuery, new { SteamId = steamId });
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[MoneyPlugin] An error occurred while retrieving balance: {ex.Message}");
            Console.ResetColor();
            throw;
        }
    }

    public async Task<int?> GetPlayerBalanceAsync(string steamId)
    {
        try
        {
            using (var connection = await GetConnectionAsync())
            {
                var selectQuery = $"SELECT `balance` FROM `{Config.DBConfig.moneyTableName}` WHERE `steamid` = @SteamId";
                var balance = await connection.ExecuteScalarAsync<int?>(selectQuery, new { SteamId = steamId });

                if (balance.HasValue)
                {
                    if (balance.Value >= 0)
                    {
                        return balance.Value;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[MoneyPlugin] An error occurred while retrieving balance: {ex.Message}");
            Console.ResetColor();
            throw;
        }
        return null;
    }


    public async Task<bool> AddPlayerMoney(string steamId, int amount)
    {
        try
        {
            using (var connection = await GetConnectionAsync())
            {
                var updateQuery = $"UPDATE `{Config.DBConfig.moneyTableName}` SET `balance` = `balance` + @Amount WHERE steamid = @SteamID";
                await connection.ExecuteAsync(updateQuery, new { Amount = amount, SteamID = steamId });
                DateTime timestamp = DateTime.UtcNow;
                await ModifyPlayerStatistics(timestamp, steamId, amount);
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[MoneyPlugin] An error occurred while adding money: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    public async Task<bool?> TakePlayerMoney(string steamId, int amount)
    {
        MySqlTransaction? transaction = null;
        try
        {
            using (var connection = await GetConnectionAsync())
            {
                using (transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var selectQuery = $"SELECT balance FROM `{Config.DBConfig.moneyTableName}` WHERE steamid = @SteamID FOR UPDATE";
                        var currentBalance = await connection.ExecuteScalarAsync<int>(selectQuery, new { SteamID = steamId }, transaction);

                        if (currentBalance >= amount)
                        {
                            var updateQuery = $"UPDATE `{Config.DBConfig.moneyTableName}` SET `balance` = `balance` - @Amount WHERE steamid = @SteamID";
                            await connection.ExecuteAsync(updateQuery, new { Amount = amount, SteamID = steamId }, transaction);

                            transaction?.Commit();
                            return true;
                        }
                        else
                        {
                            transaction?.Rollback();
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[MoneyPlugin] Player {steamId} doesn't have enough money to take away {amount}.");
                            Console.ResetColor();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[MoneyPlugin] An error occurred while taking money: {ex.Message}");
                        Console.ResetColor();
                        return null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[MoneyPlugin] An error occurred while taking money: {ex.Message}");
            Console.ResetColor();
            return null;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    public async Task<bool> TransferMoney(string payerSteamid, int takeAmount, string receiverSteamid, int giveAmount)
    {
        if (takeAmount <= 0 || giveAmount <= 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[MoneyPlugin] TransferMoney called with incorrect amount");
            Console.ResetColor();
            return false;
        }

        MySqlTransaction? transaction = null;

        try
        {
            using (var connection = await GetConnectionAsync())
            {
                transaction = await connection.BeginTransactionAsync();

                string table = Config.DBConfig.moneyTableName;

                var selectSenderQuery = $"SELECT balance FROM `{table}` WHERE steamid = @SteamID FOR UPDATE";

                var senderBalance = await connection.ExecuteScalarAsync<int?>(selectSenderQuery, new { SteamID = payerSteamid }, transaction);

                if (senderBalance == null)
                {
                    await transaction.RollbackAsync();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[MoneyPlugin:TransferMoney] sender {payerSteamid} not found.");
                    Console.ResetColor();
                    return false;
                }

                if (senderBalance.Value < takeAmount)
                {
                    await transaction.RollbackAsync();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[MoneyPlugin:TransferMoney] Player {payerSteamid} doesnt have enough money to transfer {takeAmount}.");
                    Console.ResetColor();
                    return false;
                }

                var selectReceiverQuery = $"SELECT balance FROM `{table}` WHERE steamid = @SteamID FOR UPDATE";

                await connection.ExecuteScalarAsync<int?>(selectReceiverQuery, new { SteamID = receiverSteamid }, transaction);

                var updateSenderQuery = $"UPDATE `{table}` SET balance = balance - @Amount WHERE steamid = @SteamID";
                var updateReceiverQuery = $"UPDATE `{table}` SET balance = balance + @Amount WHERE steamid = @SteamID";

                await connection.ExecuteAsync(updateSenderQuery, new { Amount = takeAmount, SteamID = payerSteamid }, transaction);
                await connection.ExecuteAsync(updateReceiverQuery, new { Amount = giveAmount, SteamID = receiverSteamid }, transaction);

                await transaction.CommitAsync();

                // transfer log
                DateTime timestamp = DateTime.UtcNow;
                await SaveTransferLog(timestamp.Date, payerSteamid, takeAmount, receiverSteamid, giveAmount);
                return true;
            }
        }
        catch (Exception ex)
        {
            try
            {
                if (transaction != null)
                    await transaction.RollbackAsync();
            }
            catch { }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[MoneyPlugin:TransferMoney] An error occurred while transferring money: {ex.Message}");
            Console.ResetColor();
            return false;
        }
        finally
        {
            transaction?.Dispose();
        }
    }


    public async Task ResetPlayerMoney(string steamId)
    {
        MySqlTransaction? transaction = null;
        try
        {
            using (var connection = await GetConnectionAsync())
            {
                await connection.OpenAsync();

                // Begin a transaction
                using (transaction = connection.BeginTransaction())
                {
                    var updateQuery = $"UPDATE `{Config.DBConfig.moneyTableName}` SET `balance` = 0 WHERE steamid = @SteamID LIMIT 1";
                    await connection.ExecuteAsync(updateQuery, new { SteamID = steamId }, transaction);
                    transaction.Commit();
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[MoneyPlugin] An error occurred while reseting money: {ex.Message}");
            Console.ResetColor();
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    // save the transfer log
    public async Task SaveTransferLog(DateTime timestamp, string payerSteamid, int payAmount, string receiverSteamid, int reveiveAmount)
    {
        try
        {
            using (var connection = await GetConnectionAsync())
            {
                var insertQuery = @"
                INSERT INTO `{0}` 
                    (
                        date, payer, payer_amount, receiver, receiver_amount
                    )
                VALUES 
                    (
                        @Timestamp, @Payer, @PayAmount, @Receiver, @ReceiveAmount
                    );";

                var formattedQuery = string.Format(insertQuery, Config.DBConfig.TransferLogTable);
                await connection.ExecuteAsync(formattedQuery, new
                {
                    Timestamp = timestamp,
                    Payer = payerSteamid,
                    PayAmount = payAmount,
                    Receiver = receiverSteamid,
                    ReceiveAmount = reveiveAmount,
                });
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[MoneyPlugin] An error occurred while saving transfer log: {ex}");
            Console.ResetColor();
        }
    }

    public async Task ModifyPlayerStatistics(DateTime timestamp, string steamid, int amount)
    {
        try
        {
            using (var connection = await GetConnectionAsync())
            {
                var updateQuery = @"
                    UPDATE `{0}` SET 
                        date = @Date,
                        total_today = 
                        CASE
                            WHEN date = @Date THEN total_today + @Amount
                            ELSE @Amount
                        END,
                        top_per_day = 
                        CASE
                            WHEN date = @Date
                                THEN GREATEST(top_per_day, total_today + @Amount)
                            ELSE
                                GREATEST(top_per_day, total_today)
                        END
                    WHERE steamid = @SteamID;";

                var formattedQuery = string.Format(updateQuery, Config.DBConfig.playerStatistics);
                await connection.ExecuteAsync(formattedQuery, new
                {
                    SteamID = steamid,
                    Date = timestamp.Date,
                    Amount = amount,
                });
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[MoneyPlugin] An error occurred while modifying player statistics: {ex.Message}");
            Console.ResetColor();
        }
    }

    public async Task GetPlayerStats(CCSPlayerController player)
    {
        try
        {
            if (MoneyBase.instance == null) return;

            using (var connection = await GetConnectionAsync())
            {
                var selectQuery = @"
                    SELECT 
                        total_today AS Today, 
                        top_per_day AS Top,
                    FROM `{0}`
                    WHERE steamid = @SteamID;";


                var formattedQuery = string.Format(selectQuery, Config.DBConfig.playerStatistics);
                var playerStats = await connection.QueryFirstOrDefaultAsync<PlayerStats>(formattedQuery, new { SteamID = player.SteamID.ToString() });

                if (playerStats != null)
                {
                    int needed = playerStats.Top - playerStats.Today;
                    string record = MoneyBase.Localize("stats.needed.for.record", needed);
                    if (needed <= 0)
                    {
                        record = MoneyBase.Localize("stats.new.record");
                    }
                    string statsStr = $"<font color='orange'>~~~~~~~ <font color='lime'>{MoneyBase.Localize("stats.title")}</font> ~~~~~~~</font><br>" +
                                      $"{MoneyBase.Localize("stats.top")}: <font color='orange'>{playerStats.Top}</font><br>" +
                                      $"{MoneyBase.Localize("stats.today")}: <font color='orange'>{playerStats.Today}</font><br><br>" +
                                      $"{record}<br>" +
                                      $"{MoneyBase.Localize("stats.menu.close")}";
                    IMenuInstance? menuInstance = null;
                    Server.NextFrame(() =>
                    {
                        menuInstance = MoneyBase.instance?.htmlPrinter?.CloseMenuInstance(player, MoneyBase.Localize("cmd.menu.exit"));

                        MoneyBase.instance?.htmlPrinter?.PrintToPlayer(player, menuInstance, statsStr);
                    });
                }
                else
                {
                    Console.WriteLine($"No flip statistics found for SteamID {player.SteamID}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[MoneyPlugin] An error occurred while fetching player statistics: {ex.Message}");
            Console.ResetColor();
        }
    }

    public async Task<PlayerStatsAPI?> GetPlayerStats(string steamid)
    {
        try
        {
            using (var connection = await GetConnectionAsync())
            {
                var selectQuery = @"
                    SELECT 
                        total_today AS Today, 
                        top_per_day AS Top,
                    FROM `{0}`
                    WHERE steamid = @SteamID;";


                var formattedQuery = string.Format(selectQuery, Config.DBConfig.playerStatistics);
                var playerStats = await connection.QueryFirstOrDefaultAsync<PlayerStats>(formattedQuery, new { SteamID = steamid });

                if (playerStats != null)
                {
                    PlayerStatsAPI playerStatsAPI = new()
                    {
                        Steamid64 = steamid,
                        Top = playerStats.Top,
                        Today = playerStats.Today
                    };

                    return playerStatsAPI;
                }
                else
                {
                    Console.WriteLine($"No flip statistics found for SteamID {steamid}");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[MoneyPlugin] An error occurred while fetching player statistics: {ex.Message}");
            Console.ResetColor();
            return null;
        }
    }
}

public class DatabaseException : Exception
{
    public DatabaseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
