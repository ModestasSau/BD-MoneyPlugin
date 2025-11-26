using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cs2_MoneyPlugin
{
    partial class MoneyBase
    {
        private const string SQL_CreateMoneyTable = @"
            CREATE TABLE IF NOT EXISTS `{0}` 
                ( 
                    `steamid` varchar(17){1} PRIMARY KEY, 
                    `balance` int(11) NOT NULL DEFAULT 0
                );";


        private const string SQL_CreateTransferLogTable = @"
            CREATE TABLE IF NOT EXISTS `{0}` 
                (
                    `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    `date` DATETIME(3) NOT NULL,
                    `payer` VARCHAR(32) NOT NULL,
                    `receiver` VARCHAR(32) NOT NULL,
                    `amount` INT NOT NULL
                );";

        private const string SQL_CreatePlayerStatisticsTable = @"
            CREATE TABLE IF NOT EXISTS `{0}` 
                (
                    `steamid` VARCHAR(32) NOT NULL PRIMARY KEY,
                    `date` DATE NOT NULL,
                    `total_today` INT NOT NULL DEFAULT 0,
                    `top_per_day` INT NOT NULL DEFAULT 0,
                );";
    }
}
