using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
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
            if (BaseManager.CheckSteamid(steamid))
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
                    if (await API.ModifyPlayerMoney(steamid, amount, false))
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
            foreach (var p in playersToPay)
            {
                if (!BaseManager.CheckSteamid(p.Key))
                {
                    return false;
                }
            }

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
            if (!BaseManager.CheckSteamid(fromSteamid)) return false;
            if (!BaseManager.CheckSteamid(toSteamid)) return false;

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
        public async Task<bool?> TakeMoney(string steamid, int amount, bool fromAdmin = false)
        {
            if (!BaseManager.CheckSteamid(steamid)) return false;

            bool? result = false;
            if (DB != null && API == null)
            {
                result = await DB.TakePlayerMoney(steamid, amount, fromAdmin);
            }
            else if (DB == null && API != null)
            {
                result = await API.ModifyPlayerMoney(steamid, amount * -1, fromAdmin);
            }
            return result;
        }

        // Reset players money
        public void ResetMoney(string steamid)
        {
            if (!BaseManager.CheckSteamid(steamid)) return;

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
        public async Task<int?> GetPlayerBalance(CCSPlayerController player, bool print = false)
        {
            int? result = await GetPlayerBalance(player.SteamID.ToString());
            if (print)
            {
                if (result != null)
                {
                    Server.NextFrame(() =>
                    {
                        player.LocalizeAnnounce(ChatManager.Localize("PLUGIN_PREFIX"), "cmd.caller.money", MoneyBase.plCurrency, result);
                    });
                }
                else
                {
                    Server.NextFrame(() =>
                    {
                        player.LocalizeAnnounce(ChatManager.Localize("PLUGIN_PREFIX"), "cmd.caller.money.error");
                    });
                }
            }
            return result;
        }

        // Get player money
        public async Task<int?> GetPlayerBalance(string steamid)
        {
            if (!BaseManager.CheckSteamid(steamid)) return null;

            if (DB != null && API == null)
            {
                return await DB.GetPlayerBalance(steamid);
            }
            else if (DB == null && API != null)
            {
                return await API.GetPlayerBalance(steamid);
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

            // API checks are in backend
        }

        public async Task GetPlayerFeedSetting(string steamid)
        {
            if (Sqlite == null) return;

            if (!await Sqlite.CheckPlayerFeedAsync(steamid))
            {
                offFeedPlayers.Add(steamid);
            }
        }

        public bool GetFeedSetting(string steamid)
        {
            return offFeedPlayers.Contains(steamid);
        }

        // Get and display payer statistics
        public async Task GetPlayerStats(CCSPlayerController? player)
        {
            if (player == null) return;

            PlayerStats? result = null;
            if (DB != null && API == null)
            {
                result = await DB.GetPlayerStats(player);
            }
            else if (DB == null && API != null)
            {
                result = await API.GetPlayerStats(player);
            }

            if (result != null)
            {
                int needed = result.Top - result.Today;
                string record = MoneyBase.Localize("stats.needed.for.record", needed);
                if (needed <= 0)
                {
                    record = MoneyBase.Localize("stats.new.record");
                }
                string statsStr = $"<font color='orange'>~~~~~~~ <font color='lime'>{MoneyBase.Localize("stats.title")}</font> ~~~~~~~</font><br>" +
                                  $"{MoneyBase.Localize("stats.top")}: <font color='orange'>{result.Top}</font><br>" +
                                  $"{MoneyBase.Localize("stats.today")}: <font color='orange'>{result.Today}</font><br><br>" +
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
                Server.NextFrame(() =>
                {
                    player.LocalizeChatAnnounce(plPrefix, "cmd.caller.error");
                });
            }
        }

        public async Task<PlayerStats?> GetPlayerStats(string steamid)
        {
            if (!BaseManager.CheckSteamid(steamid)) return null;

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
