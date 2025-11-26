using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cs2_MoneyPlugin.StatsClasses
{
    public class TransferLog
    {
        public DateTime date { get; set; }
        public string? payer { get; set; }
        public string? receiver { get; set; }
        public int? amount { get; set; }
    }
}
