using ExodvsBot.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExodvsBot.Domain.Dto
{
    public class StartSettingsDto
    {
        public string txtApiKey { get; set; }
        public string txtApiSecret { get; set; }
        public KlineIntervalEnum cmbKlineInterval { get; set; }
        public int cmbStoploss { get; set; }
        public int cmbTakeProfit { get; set; }
        public int numSellRSI { get; set; }
        public int numBuyRSI { get; set; }
        public RunIntervalEnum cmbRunInterval { get; set; }
    }
}
