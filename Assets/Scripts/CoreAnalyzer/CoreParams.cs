using System;
using System.Linq;

namespace Assets.Scripts.CoreAnalyzer
{
    public static class CoreParams
    {
        public const int TickerOfAveragePriceFetching = 10;
        
        public static TimeSpan MinBuyAverageTime;
        public static TimeSpan MinSellAverageTime;
        public static TimeSpan MaxAverageTime;
        // Buy Analyze
        public static TimeSpan BuyAnchorWaitingTime; // Fetch price by this period
        public static decimal AnchorPercentage; // Min delta when deviation from price
        public static decimal AbsoluteBuyPercentage; // Min delta when deviation from price
        
        // After Buy
        public static TimeSpan BuyStopTimeout;

        // Buy Actions
        public static decimal BuyStopPrice;
        public static decimal BuyLimitPrice;
        public static decimal BuyExchangeAmount;
        public static decimal BuyReserve;

        // Sell Analyze
        public static decimal SellPercentage; // Normal delta when ready to sell
        public static decimal AbsoluteSellPercentage; // Max delta when ready to sell

        public static TimeSpan GetMaxAverageTime()
        {
            var set = new[] {MinBuyAverageTime, MinSellAverageTime, MaxAverageTime};
            return set.Max();
        }
    }
}
