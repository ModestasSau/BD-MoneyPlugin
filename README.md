# MoneyPlugin #

MoneyPlugin for Counter-Strike is a versatile plugin designed to enhance the gameplay experience by introducing various commands and features related to in-game currency management and rewards. It extends the functionality of your Counter-Strike server by providing commands such as `/givemoney`, `/takemoney`, and `/resetmoney`, allowing server administrators to manage player finances according to specific needs. 

## Features ##

- **Commands**:
  - `!menu` - opens all commands menu
  - `!money` - show money amount
  - `!settings` - opens settings menu
  - `!stats` - shows player earning statistics (TOP per day and earned today)
  - `!transfer` - transfer money to other player
  - `!hs` - buy healthshot (you can buy one per round).

- **Admin Commands**
     - `!givemoney` - give player specified amount of money
     - `!takemoney` - take specified amount of money from player
     - `!resetmoney` - reset player money

- **MoneyPluginAPI**
	Methods:
	- `GetPlayerStats(string steamid)` *returns PlayerStatistics object*
	- `GetPlayerBalance(string steamid)` *returns int value*
	- `GivePlayerMoney(string steamid, in money)` *void*
	- `AwardPlayerMoney(string steamid, int money, string? prefix = null, string? messageToPlayer = null)`
 		- 	*prefix and messageToPlayer are optional, by default null, there applies VIP multiplier for given money amount. Returns bool*
	**For Jailbreak servers:**
	- `AwardLRWinnerPlayer(string steamid, int money)` *(you can use these, or from above, doesnt matter)
	- `AwardLRLoserPlayer(string steamid, int money)`

- **API Endpoints Usage**: Uses API endpoints for better data checking and managing. Using Auth - Bearer token.
- **Database Support**: Utilize MySQL databases for storing and managing player financial data.
- **Reward System**: Award players for various in-game events and achievements, including kills, headshots, round wins, knife kills, and more.

## Database ##
- Optionally use **ONLY ONE** of: MySQL or API endpoints, otherwise it wont work at all.
- SQLite acts like in game cookie and stores players settings.

