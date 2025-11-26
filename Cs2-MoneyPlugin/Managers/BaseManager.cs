using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using CSTimer = CounterStrikeSharp.API.Modules.Timers;


public static class BaseManager
{
    static public List<CCSPlayerController> GetPlayers()
    {
        List<CCSPlayerController> players = Utilities.GetPlayers();
        return players.FindAll(player => player.IsLegalNotBot() && player.IsConnected());
    }

    static public TargetResult? GetTarget(CommandInfo command)
    {
        TargetResult matches = command.GetArgTargetResult(1);

        if (!matches.Any())
        {
            command.ReplyToCommand($"Target {command.GetArg(1)} not found.");
            return null;
        }

        if (command.GetArg(1).StartsWith('@'))
            return matches;

        if (matches.Count() == 1)
            return matches;

        command.ReplyToCommand($"Multiple targets found for \"{command.GetArg(1)}\".");
        return null;
    }

    static public bool HasJoinedTeam(CCSPlayerController playerController)
    {
        if (playerController == null || !playerController.IsValid)
        {
            return false;
        }

        return playerController.TeamNum == 2 || playerController.TeamNum == 3;
    }
}

