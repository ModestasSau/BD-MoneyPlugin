using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Cs2_MoneyPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class ChatManager
{
    // chat + centre text print
    static public void Announce(String prefix, String str)
    {
        Server.PrintToChatAll(prefix + str);
        PrintCentreAll(str);
    }

    static public void PrintPrefix(this CCSPlayerController? player, String prefix, String str)
    {
        if (player.IsLegalNotBot() && player.IsConnected())
        {
            player.PrintToChat(prefix + str);
        }
    }

    static public void Announce(this CCSPlayerController? player, String prefix, String str)
    {
        if (player.IsLegalNotBot() && player.IsConnected())
        {
            player.PrintPrefix(prefix, str);
            player.PrintToCenter(str);
        }
    }

    static public void ChatAnnounce(this CCSPlayerController? player, String prefix, String str)
    {
        if (player.IsLegalNotBot() && player.IsConnected())
        {
            player.PrintPrefix(prefix, str);
        }
    }

    static public void PrintCentreAll(String str)
    {
        foreach (CCSPlayerController player in BaseManager.GetPlayers())
        {
            player.PrintToCenter(str);
        }
    }


    static public void LocalizeAnnounce(this CCSPlayerController? player, String prefix, String name, params Object[] args)
    {
        player.Announce(prefix, Localize(name, args));
    }

    static public void LocalizeChatAnnounce(this CCSPlayerController? player, String prefix, String name, params Object[] args)
    {
        player.ChatAnnounce(prefix, Localize(name, args));
    }

    public static String Localize(String name, params Object[] args)
    {
        return MoneyBase.Localize(name, args);
    }
}