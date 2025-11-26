using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cs2_MoneyPlugin
{
    partial class MoneyBase
    {
        // Checking connetion to DB/API
        // for low amounts
        public async Task<bool> AddMoneyAsync(string steamid, int amount)
        {
            if (steamid.Length == 17)
            {
                if (DB != null && API == null)
                {
                    if (await DB.AddPlayerMoney(steamid, amount))
                    {
                        return true;
                    }
                }
                else if (DB == null && API != null)
                {
                    if (await API.ModifyPlayerMoney(steamid, amount))
                    {
                        return true;
                    }
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[MoneyPlugin] Invalid steamid (steamid64 must be 17 char long)");
                Console.ResetColor();
            }
            return false;
        }

        // Adding money for each payer in dictionary without transactions
        public async Task<bool> AddMoneyAsync(Dictionary<string, int> playersToPay)
        {
            bool result = false;
            if (DB != null && API == null)
            {
                foreach (var pair in playersToPay)
                {
                    result = await DB.AddPlayerMoney(pair.Key, pair.Value);
                }
            }
            else if (DB == null && API != null)
            {
                result = await API.ModifyPlayerMoneyTrans(playersToPay);
            }
            return result;
        }

        // Transfering money from playerA to playerB with specific take/give amounts
        public async Task<bool> TransferMoney(string fromSteamid, int takeAmount, string toSteamid, int giveAmount)
        {
            bool result = false;
            if (DB != null && API == null)
            {
                result = await DB.TransferMoney(fromSteamid, takeAmount, toSteamid, giveAmount);
            }
            else if (DB == null && API != null)
            {
                result = await API.ModifyPlayerMoneyTrans(fromSteamid, takeAmount * -1, toSteamid, giveAmount);
            }
            return result;
        }

        // Take players money
        public async Task<bool?> TakeMoney(string steamid, int amount)
        {
            bool? result = false;
            if (DB != null && API == null)
            {
                result = await DB.TakePlayerMoney(steamid, amount);
            }
            else if (DB == null && API != null)
            {
                result = await API.ModifyPlayerMoney(steamid, amount * -1);
            }
            return result;
        }

        // Reset players money
        public void ResetMoney(string steamid)
        {
            // Checking connetion to DB/API
            if (DB != null && API == null)
            {
                _ = DB.ResetPlayerMoney(steamid);
            }
            else if (DB == null && API != null)
            {
                _ = API.ResetPlayerMoney(steamid);
            }
        }

        // Checking connection and cache single players balance
        public async Task GetPlayerBalance(CCSPlayerController player)
        {
            if (DB != null && API == null)
            {
                await DB.GetPlayerBalance(player.SteamID.ToString());
            }
            else if (DB == null && API != null)
            {
                await API.API_GetPlayerBalance(player);
            }
        }

        // Get player money
        public async Task<int?> GetPlayerBalanceNoPrint(string steamid)
        {
            if (DB != null && API == null)
            {
                return await DB.GetPlayerBalanceAsync(steamid);
            }
            else if (DB == null && API != null)
            {
                return await API.API_GetPlayerBalance(steamid);
            }
            return null;
        }

        // Checking if payer exists and if not - creates new with default values
        public async Task CreateDefaultIfNotExist(string playerSteamid)
        {
            if (DB != null && API == null)
            {
                await DB.CreatePlayerFieldIfNotExist(playerSteamid);
            }

            // API check are in backend
        }

        // Get and display payer statistics
        public async Task GetPlayerStats(CCSPlayerController? player)
        {
            if (player == null) return;

            if (DB != null && API == null)
            {
                await DB.GetPlayerStats(player);
            }
            else if (DB == null && API != null)
            {
                await API.GetPlayerStats(player);
            }
        }

        public async Task<PlayerStatsAPI?> GetPlayerStats(string steamid)
        {
            if (DB != null && API == null)
            {
                return await DB.GetPlayerStats(steamid);
            }
            else if (DB == null && API != null)
            {
                return await API.GetPlayerStats(steamid);
            }
            return null;
        }
    }
}
