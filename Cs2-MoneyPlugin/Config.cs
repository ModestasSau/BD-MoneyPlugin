using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Cs2_MoneyPlugin;

public partial class MoneyBase
{
    public required BaseConfig Config { get; set; } = new();

    private static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "";
    private static readonly string CfgPath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/{AssemblyName}/{AssemblyName}.json";

    public void OnConfigParsed(BaseConfig config)
    {
        Config = config;
        UpdateConfig(config);

        StartupCheckDatabaseOrApi(Config);
        instance = this;
    }

    public static void UpdateConfig<T>(T config) where T : BasePluginConfig, new()
    {
        var newCfgVersion = new T().Version;

        if (config.Version == newCfgVersion)
            return;

        config.Version = newCfgVersion;

        var updatedJsonContent = JsonSerializer.Serialize(config,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        File.WriteAllText(CfgPath, updatedJsonContent);
    }

    public class BaseConfig : BasePluginConfig
    {
        [JsonPropertyName("MinPlayersForActivating")]
        public int MinPlayersForActivating { get; set; } = 4;

        [JsonPropertyName("CommandCooldownSeconds")]
        public int CommandCooldown { get; set; } = 2;

        [JsonPropertyName("GiveMoneyForBotKill")]
        public bool GiveMoneyForBotKill { get; set; } = false;

        [JsonPropertyName("TransferFeePercent")]
        public int TransferFeePercent { get; set; } = 10;

        [JsonPropertyName("VipKebabsMultiplier")]
        public double VipKebabsMultiplier { get; set; } = 1.5;

        [JsonPropertyName("HealthshotBuyAmountPerRound")]
        public int HealthshotBuyAmount { get; set; } = 2;

        [JsonPropertyName("HealthshotPrice")]
        public int HealthshotPrice { get; set; } = 50;

        [JsonPropertyName("DatabaseConfig")]
        public DatabaseConfig DBConfig { get; set; } = new();

        [JsonPropertyName("ApiConfig")]
        public ApiConfig ApiConfig { get; set; } = new();

        [JsonPropertyName("MoneyEvents")]
        public MoneyEvents MoneyEvents { get; set; } = new();

        public override int Version { get; set; } = ConfigVersion;
    }

    public class DatabaseConfig
    {
        [JsonPropertyName("dbuser")]
        public string dbuser { get; set; } = "";

        [JsonPropertyName("dbpassword")]
        public string dbpassword { get; set; } = "";

        [JsonPropertyName("dbhost")]
        public string dbhost { get; set; } = "";

        [JsonPropertyName("dbport")]
        public int dbport { get; set; } = 0;

        [JsonPropertyName("dbname")]
        public string dbname { get; set; } = "";

        [JsonPropertyName("money_tablename")]
        public string moneyTableName { get; set; } = "";

        [JsonPropertyName("logs_tablename")]
        public string TransferLogTable { get; set; } = "";

        [JsonPropertyName("player_statistics_tablename")]
        public string playerStatistics { get; set; } = "";
    }
    public class ApiConfig
    {
        [JsonPropertyName("API_GiveMoneyEndpoint")]
        public string GiveMoneyEndpoint { get; set; } = "";

        [JsonPropertyName("API_GetBalanceEndpoint")]
        public string GetBalanceEndpoint { get; set; } = "";

        [JsonPropertyName("API_GetPlayerStatsEndpoint")]
        public string GetPlayerStatsEndpoint { get; set; } = "";

        [JsonPropertyName("API_ResetMoneyEndpoint")]
        public string ResetMoneyEndpoint { get; set; } = "";

        [JsonPropertyName("API_TransferMoneyEndpoint")]
        public string TransferMoneyEndpoint { get; set; } = "";

        [JsonPropertyName("SecurityBearerToken")]
        public string SecurityToken { get; set; } = "";
    }

    public class MoneyEvents
    {
        [JsonPropertyName("MoneyForRoundWin")]
        public int MoneyForRoundWin { get; set; } = 3;

        [JsonPropertyName("MoneyForRoundLose")]
        public int MoneyForRoundLose { get; set; } = 2;

        [JsonPropertyName("MoneyForMVP")]
        public int MoneyForMVP { get; set; } = 2;

        [JsonPropertyName("MoneyForKill")]
        public int MoneyForKill { get; set; } = 1;

        [JsonPropertyName("MoneyForHeadshot")]
        public int MoneyForHeadshot { get; set; } = 2;

        [JsonPropertyName("MoneyForNoScope")]
        public int MoneyForNoScope { get; set; } = 2;

        [JsonPropertyName("MoneyForNoScopeHeadshot")]
        public int MoneyForNoScopeHeadshot { get; set; } = 3;

        [JsonPropertyName("MoneyForKnife")]
        public int MoneyForKnife { get; set; } = 10;

        [JsonPropertyName("MoneyForTaser")]
        public int MoneyForTaser { get; set; } = 5;

        [JsonPropertyName("MoneyForGameWin")]
        public int MoneyForGameWin { get; set; } = 100;

        [JsonPropertyName("MoneyForGameLose")]
        public int MoneyForGameLose { get; set; } = 80;

        [JsonPropertyName("MoneyForGameTie")]
        public int MoneyForGameTie { get; set; } = 80;
    }
}
