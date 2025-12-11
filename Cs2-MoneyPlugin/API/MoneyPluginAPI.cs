using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APIMoneyPlugin;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Cs2_MoneyPlugin;

public class MoneyPluginAPI : IMoneyPlugin
{
    public async Task<PlayerStatistics?> GetPlayerStats(string steamId)
    {
        if (MoneyBase.instance == null) return null;

        PlayerStats? moneyStats = await MoneyBase.instance.GetPlayerStats(steamId);
        if (moneyStats == null) return null;

        PlayerStatistics? pStats = new PlayerStatistics()
        {
            Steamid64 = steamId,
            Today = moneyStats.Today,
            Top = moneyStats.Top
        };

        return pStats;
    }

    public async Task<int?> GetPlayerBalance(string steamId)
    {
        if (MoneyBase.instance == null) return null;

        int? balance = await MoneyBase.instance.GetPlayerBalance(steamId);
        return balance;
    }

    public void GivePlayerMoney(string steamId, int money)
    {
        if (MoneyBase.API == null) return;

        _ = MoneyBase.instance?.AddMoneyAsync(steamId, money);
    }

    // Handle

    // Awards payer
    // NOTE: ONLY IF it is allowed round for awarding (Config set MIN players required for plugin to work)
    public async Task<bool> AwardPlayerMoney(string steamId, int money, string? prefix = null, string? messageToPlayer = null)
    {
        if (MoneyBase.API == null) return false;
        if (MoneyBase.instance == null) return false;

        try
        {
            if (MoneyBase.instance.gameEvents == null) return false;
            bool success = ulong.TryParse(steamId, out ulong steamid64);
            if (!success || steamId.Length != 17) return false;
            if (!MoneyBase.instance.gameEvents.isActiveRoundForMoney) return false;

            int finalMoney = money;
            CCSPlayerController? player = null;

            Server.NextFrame(() =>
            {
                player = Utilities.GetPlayerFromSteamId(steamid64);
            });

            if (player != null)
            {
                Server.NextFrame(() =>
                {
                    finalMoney = MoneyBase.instance.gameEvents.GetVipMoney(
                    player.IsVip(),
                    player,
                    "cmd.target.announce.addmoney",
                    money,
                    prefix
                );
                });
            }

            bool isMoneyAdded = await MoneyBase.instance.AddMoneyAsync(steamId, finalMoney);

            Server.NextFrame(() =>
            {
                if (isMoneyAdded && player != null)
                {
                    string prfx = prefix ?? MoneyBase.plPrefix;
                    string msg = messageToPlayer ?? ChatManager.Localize("cmd.target.announce.addmoney", finalMoney, MoneyBase.plCurrency);
                    player.PrintToChat(prfx + msg);

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[MoneyPlugin] Awarded {finalMoney} to {(player.IsVip() ? "VIP" : "Non-VIP")} player: {player.PlayerName}");
                    Console.ResetColor();
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MoneyPlugin] Error: {ex}");
        }

        return false;
    }
}
