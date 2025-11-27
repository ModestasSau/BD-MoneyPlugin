using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static CounterStrikeSharp.API.Core.Listeners;

namespace Cs2_MoneyPlugin
{
    public class GameEvents
    {
        private readonly MoneyBase? instance;
        private bool isWarmup;
        private Dictionary<string, int> gameEndPay = new();

        public bool isActiveRoundForMoney { get; private set; } = false;

        public GameEvents(MoneyBase instance)
        {
            this.instance = instance;
            RegisterGameEventHooks();
        }

        private void RegisterGameEventHooks()
        {
            if (instance == null) return;

            instance.RegisterEventHandler<EventPlayerConnectFull>(OnClientConnectedAsync);
            instance.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

            instance.RegisterEventHandler<EventRoundAnnounceWarmup>(OnWarmupStart);
            instance.RegisterEventHandler<EventWarmupEnd>(OnWarmupEnd);
            instance.RegisterEventHandler<EventRoundAnnounceMatchStart>(OnMatchStart);
            instance.RegisterEventHandler<EventRoundStart>(OnRoundStart);
            instance.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);

            instance.RegisterListener<OnMapEnd>(OnMapEnd);
            instance.RegisterListener<OnMapStart>(OnMapStarted);

            instance.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            instance.RegisterEventHandler<EventRoundMvp>(OnPlayerMVP);

            instance.RegisterEventHandler<EventCsWinPanelMatch>(BeforeGameEnd);

            instance.AddCommandListener("say", OnPlayerChat);
            instance.AddCommandListener("say_team", OnPlayerChat);
        }

