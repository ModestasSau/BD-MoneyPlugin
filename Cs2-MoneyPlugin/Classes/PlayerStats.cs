using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cs2_MoneyPlugin
{
    public class PlayerStats
    {
        public int Top { get; set; } = 0;
        public int Today { get; set; } = 0;
    }

    public class PlayerStatsAPI
    {
        public string Steamid64 { get; set; } = string.Empty;
        public int Top { get; set; } = 0;
        public int Today { get; set; } = 0;
    }
}
