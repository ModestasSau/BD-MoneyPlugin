using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using static Cs2_MoneyPlugin.MoneyBase;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Cs2_MoneyPlugin;

public class PlayerCommands
{
    private readonly MoneyBase instance;

    private Dictionary<int, DateTime> commandCooldown = new();
    private Timer? menuTimerCancel = null;
    private List<CustomPlayerMessage> CustomPlayerMsg = new();
    private Dictionary<int, int> BoughtHealtshotPlayers = new();

    public PlayerCommands(MoneyBase instance)
    {
        this.instance = instance;
        RegisterCommands();
    }

    public List<CustomPlayerMessage> GetCustomPlayerMessage()
    {
        return CustomPlayerMsg;
    }

    public void ClearCommandCooldown()
    {
        commandCooldown.Clear();
    }

    public void ClearBoughtHealtshotPlayers()
    {
        BoughtHealtshotPlayers.Clear();
    }

    public void RegisterCommands()
    {
        instance.AddCommand("css_menu", "Opens all commands menu.", CommandsMenu);
        instance.AddCommand("css_credits", "Shows how much money you have.", CheckMoney);
        instance.AddCommand("css_settings", "Settings menu", Settings);
        instance.AddCommand("css_stats", "Show earned money per session statistics", Statistics);
        instance.AddCommand("css_transfer", "Transfer money to other player.", TransferMoney);
        instance.AddCommand("css_hs", "Buy healthshot", BuyHealthshot);
    }

    public bool AddPlayerToCooldown(int playerSlot)
    {
        if (commandCooldown.ContainsKey(playerSlot))
        {
            commandCooldown[playerSlot] = DateTime.UtcNow.AddSeconds(instance.Config.CommandCooldown);
            return true;
        }
        else
        {
            commandCooldown.Add(playerSlot, DateTime.UtcNow.AddSeconds(instance.Config.CommandCooldown));
            return true;
        }
    }
    public bool CanExecuteCommand(int playerSlot)
    {
        if (commandCooldown.ContainsKey(playerSlot))
        {
            if (DateTime.UtcNow >= commandCooldown[playerSlot])
            {
                return true;
            }
            else
            {
                int remainingCooldown = (int)(commandCooldown[playerSlot] - DateTime.UtcNow).TotalSeconds;
                Utilities.GetPlayerFromSlot(playerSlot).LocalizeChatAnnounce(MoneyBase.plPrefix, "cmd.cooldown", remainingCooldown);
                return false;
            }
        }
        return true;
    }

    // Game commands

    public void CheckMoney(CCSPlayerController? player, CommandInfo? command = null)
    {
        if (player.IsLegalNotBot() && CanExecuteCommand(player.Slot))
        {
            _ = instance.GetPlayerBalance(player, true);
            AddPlayerToCooldown(player.Slot);
        }
    }

    public void Settings(CCSPlayerController? player, CommandInfo? command = null)
    {
        if (player.IsLegalNotBot() && CanExecuteCommand(player.Slot))
        {
            _ = SettingsMenu(player);

            AddPlayerToCooldown(player.Slot);
        }

    }

    public void Statistics(CCSPlayerController? player, CommandInfo? command = null)
    {
        if (player.IsLegalNotBot() && CanExecuteCommand(player.Slot))
        {
            _ = instance.GetPlayerStats(player);

            AddPlayerToCooldown(player.Slot);
        }
    }
    public void TransferMoney(CCSPlayerController? player, CommandInfo? command = null)
    {
        if (player.IsLegalNotBot() && CanExecuteCommand(player.Slot))
        {
            CustomTransferAmount(player);

            AddPlayerToCooldown(player.Slot);
        }
    }

