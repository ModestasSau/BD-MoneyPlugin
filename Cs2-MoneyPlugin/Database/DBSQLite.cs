using CounterStrikeSharp.API;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Localization;
using MySqlConnector;
using System;
using System.Threading.Tasks;

namespace Cs2_MoneyPlugin
{
    public class DBSQLite
    {
        private readonly string _connectionString;

        public DBSQLite(string filepath)
        {
            _connectionString = $"Data Source={filepath};";
            SQLitePCL.Batteries.Init();
        }

        public async Task<SqliteConnection> GetConnectionAsync()
        {
            try
            {
                var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                return connection;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw new DatabaseException("[MoneyPlugin] Failed to establish a database connection.", ex);
            }
        }

        public async Task EnsurePlayersTableExistsAsync()
        {
            using (var connection = await GetConnectionAsync())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT name FROM sqlite_master WHERE type='table' AND name='Players'";
                    var tableName = await command.ExecuteScalarAsync() as string;

                    if (string.IsNullOrEmpty(tableName))
                    {
                        // table doesnt exist - create new
                        await CreatePlayersTableAsync(connection);
                    }
                }
            }
        }

        private async Task CreatePlayersTableAsync(SqliteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"CREATE TABLE Players (SteamId TEXT PRIMARY KEY, toggleMoneyFeed INTEGER NOT NULL)";
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<bool> CheckPlayerGambleAsync(string steamid)
        {
            using (var connection = await GetConnectionAsync())
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Players WHERE SteamId = @steamId";
                    command.Parameters.AddWithValue("@steamId", steamid);
                    var result = await command.ExecuteScalarAsync();
                    bool playerExists = Convert.ToInt32(result) > 0;

                    if (!playerExists)
                    {
                        await InsertDefaultPlayerAsync(connection, steamid);
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT toggleMoneyFeed FROM Players WHERE SteamId = @steamid";
                    command.Parameters.AddWithValue("@steamid", steamid);
                    var result = await command.ExecuteScalarAsync();

                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result) != 0;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }


        private async Task InsertDefaultPlayerAsync(SqliteConnection connection, string steamid)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "INSERT INTO Players (SteamId, toggleMoneyFeed) VALUES (@steamId, @ToggleMoneyFeed)";
                command.Parameters.AddWithValue("@steamId", steamid);
                command.Parameters.AddWithValue("@ToggleMoneyFeed", 1);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task TogglePlayerMoneyChatFeed(string steamid, bool enable)
        {
            using (var connection = await GetConnectionAsync())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Players SET toggleMoneyFeed = @ToggleMoneyFeed WHERE SteamId = @steamId";
                    command.Parameters.AddWithValue("@steamId", steamid);
                    command.Parameters.AddWithValue("@ToggleMoneyFeed", enable ? 1 : 0);
                    var response = await command.ExecuteNonQueryAsync();

                    var player = Utilities.GetPlayerFromSteamId(ulong.Parse(steamid));
                    if (response > 0)
                    {
                        if (enable)
                        {
                            Server.NextFrame(() =>
                            {
                                player.LocalizeChatAnnounce(MoneyBase.Localize("PLUGIN_PREFIX"), "settings.moneyfeed.enabled.response");
                            });
                        }
                        else
                        {
                            Server.NextFrame(() =>
                            {
                                player.LocalizeChatAnnounce(MoneyBase.Localize("PLUGIN_PREFIX"), "settings.moneyfeed.disabled.response");
                            });
                        }
                    }
                }
            }
        }

    }
}
