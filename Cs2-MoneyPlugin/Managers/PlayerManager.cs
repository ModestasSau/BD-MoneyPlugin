using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public static class PlayerManager
{
    static public bool IsLegal([NotNullWhen(true)] this CCSPlayerController? player)
    {
        return player != null && player.IsValid && player.PlayerPawn.IsValid && player.PlayerPawn.Value?.IsValid == true;
    }
    static public bool IsLegalNotBot([NotNullWhen(true)] this CCSPlayerController? player)
    {
        return player != null && player.IsValid && player.PlayerPawn.IsValid && player.PlayerPawn.Value?.IsValid == true && !player.IsBot && !player.IsHLTV;
    }

    static public bool IsConnected([NotNullWhen(true)] this CCSPlayerController? player)
    {
        return player.IsLegal() && player.Connected == PlayerConnectedState.PlayerConnected;
    }

    static public bool IsLegalAlive([NotNullWhen(true)] this CCSPlayerController? player)
    {
        return player.IsConnected() && player.PawnIsAlive && player.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
    }

    static public bool IsRootAdmin(this CCSPlayerController? player)
    {
        if (!player.IsLegalNotBot())
        {
            return false;
        }

        return AdminManager.PlayerHasPermissions(player, new String[] { "@css/root" });
    }

    static public bool IsVip(this CCSPlayerController? player)
    {
        if (!player.IsLegalNotBot()) return false;

        return AdminManager.PlayerHasPermissions(player, new String[] { "@css/vip" });
    }

    public static bool CanTarget(this CCSPlayerController controller, CCSPlayerController target)
    {
        if (target.IsBot) return true;
        if (controller is null) return true;

        return AdminManager.CanPlayerTarget(controller, target) || AdminManager.CanPlayerTarget(new SteamID(controller.SteamID), new SteamID(target.SteamID));
    }
}




