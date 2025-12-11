using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace Cs2_MoneyPlugin;

public class API
{
    private readonly string GiveMoneyEndpoint;
    private readonly string GetBalanceEndpoint;
    private readonly string GetPlayerStatsEndpoint;
    private readonly string TransferMoneyEndpoint;
    private readonly string ResetMoneyEndpoint;
    private readonly string _APItoken;

    public API(string GiveMoneyEndp,
               string GetBalanceEndp,
               string GetPlayerStatsEndp,
               string TransferMoneyEndp,
               string ResetMoneyEndp,
               string apiToken)
    {
        GiveMoneyEndpoint = GiveMoneyEndp;
        GetBalanceEndpoint = GetBalanceEndp;
        GetPlayerStatsEndpoint = GetPlayerStatsEndp;
        TransferMoneyEndpoint = TransferMoneyEndp;
        ResetMoneyEndpoint = ResetMoneyEndp;
        _APItoken = apiToken;
    }

    public async Task<int?> GetPlayerBalance(string steamId)
    {
        HttpResponseMessage? response = await API_GetMethod(GetBalanceEndpoint, steamId);

        if (response != null && response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            string? bal = await response.Content.ReadAsStringAsync();

            if (bal is not null && bal.Length > 0)
            {
                if (int.TryParse(bal, out int balance) && balance >= 0)
                {
                    return balance;
                }
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[MoneyPlugin] Error in getting player balance " + response?.StatusCode);
            Console.ResetColor();
        }
        return null;
    }

    public async Task<PlayerStats?> GetPlayerStats(CCSPlayerController player)
    {
        string steamid = player.SteamID.ToString();
        return await GetPlayerStats(steamid);
    }

    public async Task<PlayerStats?> GetPlayerStats(string steamid)
    {
        HttpResponseMessage? response = await API_GetMethod(GetPlayerStatsEndpoint, steamid);

        if (response != null && response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            PlayerStatsAPI? stats = await response.Content.ReadFromJsonAsync<PlayerStatsAPI>();

            if (stats != null)
            {
                PlayerStats pStats = new PlayerStats()
                {
                    Today = stats.Today,
                    Top = stats.Top,
                };
                return pStats;
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[MoneyPlugin] Error in getting player stats " + response?.StatusCode);
            Console.ResetColor();
        }

        return null;
    }


    public async Task<bool> ModifyPlayerMoney(string steamid, int playerMoney, bool fromAdmin)
    {
        var playerMoneyUpdates = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object> { { "admin", fromAdmin } },
            new Dictionary<string, object> { { "steamid64", steamid }, { "amount", playerMoney } }
        };

        string jsonContent = JsonConvert.SerializeObject(playerMoneyUpdates);

        var response = await API_PostMethod(GiveMoneyEndpoint, jsonContent);

        if (response == null || response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[MoneyPlugin] Error in adding player balance " + response?.StatusCode);
            Console.ResetColor();
            return false;
        }


        return response.StatusCode == System.Net.HttpStatusCode.OK;
    }

    public async Task<bool> ModifyPlayerMoneyTrans(string steamid, int moneyAmount)
    {
        var playerMoneyUpdates = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object> { { "steamid64", steamid }, { "amount", moneyAmount } }
        };

        var jsonContent = JsonConvert.SerializeObject(playerMoneyUpdates);

        var response = await API_PostMethod(TransferMoneyEndpoint, jsonContent);

        if (response == null || response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[MoneyPlugin] Error in adding player balance: " + response?.StatusCode);
            Console.ResetColor();
            return false;
        }

        return response.StatusCode == System.Net.HttpStatusCode.OK;
    }

    public async Task<bool> ModifyPlayerMoneyTrans(string steamid1, int playerMoney1, string steamid2, int playerMoney2)
    {
        var playerMoneyUpdates = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object> { { "steamid64", steamid1 }, { "amount", playerMoney1 } },
            new Dictionary<string, object> { { "steamid64", steamid2 }, { "amount", playerMoney2 } }
        };

        var jsonContent = JsonConvert.SerializeObject(playerMoneyUpdates);

        var response = await API_PostMethod(TransferMoneyEndpoint, jsonContent);

        if (response == null || response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[MoneyPlugin] Error in adding player balance (not enough kebabs, or error) ");
            Console.ResetColor();
            return false;
        }

        return response.StatusCode == System.Net.HttpStatusCode.OK;
    }

    public async Task<bool> ModifyPlayerMoneyTrans(Dictionary<string, int> playersToPay)
    {
        var playerMoneyUpdates = new List<Dictionary<string, object>>();

        foreach (var pair in playersToPay)
        {
            var update = new Dictionary<string, object>
            {
                { "steamid64", pair.Key },
                { "amount", pair.Value }
            };
            playerMoneyUpdates.Add(update);
        }

        var jsonContent = JsonConvert.SerializeObject(playerMoneyUpdates);

        var response = await API_PostMethod(GiveMoneyEndpoint, jsonContent);

        if (response == null || response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[MoneyPlugin] Error in adding player balance " + response?.StatusCode);
            Console.ResetColor();
            return false;
        }

        return response.StatusCode == System.Net.HttpStatusCode.OK;
    }

    public async Task<bool> ResetPlayerMoney(string steamid)
    {
        var playerMoneyUpdates = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object> { { "steamid64", steamid } }
        };

        var jsonContent = JsonConvert.SerializeObject(playerMoneyUpdates);

        var response = await API_PostMethod(ResetMoneyEndpoint, jsonContent);

        if (response == null || response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[MoneyPlugin] Error in reseting player money: " + response?.StatusCode);
            Console.ResetColor();
            return false;
        }

        return response.StatusCode == System.Net.HttpStatusCode.OK;
    }

    // INTERNAL METHODS

    private async Task<HttpResponseMessage?> API_GetMethod(string endpoint, string steamid)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _APItoken);

        string endpointUrl = $"{endpoint}?steamid64={steamid}";
        HttpResponseMessage? response = null;
        try
        {
            response = await httpClient.GetAsync(endpointUrl);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[MoneyPlugin] Error in GET: " + ex.Message);
            Console.ResetColor();
        }
        return response;
    }

    private async Task<HttpResponseMessage?> API_PostMethod(string endpoint, string jsonContent)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _APItoken);

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage? response = null;
            try
            {
                response = await httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[MoneyPlugin] Error in POST: " + ex.Message);
                Console.ResetColor();
            }
            return response;
        }
    }
}