        private HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo info)
        {
            if (!player.IsLegalNotBot()) return HookResult.Continue;


            // Checking if payer object exists in list
            var findPlayer = instance?.playerCommands?.GetCustomPlayerMessage().Find(obj => obj.Player == (int)player.Index);

            // Checking if handler is on for payer
            // Allowing to use commands while handler is on
            if (findPlayer == null || !findPlayer.TransferCustomMsg) return HookResult.Continue;

            string argString = info.ArgString.Replace("\"", "");

            if (instance == null) return HookResult.Continue;

            // Cancel
            if (argString.ToLower() == "cancel")
            {
                player.LocalizeChatAnnounce(MoneyBase.plPrefix, "cmd.cancel");

                // Toggle off handler
                findPlayer.TransferCustomMsg = false;

                MenuManager.CloseActiveMenu(player);

                return HookResult.Handled;
            }

            // Custom value for transfer amount from chat
            if (findPlayer.TransferCustomMsg)
            {
                // Send transfer money
                if (int.TryParse(argString, out int amount) && amount > 0)
                {
                    instance?.playerCommands?.SelectTransferPlayer(player, amount);
                }
                else
                {
                    MenuManager.CloseActiveMenu(player);
                    player.LocalizeChatAnnounce(MoneyBase.plPrefix, "cmd.select.money.incorrect");
                }

            }

            // Toggle off handler
            findPlayer.TransferCustomMsg = false;
            return HookResult.Handled;
        }

        private HookResult OnClientConnectedAsync(EventPlayerConnectFull @event, GameEventInfo info)
        {
            if (instance == null) return HookResult.Continue;

            var player = @event?.Userid;
            var playerSteamid = player?.SteamID.ToString();
            if (player.IsLegalNotBot() && playerSteamid != null)
            {
                _ = instance.CreateDefaultIfNotExist(playerSteamid);
                _ = instance.GetPlayerBalance(player);

                instance.onlinePlayers.Add(player.SteamID.ToString());
            }

            return HookResult.Continue;
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            if (instance == null) return HookResult.Continue;

            if (@event?.Userid != null && !@event.Userid.IsBot)
            {
                var playersteamid = @event.Userid.SteamID.ToString();
                instance.onlinePlayers.Remove(playersteamid);
            }
            return HookResult.Continue;
        }

        private HookResult OnWarmupStart(EventRoundAnnounceWarmup warmupStartEvent, GameEventInfo info)
        {
            isWarmup = true;
            isActiveRoundForMoney = false;

            return HookResult.Continue;
        }

        private HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
        {
            isWarmup = false;

            return HookResult.Continue;
        }

        private HookResult OnMatchStart(EventRoundAnnounceMatchStart matchStartEvent, GameEventInfo info)
        {
            isWarmup = false;
            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd roundEndEvent, GameEventInfo info)
        {
            if (instance == null) return HookResult.Continue;

            if (instance.playerCommands != null)
            {
                instance.playerCommands.ClearBoughtHealtshotPlayers();
            }

            if (!isActiveRoundForMoney) return HookResult.Continue;

            CsTeam winnerTeam = (CsTeam)roundEndEvent.Winner;

            for (int playerIndex = 0; playerIndex <= Server.MaxPlayers; playerIndex++)
            {
                CCSPlayerController? playerController = Utilities.GetPlayerFromUserid(playerIndex);

                // payer is valid, not a bot, and is joined in team
                if (playerController != null && playerController.IsLegalNotBot() && BaseManager.HasJoinedTeam(playerController))
                {
                    CsTeam playerTeam = (CsTeam)playerController.TeamNum;
                    var playerSteamid = playerController.SteamID.ToString();

                    bool isWin = playerTeam == winnerTeam;

                    if (isWin)
                    {
                        int winMoneyAmount = GetVipMoney(playerController.IsVip(), playerController, "event.response.roundwin", instance.Config.MoneyEvents.MoneyForRoundWin);
                        _ = instance.AddMoneyAsync(playerSteamid, winMoneyAmount);
                    }
                    else
                    {
                        int loseMoneyAmount = GetVipMoney(playerController.IsVip(), playerController, "event.response.roundlose", instance.Config.MoneyEvents.MoneyForRoundLose);
                        _ = instance.AddMoneyAsync(playerSteamid, loseMoneyAmount);
                    }
                }
            }

            return HookResult.Continue;
        }


        private HookResult OnRoundStart(EventRoundStart roundStartEvent, GameEventInfo info)
        {
            if (instance == null) return HookResult.Continue;

            if (isWarmup)
            {
                isActiveRoundForMoney = false;
            }
            else
            {
                isActiveRoundForMoney = instance.onlinePlayers.Count >= instance.Config.MinPlayersForActivating;
            }

            return HookResult.Continue;
        }

        private HookResult OnPlayerDeath(EventPlayerDeath? deathEvent, GameEventInfo info)
        {
            try
            {
                if (instance == null) return HookResult.Continue;

                // if null, or suicide do nothing
                if (deathEvent == null || deathEvent.Attacker == null || deathEvent.Attacker == deathEvent.Userid) return HookResult.Continue;

                // if bot made a kill do nothing
                if (deathEvent?.Attacker?.IsBot == true) return HookResult.Continue;

                // if bot is killed by payer, and if toggle is off - do nothing, (if toggle is on, payer will get money for killing bot)
                if (deathEvent?.Userid?.IsBot == true && !instance.Config.GiveMoneyForBotKill) return HookResult.Continue;

                // check if there is inactive round for getting money
                if (!isActiveRoundForMoney) return HookResult.Continue;

                if (deathEvent != null && deathEvent.Attacker != null && deathEvent.Attacker.IsLegalNotBot())
                {
                    var player = deathEvent.Attacker;
                    var attackerSteamId64 = deathEvent.Attacker.SteamID.ToString();
                    string? weapon = deathEvent.Weapon;
                    var moneyAmount = 0;
                    bool foundMelee = false;
                    bool isVip = player.IsVip();

                    if (!string.IsNullOrEmpty(weapon))
                    {
                        if (weapon.Contains("knife") && instance.Config.MoneyEvents.MoneyForKnife != 0)
                        {
                            moneyAmount = GetVipMoney(isVip, player, "event.response.knife", instance.Config.MoneyEvents.MoneyForKnife);
                            foundMelee = true;
                        }
                        else if (weapon.Contains("taser") && instance.Config.MoneyEvents.MoneyForTaser != 0)
                        {
                            moneyAmount = GetVipMoney(isVip, player, "event.response.taser", instance.Config.MoneyEvents.MoneyForTaser);
                            foundMelee = true;
                        }
                    }

                    bool noScope = deathEvent.Noscope;
                    bool headshot = deathEvent.Headshot;
                    bool NoscopeHeadshot = noScope && headshot;

                    if (NoscopeHeadshot && instance.Config.MoneyEvents.MoneyForNoScopeHeadshot != 0)
                    {
                        moneyAmount += GetVipMoney(isVip, player, "event.eesponse.noscope.headshot", instance.Config.MoneyEvents.MoneyForNoScopeHeadshot);
                    }

                    else if (noScope && instance.Config.MoneyEvents.MoneyForNoScope != 0)
                    {
                        moneyAmount += GetVipMoney(isVip, player, "event.response.noscope", instance.Config.MoneyEvents.MoneyForNoScope);
                    }
                    else if (headshot && instance.Config.MoneyEvents.MoneyForHeadshot != 0)
                    {
                        moneyAmount += GetVipMoney(isVip, player, "event.response.headshot", instance.Config.MoneyEvents.MoneyForHeadshot);
                    }

                    if (instance.Config.MoneyEvents.MoneyForKill != 0 && !foundMelee && !headshot && !noScope && !NoscopeHeadshot)
                    {
                        moneyAmount += GetVipMoney(isVip, player, "event.response.kill", instance.Config.MoneyEvents.MoneyForKill);
                    }

                    if (moneyAmount > 0)
                    {
                        _ = instance.AddMoneyAsync(attackerSteamId64, moneyAmount);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in OnPlayerDeath: " + ex.Message);
            }
            return HookResult.Continue;
        }

        public int GetVipMoney(bool isVip, CCSPlayerController player, string localizerName, int eventMoney, string? prefix = null)
        {
            if (instance == null) return eventMoney;

            // check if VIP
            if (isVip)
            {
                player.LocalizeChatAnnounce(prefix ?? MoneyBase.plPrefix, localizerName, (int)Math.Round(eventMoney * instance.Config.VipKebabsMultiplier));
                return (int)Math.Round(eventMoney * instance.Config.VipKebabsMultiplier);
            }

            player.LocalizeChatAnnounce(prefix ?? MoneyBase.plPrefix, localizerName, eventMoney);
            return eventMoney;
        }

        private HookResult OnPlayerMVP(EventRoundMvp mvpEvent, GameEventInfo info)
        {
            if (instance == null) return HookResult.Continue;
            if (!isActiveRoundForMoney) return HookResult.Continue;
            if (!mvpEvent.Userid.IsLegalNotBot()) return HookResult.Continue;

            var mvpPlayerSteamId64 = mvpEvent.Userid.SteamID.ToString();

            if (instance.Config.MoneyEvents.MoneyForMVP > 0)
            {
                int moneyAmount = GetVipMoney(mvpEvent.Userid.IsVip(), mvpEvent.Userid, "event.response.mvp", instance.Config.MoneyEvents.MoneyForMVP);
                _ = instance.AddMoneyAsync(mvpPlayerSteamId64, moneyAmount);
            }
            return HookResult.Continue;
        }

        [GameEventHandler(Mode = HookMode.Pre)]
        private HookResult BeforeGameEnd(EventCsWinPanelMatch @event, GameEventInfo info)
        {
            if (instance == null) return HookResult.Continue;

            if (instance.Config.MoneyEvents.MoneyForGameWin != 0 || instance.Config.MoneyEvents.MoneyForGameLose != 0 || instance.Config.MoneyEvents.MoneyForGameTie != 0)
            {
                // dictionary to pay players
                gameEndPay.Clear();

                var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");
                if (teams == null) return HookResult.Continue;

                int tScore = teams.Where(team => team.TeamNum == 2).First().Score;
                int ctScore = teams.Where(team => team.TeamNum == 3).First().Score;

                if (tScore == 0 && ctScore == 0) return HookResult.Continue;

                // T   -  2
                // CT  -  3
                // TIE - -1
                int winnerTeam = -2;

                if (ctScore < tScore)
                {
                    ChatManager.Announce(MoneyBase.plPrefix, $"{ChatColors.Orange}Terrorists {ChatColors.Green}Won!");
                    Console.WriteLine($"Terrorists Won!");
                    winnerTeam = 2;
                }
                else if (ctScore > tScore)
                {
                    ChatManager.Announce(MoneyBase.plPrefix, $"{ChatColors.Orange}Counter-Terrorists {ChatColors.Green}Won!");
                    Console.WriteLine($"Counter-Terrorists Won!");
                    winnerTeam = 3;
                }
                else if (ctScore == tScore)
                {
                    ChatManager.Announce(MoneyBase.plPrefix, $"{ChatColors.Orange}Teams {ChatColors.Green}TIED!");
                    Console.WriteLine($"TIED!");
                    winnerTeam = -1;
                }

                int moneyAmount;
                bool isVip;
                string playerSteamid;
                var playersList = BaseManager.GetPlayers();
                foreach (var player in playersList)
                {
                    moneyAmount = 0;
                    isVip = false;
                    if (player.TeamNum == (byte)CsTeam.Terrorist || player.TeamNum == (byte)CsTeam.CounterTerrorist)
                    {
                        if (!instance.onlinePlayers.Contains(player.SteamID.ToString())) continue;

                        // Check once if vip
                        isVip = player.IsVip();
                        playerSteamid = player.SteamID.ToString();

                        if (winnerTeam == -1 && instance.Config.MoneyEvents.MoneyForGameTie != 0)
                        {
                            moneyAmount = GetVipMoney(isVip, player, "event.response.gametie", instance.Config.MoneyEvents.MoneyForGameTie);
                            gameEndPay.Add(playerSteamid, moneyAmount);
                            continue;
                        }
                        else if (player.TeamNum == winnerTeam && instance.Config.MoneyEvents.MoneyForGameWin != 0)
                        {
                            moneyAmount = GetVipMoney(isVip, player, "event.response.gamewinner", instance.Config.MoneyEvents.MoneyForGameWin);
                            gameEndPay.Add(playerSteamid, moneyAmount);
                            continue;
                        }
                        else if (instance.Config.MoneyEvents.MoneyForGameLose != 0)
                        {
                            moneyAmount = GetVipMoney(isVip, player, "event.response.gameloser", instance.Config.MoneyEvents.MoneyForGameLose);
                            gameEndPay.Add(playerSteamid, moneyAmount);
                        }
                    }
                }

                // payout all at once
                _ = instance.AddMoneyAsync(gameEndPay);
            }

            return HookResult.Continue;
        }

        private void OnMapEnd()
        {
            if (instance == null || instance.playerCommands == null) return;

            instance.playerCommands.ClearCommandCooldown();
        }

        private void OnMapStarted(string mapName)
        {
            if (instance == null || instance.playerCommands == null) return;

            instance.playerCommands.ClearBoughtHealtshotPlayers();
            instance.onlinePlayers.Clear();
            instance.playerCommands.ClearCommandCooldown();
            isWarmup = true;
            isActiveRoundForMoney = false;
        }
    }
}
