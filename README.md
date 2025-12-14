# MoneyPlugin #

MoneyPlugin for Counter-Strike is universal plugin designed to enhance the gameplay experience by introducing various commands and features related to in-game currency management and rewards. It extends the functionality of Counter-Strike server by providing commands such as `/credits`, `/transfer`, `/hs` to player, and `/givemoney`, `/takemoney`, and `/resetmoney` to administrators, allowing to manage player credits according to specific needs.

## Features ##

- **Commands**:
  - `!menu` - opens all commands menu
  - `!credits` - checks money balance
  - `!settings` - opens settings menu
  - `!stats` - shows player earning statistics (TOP per day and earned today)
  - `!transfer` - transfer money to other player
  - `!hs` - buy healthshot (amount per round configurable in config).

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
- **API Endpoints Usage**: Uses API endpoints for better data checking and managing. Using Auth - Bearer token.
- **Database Support**: Utilize MySQL databases for storing and managing player credits, statistics data.
- **Reward System**: Award players for various in-game events and achievements, including kills, headshots, round wins, knife kills, and more.

## Database ##
- Optionally use **ONLY ONE**: MySQL or API endpoints, otherwise it will take only MySQL.
- SQLite acts like in game cookie and stores players settings.

## Requirements ##
- Metamod:Source v2.0.0 dev build 1374
- CounterStrikeSharp latest version available.

