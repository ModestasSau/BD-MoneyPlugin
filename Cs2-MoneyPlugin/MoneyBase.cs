using APIMoneyPlugin;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Dapper;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static CounterStrikeSharp.API.Core.Listeners;
using static Cs2_MoneyPlugin.MoneyBase;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Cs2_MoneyPlugin;

[MinimumApiVersion(346)]
public partial class MoneyBase : BasePlugin, IPluginConfig<BaseConfig>
{
    // MAIN CLASS ---------------------------------------------------------------------------------
    public override string ModuleName => "CS2-MoneyPlugin";
    public override string ModuleAuthor => "ModestasSau";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleDescription => "Virtual currency plugin. In game rewards and etc.";
    public static int ConfigVersion => 6;

    // --------------------------------------------------------------------------------------------

    internal string dbConnectionString = string.Empty;
    internal static MoneyPluginAPI? SharedAPI { get; set; }

    public DBMySql? DB;
    public static API? API;
    public DBSQLite? Sqlite;
    public static MoneyBase? instance { get; private set; }
    public static string plPrefix = $" {ChatColors.Green}[Credits]: {ChatColors.Default}";
    public static string plCurrency = "coins";
    public static bool DEBUG = false;

    public HashSet<string> offFeedPlayers = new HashSet<string>();
    public HashSet<string> onlinePlayers = new();
    public GameEvents? gameEvents;
    public HTMLPrinter? htmlPrinter;
    public AdminCommands? adminCommands;
    public PlayerCommands? playerCommands;


    public override void Load(bool hotReload)
    {
        plPrefix = ChatManager.Localize("PLUGIN_PREFIX");
        plCurrency = ChatManager.Localize("PLUGIN_CURRENCY_NAME");
        instance = this;
        htmlPrinter = new(instance);
        gameEvents = new(instance);
        adminCommands = new(instance);
        playerCommands = new(instance);
        SharedAPI = new MoneyPluginAPI();
        Capabilities.RegisterPluginCapability(IMoneyPlugin.PluginCapability, () => SharedAPI);

        DEBUG = Config.Debug;
    }

    public void StartupCheckDatabaseOrApi(BaseConfig config)
    {
        if (config.DBConfig.dbhost.Length > 1 && config.DBConfig.dbname.Length > 1 && config.DBConfig.dbuser.Length > 1)
        {
            MySqlConnectionStringBuilder builder = new()
            {
                Server = config.DBConfig.dbhost,
                Database = config.DBConfig.dbname,
                UserID = config.DBConfig.dbuser,
                Password = config.DBConfig.dbpassword,
                Port = (uint)config.DBConfig.dbport,
                Pooling = false,
                MinimumPoolSize = 0,
                MaximumPoolSize = 640,
                ConnectionReset = false
            };

            dbConnectionString = builder.ConnectionString;
            DB = new(dbConnectionString, Config);

            try
            {
                using MySqlConnection connection = DB.GetConnection();

                try
                {
                    string CreateMoneyTableSql = string.Format(SQL_CreateMoneyTable, Config.DBConfig.moneyTableName);
                    string createFlipLogTableSql = string.Format(SQL_CreateTransferLogTable, Config.DBConfig.TransferLogTable);
                    string createPlayerStatisticsTableSql = string.Format(SQL_CreatePlayerStatisticsTable, Config.DBConfig.playerStatistics);

                    connection.Query(CreateMoneyTableSql);
                    connection.Query(createFlipLogTableSql);
                    connection.Query(createPlayerStatisticsTableSql);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[MoneyPlugin] Success! Using MySQL Database!");
                    Console.ResetColor();
                }
                catch (Exception)
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException("[MoneyPlugin] Failed to establish a database connection.", ex);
            }
        }
        else
        {
            DB = null;

            if (config.ApiConfig.GiveMoneyEndpoint.Length > 1 &&
                config.ApiConfig.GetBalanceEndpoint.Length > 1 &&
                config.ApiConfig.GetPlayerStatsEndpoint.Length > 1 &&
                config.ApiConfig.TransferMoneyEndpoint.Length > 1 &&
                config.ApiConfig.ResetMoneyEndpoint.Length > 1 &&
                config.ApiConfig.SecurityToken.Length > 1 &&
                DB == null)
            {
                API = new(config.ApiConfig.GiveMoneyEndpoint,
                          config.ApiConfig.GetBalanceEndpoint,
                          config.ApiConfig.GetPlayerStatsEndpoint,
                          config.ApiConfig.TransferMoneyEndpoint,
                          config.ApiConfig.ResetMoneyEndpoint,
                          config.ApiConfig.SecurityToken);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[MoneyPlugin] Success! Using API Endpoint and security token!");
                Console.ResetColor();
            }
        }
        string path = Path.Combine(ModulePath, "../players_settings.db");
        Sqlite = new(path);

        Task.Run(async () =>
        {
            try
            {
                await Sqlite.EnsurePlayersTableExistsAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[MoneyPlugin] Ensuring players SQLite table exists..");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Sqlite = null;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                throw;
            }
        });

        if (DB == null && API == null)
        {
            API = null;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[MoneyPlugin] You need to setup Database credentials OR API Endpoints and SecurityToken in configuration file!");
            Console.ResetColor();
            throw new Exception("Setup database credentials OR API endpoints");
        }
    }

    public static String Localize(string name, params Object[] args)
    {
        if (instance == null)
        {
            Console.WriteLine("Error: Instance not found! [MoneyBase: Localize()]");
            return "";
        }
        return String.Format(instance.Localizer[name], args);
    }
}
