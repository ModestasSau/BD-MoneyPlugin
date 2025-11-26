using CounterStrikeSharp.API.Core.Capabilities;

namespace APIMoneyPlugin
{
    public interface IMoneyPlugin
    {
        public static readonly PluginCapability<IMoneyPlugin?> PluginCapability = new("moneyplugin:api");


        // common
        public Task<PlayerStatistics?> GetPlayerStats(string steamId);
        public Task<int?> GetPlayerBalance(string steamId);
        public void GivePlayerMoney(string steamId, int money);

        // JB
        public void AwardLRWinnerPlayer(string steamId, int money);
        public void AwardLRLoserPlayer(string steamId, int money);

        public Task<bool> AwardPlayerMoney(string steamId, int money, string? prefix = null, string? messageToPlayer = null);
    }

    public class PlayerStatistics
    {
        public string Steamid64 { get; set; } = string.Empty;
        public int Top { get; set; } = 0;
        public int Today { get; set; } = 0;
    }
}