    private void BuyHealthshot(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player.IsLegalNotBot() && CanExecuteCommand(player.Slot))
        {
            _ = BuyHealthshot(player);
        }
    }

    public void CommandsMenu(CCSPlayerController? player, CommandInfo? command = null)
    {
        if (player.IsLegalNotBot() && CanExecuteCommand(player.Slot))
        {
            menuTimerCancel?.Kill();

            string mainMenuTitle = MoneyBase.Localize("cmd.menu");

            if (instance == null) return;

            CenterHtmlMenu mainMenu = new(mainMenuTitle, instance);

            mainMenu.AddMenuOption(MoneyBase.Localize("commands.menu.check.money"), (p, option) => CheckMoney(p));
            mainMenu.AddMenuOption(MoneyBase.Localize("commands.menu.check.money.stats"), (p, option) => Statistics(p));
            mainMenu.AddMenuOption(MoneyBase.Localize("commands.menu.transfer.money"), (p, option) => TransferMoney(p));
            mainMenu.AddMenuOption(MoneyBase.Localize("commands.menu.buy.healthshot", instance.Config.HealthshotPrice, MoneyBase.Localize("PLUGIN_CURRENCY_NAME")), async (p, option) => await BuyHealthshot(p));
            mainMenu.AddMenuOption(MoneyBase.Localize("commands.menu.settings"), (p, option) => Settings(p));

            MenuManager.OpenCenterHtmlMenu(instance, player, mainMenu);

        }
    }

    // Functions -------------------------------------------------------------------

    public async Task SettingsMenu(CCSPlayerController player)
    {
        menuTimerCancel?.Kill();

        string mainMenuTitle = MoneyBase.Localize("commands.menu.settings");

        if (instance == null) return;

        CenterHtmlMenu mainMenu = new(mainMenuTitle, instance);

        bool active = false;
        if (instance.Sqlite != null)
        {
            active = await instance.Sqlite.CheckPlayerFeedAsync(player.SteamID.ToString());
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[MoneyPlugin] SQlite doesnt give toggleMoneyFeed option from Database!");
            Console.ResetColor();
            return;
        }

        // Toggle chat feed
        if (active)
        {
            mainMenu.AddMenuOption(Localize("settings.moneyfeed.disable"), (p, option) =>
                {
                    _ = instance.Sqlite?.TogglePlayerMoneyChatFeed(player.SteamID.ToString(), false);
                    _ = SettingsMenu(player);
                });

        }
        else
        {
            mainMenu.AddMenuOption(Localize("settings.moneyfeed.enable"), (p, option) =>
            {
                _ = instance.Sqlite?.TogglePlayerMoneyChatFeed(player.SteamID.ToString(), true);
                _ = SettingsMenu(player);
            });
        }

        MenuManager.OpenCenterHtmlMenu(instance, player, mainMenu);
    }

    public void CustomTransferAmount(CCSPlayerController player)
    {
        menuTimerCancel?.Kill();
        MenuManager.CloseActiveMenu(player);

        if (instance == null || instance.htmlPrinter == null)
        {
            player.LocalizeAnnounce(plPrefix, "cmd.caller.transfer.unavailable");
            return;
        }

        ChatMenu emptyMenu = new("");
        MenuManager.OpenChatMenu(player, emptyMenu);

        IMenuInstance? menuInstance = MenuManager.GetActiveMenu(player);
        instance.htmlPrinter.PrintToPlayer(player, menuInstance, Localize("cmd.caller.transfer.amount.html"), 20);

        player.LocalizeChatAnnounce(plPrefix, "cmd.caller.transfer.amount");

        var findPlayer = CustomPlayerMsg.Find(obj => obj.Player == (int)player.Index);
        if (findPlayer == null)
        {
            CustomPlayerMsg.Add(new CustomPlayerMessage
            {
                Player = (int)player.Index,
                TransferCustomMsg = true
            });
        }
        else
        {
            findPlayer.TransferCustomMsg = true;
        }

        menuTimerCancel?.Kill();
        menuTimerCancel = instance.AddTimer(20f, () =>
        {
            if (findPlayer != null)
            {
                findPlayer.TransferCustomMsg = false;
                player.LocalizeChatAnnounce(plPrefix, "cmd.cancel");
                MenuManager.CloseActiveMenu(player);
            }
        }, TimerFlags.STOP_ON_MAPCHANGE);

        // next in onPlayerChat listener to check custom amount..
        return;
    }

    public void SelectTransferPlayer(CCSPlayerController player, int amount)
    {
        menuTimerCancel?.Kill();

        if (instance == null || instance.gameEvents == null) return;

        string Title = Localize("cmd.caller.transfer.players.menu");

        CenterHtmlMenu playersMenu = new(Title, instance);

        foreach (var p in Utilities.GetPlayers())
        {
            if (p == null) continue;
            if (!p.IsLegalNotBot()) continue;

            if (DEBUG == false)
            {
                if (p == player) continue;
                if (!instance.onlinePlayers.Contains(p.SteamID.ToString())) continue;
            }
            playersMenu.AddMenuOption(p.PlayerName, (opponent, option) => { ConfirmTransfer(player, p, amount); });
        }
        if (playersMenu.MenuOptions.Count > 0)
        {
            player.LocalizeChatAnnounce(plPrefix, "cmd.caller.transfer.amount.selected", amount, plCurrency);
            MenuManager.OpenCenterHtmlMenu(instance, player, playersMenu);
        }
        else
        {
            player.LocalizeAnnounce(plPrefix, "cmd.caller.transfer.players.online.none");
            MenuManager.CloseActiveMenu(player);
            return;
        }
    }

    private void ConfirmTransfer(CCSPlayerController player, CCSPlayerController selectedPlayer, int amount)
    {
        MenuManager.CloseActiveMenu(player);
        menuTimerCancel?.Kill();

        string htmlPrint = Localize("cmd.transfer.confirm.html", instance.Config.TransferFeePercent, amount, selectedPlayer.PlayerName) + "<br>" +
                           $"Confirm transfer: <font color='lime'>!3</font><br>" + $"Cancel: <font color='red'>!9</font>";

        if (instance == null || instance.htmlPrinter == null)
        {
            player.LocalizeAnnounce(plPrefix, "cmd.caller.transfer.unavailable");
            return;
        }

        ChatMenu confirmMenu = new("Confirm Transfer");
        confirmMenu.AddMenuOption("", (p, opt) => { });
        confirmMenu.AddMenuOption("", (p, opt) => { });
        confirmMenu.AddMenuOption("Confirm", (p, opt) => { _ = TransferMoney(player, selectedPlayer, amount); });
        confirmMenu.ExitButton = true;

        MenuManager.OpenChatMenu(player, confirmMenu);
        IMenuInstance? transferAcceptMenu = MenuManager.GetActiveMenu(player);

        instance.htmlPrinter.PrintToPlayer(player, transferAcceptMenu, htmlPrint, 20);
        menuTimerCancel?.Kill();
        menuTimerCancel = instance.AddTimer(20f, () =>
        {
            MenuManager.CloseActiveMenu(player);
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    private async Task TransferMoney(CCSPlayerController player, CCSPlayerController selectedPlayer, int amount)
    {
        MenuManager.CloseActiveMenu(player);

        int amountAfterFee = amount - (amount * instance.Config.TransferFeePercent / 100);
        var bal = await instance.GetPlayerBalance(player.SteamID.ToString());

        if (bal < amount)
        {
            if (player.IsConnected())
            {
                Server.NextWorldUpdate(() =>
                {
                    player.LocalizeAnnounce(plPrefix, "cmd.caller.transfer.money.notenough", Localize("PLUGIN_CURRENCY_NAME"));
                    return;
                });
            }
        }

        bool success = await instance.TransferMoney(player.SteamID.ToString(), amount, selectedPlayer.SteamID.ToString(), amountAfterFee);

        if (success)
        {
            if (player.IsConnected())
            {
                Server.NextWorldUpdate(() =>
                {
                    player.LocalizeAnnounce(plPrefix, "cmd.caller.transfer.successful", amountAfterFee, plCurrency, selectedPlayer.PlayerName);
                });
            }
            if (selectedPlayer.IsConnected())
            {
                Server.NextWorldUpdate(() =>
                {
                    selectedPlayer.LocalizeAnnounce(plPrefix, "cmd.target.transfer.successful", player.PlayerName, amountAfterFee, plCurrency);
                });
            }
        }
        else
        {
            Server.NextWorldUpdate(() =>
            {
                player.LocalizeAnnounce(plPrefix, "cmd.transfer.error");
            });
        }
    }

    private async Task BuyHealthshot(CCSPlayerController player)
    {
        if (!player.IsLegalAlive()) return;


        if (BoughtHealtshotPlayers.ContainsKey(player.Slot))
        {
            if (BoughtHealtshotPlayers.TryGetValue(player.Slot, out int qty))
            {
                if (qty >= instance.Config.HealthshotBuyAmount)
                {
                    Server.NextWorldUpdate(() =>
                    {
                        player.LocalizeChatAnnounce(plPrefix, Localize("cmd.caller.buy.exceeded"));
                    });
                    return;
                }
            }
        }

        int? playerBalance = await instance.GetPlayerBalance(player.SteamID.ToString());

        if (playerBalance == null || playerBalance < instance.Config.HealthshotPrice)
        {
            Server.NextWorldUpdate(() =>
            {
                player.LocalizeChatAnnounce(plPrefix, "cmd.caller.money.notenough", Localize("PLUGIN_CURRENCY_NAME"));
            });
            return;
        }
        bool? result = await instance.TakeMoney(player.SteamID.ToString(), instance.Config.HealthshotPrice);
        if (result == null)
        {
            Server.NextWorldUpdate(() =>
            {
                player.LocalizeChatAnnounce(plPrefix, "cmd.caller.error");
            });
            return;
        }

        if (result == false)
        {
            Server.NextWorldUpdate(() =>
            {
                player.LocalizeChatAnnounce(plPrefix, "cmd.caller.error");
            });
            return;
        }

        if (player.IsLegalAlive())
        {
            Server.NextWorldUpdate(() => { player.GiveNamedItem(CsItem.Healthshot); });

            if (BoughtHealtshotPlayers.ContainsKey(player.Slot))
            {
                BoughtHealtshotPlayers[player.Slot]++;
                return;
            }
            BoughtHealtshotPlayers.Add(player.Slot, 1);
        }
    }
}
