using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Cs2_MoneyPlugin
{
    public class HTMLPrinter
    {
        private readonly MoneyBase instance;
        public HTMLPrinter(MoneyBase instance)
        {
            this.instance = instance;
        }

        public async void PrintToPlayer(CCSPlayerController? player, IMenuInstance? menuInstance, string str, float showMenuForSeconds = 15)
        {
            if (instance == null || player == null) return;

            if (menuInstance == null)
            {
                MenuManager.CloseActiveMenu(player);
                return;
            }

            Timer? timer = instance.AddTimer(showMenuForSeconds, () =>
            {
                MenuManager.CloseActiveMenu(player);
            }, TimerFlags.STOP_ON_MAPCHANGE);


            while (MenuManager.GetActiveMenu(player) == menuInstance)
            {
                Server.NextFrame(() =>
                {
                    player.PrintToCenterHtml(str);
                });
                await Task.Delay(20);
            }

            if (MenuManager.GetActiveMenu(player) != menuInstance)
            {
                Server.NextFrame(() =>
                {
                    timer?.Kill();
                });
            }
        }


        public IMenuInstance? CloseMenuInstance(CCSPlayerController player, string closeMenuTitle = "Close: !9 or /9")
        {
            ChatMenu cancelMenu = new(closeMenuTitle);
            cancelMenu.ExitButton = true;

            MenuManager.OpenChatMenu(player, cancelMenu);

            return MenuManager.GetActiveMenu(player);
        }
    }
}
