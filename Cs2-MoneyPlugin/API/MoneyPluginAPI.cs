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

        PlayerStatsAPI? moneyStats = await MoneyBase.instance.GetPlayerStats(steamId);
        if (moneyStats == null) return null;

        PlayerStatistics? pStats = new PlayerStatistics();

        return pStats;
    }

    public async Task<int?> GetPlayerBalance(string steamId)
    {
        if (MoneyBase.instance == null) return null;

        int? balance = await MoneyBase.instance.GetPlayerBalanceNoPrint(steamId);
        return balance;
    }

    public void GivePlayerMoney(string steamId, int money)
    {
        if (MoneyBase.API == null) return;

        _ = MoneyBase.instance?.AddMoneyAsync(steamId, money);
    }

    // ------------------------ JAILBREAK -----------------------------------

    public void AwardLRWinnerPlayer(string steamId, int money)
    {
        AwardPlayer(steamId, money, "event.response.lr.win");
    }

    public void AwardLRLoserPlayer(string steamId, int money)
    {
        AwardPlayer(steamId, money, "event.response.lr.lose");
    }


    // Handle

    public void AwardPlayer(string steamId, int money, string localizerName)
    {
        // Check basics
        if (MoneyBase.API == null) return;
        if (MoneyBase.instance == null) return;
        if (MoneyBase.instance.gameEvents == null) return;

        // check if valid payer
        bool success = ulong.TryParse(steamId, out ulong steamid64);
        if (!success || steamId.Length != 17) return;

        // Check if money can be given
        if (!MoneyBase.instance.gameEvents.isActiveRoundForMoney) return;

        var player = Utilities.GetPlayerFromSteamId(steamid64);
        if (player == null) return;
        int finalMoney = money;

        Console.WriteLine($"MoneyPlugin trying to add money LR");

        finalMoney = MoneyBase.instance.gameEvents.GetVipMoney(
                    player.IsVip(),
                    player,
                    localizerName,
                    money
                );

        _ = MoneyBase.instance.AddMoneyAsync(steamId, finalMoney);
    }

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
                    string prfx = prefix ?? MoneyBase.PL_PREFIX;
                    string msg = messageToPlayer ?? ChatManager.Localize("cmd.target.announce.addmoney", finalMoney);
                    player.PrintToChat(prfx + msg);

                    Server.PrintToConsole($"[MoneyPlugin] Awarded {finalMoney} to {(player.IsVip() ? "VIP" : "Non-VIP")} player: {player.PlayerName}");
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
