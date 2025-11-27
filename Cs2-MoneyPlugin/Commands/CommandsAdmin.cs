using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cs2_MoneyPlugin
{
    public class AdminCommands
    {
        private readonly MoneyBase instance;

        public AdminCommands(MoneyBase instance)
        {
            this.instance = instance;
            RegisterAdminCommands();
        }

        public void RegisterAdminCommands()
        {
            instance.AddCommand("css_givemoney", "Give player specified amount of money.", GiveMoneyCommand);
            instance.AddCommand("css_takemoney", "Take from player specified amount of money.", TakeMoneyCommand);
            instance.AddCommand("css_resetmoney", "Reset players money.", ResetMoneyCommand);
        }

        [CommandHelper(minArgs: 2, usage: "<#userid / steamid64 / name> <amount>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/root")]
        public void GiveMoneyCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!player.IsRootAdmin()) return;
            if (!player.IsLegalNotBot()) return;

            int moneyAmount;
            var target = command.ArgByIndex(1);

            if (string.IsNullOrEmpty(target))
            {
                Console.WriteLine($"[MoneyPlugin] Invalid #userid, SteamID64 or playername.");

                if (command.CallingContext == CommandCallingContext.Console)
                {
                    if (command != null && command.CallingPlayer != null)
                    {
                        command.CallingPlayer.PrintToConsole($"[MoneyPlugin] Invalid #userid, SteamID64 or playername.");
                    }
                }
                else
                {
                    player.LocalizeChatAnnounce(MoneyBase.plPrefix, $"{ChatColors.Red}Invalid #userid, SteamID64 or playername.");
                }
                return;
            }

            if (!int.TryParse(command.ArgByIndex(2), out moneyAmount) || moneyAmount < 1)
            {
                Console.WriteLine($"[MoneyPlugin] Invalid money amount. Ensure you have entered a positive number.");
                if (command.CallingContext == CommandCallingContext.Console)
                {
                    if (command != null && command.CallingPlayer != null)
                    {
                        command.CallingPlayer.PrintToConsole($"[MoneyPlugin] Invalid money amount. Ensure you have entered a positive number.");
                    }
                }
                else
                {
                    player.LocalizeChatAnnounce(MoneyBase.plPrefix, "Invalid money amount. Ensure you have entered a positive number.");
                }
                return;
            }

            if (ulong.TryParse(target, out _))
            {
                if (target.Length == 17)
                {
                    if (target.Substring(0, 3) == "765")
                    {
                        Console.WriteLine($"[MoneyPlugin] {moneyAmount} added to SteamID64: {target}");
                        if (command.CallingContext == CommandCallingContext.Console)
                        {
                            if (command != null && command.CallingPlayer != null)
                            {
                                command.CallingPlayer.PrintToConsole($"[MoneyPlugin] {moneyAmount} kebabs has been added to {target}");
                            }
                        }
                        else
                        {
                            player.LocalizeChatAnnounce(MoneyBase.plPrefix, "cmd.caller.announce.addmoney", moneyAmount, target);
                        }
                        _ = instance.AddMoneyAsync(target, moneyAmount);
                        return;
                    }
                }
            }


            // Target by <name>
            TargetResult? targets = BaseManager.GetTarget(command);

            if (targets == null) return;

            List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.SteamID.ToString().Length == 17 && !player.IsHLTV).ToList();


            // @ALL / @CT / @T toggle
            // "if block" commented below - toggle ON
            // not commented - toggle OFF

            /*if (playersToTarget.Count > 1 || playersToTarget.Count == 0)
            {
                return;
            }*/

            playersToTarget.ForEach(p =>
            {
                if (player!.CanTarget(p))
                {
                    Console.WriteLine($"[MoneyPlugin]{moneyAmount} has been added to {p.PlayerName}");
                    if (command.CallingContext == CommandCallingContext.Console)
                    {
                        if (command != null && command.CallingPlayer != null)
                        {
                            command.CallingPlayer.PrintToConsole($"[MoneyPlugin] {moneyAmount} kebabs has been added to {target}");
                        }
                    }
                    else
                    {
                        player.LocalizeChatAnnounce(MoneyBase.plPrefix, "cmd.caller.announce.addmoney", moneyAmount, p.PlayerName);
                    }
                    p.LocalizeChatAnnounce(MoneyBase.plPrefix, "cmd.target.announce.addmoney", moneyAmount);

                    _ = instance.AddMoneyAsync(p.SteamID.ToString(), moneyAmount);
                }
            });
        }

        [CommandHelper(minArgs: 2, usage: "<#userid / steamid64 / name> <amount>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/root")]
        public void TakeMoneyCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!player.IsRootAdmin()) return;

            int moneyAmount;
            var target = command.ArgByIndex(1);

            if (string.IsNullOrEmpty(target))
            {
                Console.WriteLine($"[MoneyPlugin] Invalid #userid, SteamID64 or playername.");

                if (command.CallingContext == CommandCallingContext.Console && command.CallingPlayer != null)
                {
                    command.CallingPlayer.PrintToConsole($"[MoneyPlugin] Invalid #userid, SteamID64 or playername.");
                }
                else
                {
                    player.LocalizeAnnounce(MoneyBase.plPrefix, $"{ChatColors.Red}Invalid #userid, SteamID64 or playername.");
                }
                return;
            }

            // get money amount
            if (!int.TryParse(command.ArgByIndex(2), out moneyAmount) || moneyAmount < 0)
            {
                Console.WriteLine($"[MoneyPlugin] Invalid money amount. Ensure you have entered a number.");
                if (command.CallingContext == CommandCallingContext.Console && command.CallingPlayer != null)
                {
                    command.CallingPlayer.PrintToConsole($"[MoneyPlugin] Invalid money amount. Ensure you have entered a number.");
                }
                else
                {
                    player.LocalizeAnnounce(MoneyBase.plPrefix, "Invalid money amount. Ensure you have entered a number.");
                }
                return;
            }

            if (ulong.TryParse(target, out _))
            {
                if (target.Length == 17)
                {
                    if (target.Substring(0, 3) == "765")
                    {
                        Console.WriteLine($"[MoneyPlugin] {moneyAmount} taken from SteamID64: {target}");

                        if (command.CallingContext == CommandCallingContext.Console && command.CallingPlayer != null)
                        {
                            command.CallingPlayer.PrintToConsole($"{moneyAmount} kebabs has been taken from {target}");
                        }
                        else
                        {
                            player.LocalizeAnnounce(MoneyBase.plPrefix, "cmd.caller.announce.takemoney", moneyAmount, target);
                        }
                        _ = instance.TakeMoney(target, moneyAmount);
                        return;
                    }
                }
            }


            // Target by <name>
            TargetResult? targets = BaseManager.GetTarget(command);

            if (targets == null) return;

            List<CCSPlayerController> playersToTarget = targets!.Players.Where(player => player != null && player.IsValid && player.SteamID.ToString().Length == 17 && !player.IsHLTV).ToList();

            if (playersToTarget.Count > 1 || playersToTarget.Count == 0)
            {
                return;
            }

            playersToTarget.ForEach(p =>
            {
                if (player!.CanTarget(p))
                {
                    Console.WriteLine($"[MoneyPlugin]{moneyAmount} has been taken from {p.PlayerName}");
                    if (command.CallingContext == CommandCallingContext.Console && command.CallingPlayer != null)
                    {
                        command.CallingPlayer.PrintToConsole($"{moneyAmount} kebabs has been taken from {target}");
                    }
                    else
                    {
                        player.LocalizeAnnounce(MoneyBase.plPrefix, "cmd.caller.announce.takemoney", moneyAmount, target);
                    }
                    p.LocalizeAnnounce(MoneyBase.plPrefix, "cmd.target.announce.takemoney", moneyAmount);
                    _ = instance.TakeMoney(p.SteamID.ToString(), moneyAmount);
                }
            });
        }

        [CommandHelper(minArgs: 1, usage: "<steamid64>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/root")]
        public void ResetMoneyCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!player.IsRootAdmin()) return;

            var target = command.ArgByIndex(1);

            if (string.IsNullOrEmpty(target))
            {
                Console.WriteLine($"[MoneyPlugin] Invalid SteamID64.");
                if (command.CallingContext == CommandCallingContext.Console && command.CallingPlayer != null)
                {
                    command.CallingPlayer.PrintToConsole($"[MoneyPlugin] Invalid SteamID64.");
                }
                else
                {
                    player.LocalizeAnnounce(MoneyBase.plPrefix, $"{ChatColors.Red}Invalid SteamID64.");
                }

                return;
            }

            // Target by <steamid64>
            if (ulong.TryParse(target, out _))
            {
                if (target.Length == 17)
                {
                    if (target.Substring(0, 3) == "765")
                    {
                        Console.WriteLine($"[MoneyPlugin] Reseted Money for SteamID64: {target}");

                        if (command.CallingContext == CommandCallingContext.Console && command.CallingPlayer != null)
                        {
                            command.CallingPlayer.PrintToConsole($"[MoneyPlugin] SteamID64:{target} kebabs have been reseted");
                        }
                        else
                        {
                            player.LocalizeAnnounce(MoneyBase.plPrefix, "cmd.caller.announce.resetmoney", target);
                        }
                        instance.ResetMoney(target);
                        return;
                    }
                }
            }

            Console.WriteLine($"[MoneyPlugin] Invalid SteamID64.");
            if (command.CallingContext == CommandCallingContext.Console && command.CallingPlayer != null)
            {
                command.CallingPlayer.PrintToConsole($"[MoneyPlugin] Invalid SteamID64.");
            }
            else
            {
                player.LocalizeAnnounce(MoneyBase.plPrefix, $"{ChatColors.Red}Invalid SteamID64.");
            }
        }

    }
}
