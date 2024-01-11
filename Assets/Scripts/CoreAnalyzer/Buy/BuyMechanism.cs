using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Main;
using Assets.Scripts.Tools;

namespace Assets.Scripts.CoreAnalyzer.Buy
{
    public class BuyMechanism
    {
        private readonly BuyAnchor buyAnchor;

        public BuyMechanism(BuyAnchor anchor)
        {
            buyAnchor = anchor;
        }

        public bool NeedBuy(PricesData data, DateTime lastBoughtTime)
        {
            if (!data.HasData())
            {
                LogView.AddLog("[Buy Skip] No data...", LogView.ColorInfo.OnlySilent);
                return false;
            }

            if (!buyAnchor.MaxPrice.HasValue)
            {
                LogView.AddLog("[Buy Skip] No max price value...", LogView.ColorInfo.OnlySilent);
                return false;
            }
            
            var buyMaxPrice = buyAnchor.MaxPrice.Value;

            // More then anchor
            if (OutOfBuyArea(data, buyMaxPrice))
            {
                LogView.AddLog($"[Buy Skip] Out of area. Price: {data.LastPrice():0.0000}", LogView.ColorInfo.OnlySilent);
                return false;
            }
            
            // Check if can buy buy absolute min price
            var readyBuyPrice = buyMaxPrice * (1 - CoreParams.AbsoluteBuyPercentage);
            if (InBuyArea(data, readyBuyPrice))
                return true;

            if (StillWaitingByAverage(data, buyMaxPrice, lastBoughtTime))
                return false;
            
            return true;
        }
        
        private static bool OutOfBuyArea(PricesData data, decimal buyPrice)
        {
            var currentPrice = data.LastPrice();
            return currentPrice > buyPrice;
        }
        
        private static bool InBuyArea(PricesData data, decimal buyPrice)
        {
            var currentPrice = data.LastPrice();
            return currentPrice <= buyPrice;
        }
        
        private static bool StillWaitingByAverage(PricesData data, decimal buyMaxPrice, DateTime lastBoughtTime)
        {
            // Max time for get prices
            var averageFromMax = DateTime.Now.Subtract(CoreParams.MaxAverageTime);
            // Get all ordered prices that older than max time period
            var allDataCollected = data.GetPricesNewerThan(averageFromMax);
            
            var averageFromMin = DateTime.Now.Subtract(CoreParams.MinBuyAverageTime);

            var lastTimeOfAverage = averageFromMin;
            var neededPrices = new List<KeyValuePair<DateTime, decimal>>();
            foreach (var keyValuePair in allDataCollected)
            {
                if (keyValuePair.Key >= averageFromMin)
                {
                    neededPrices.Add(keyValuePair);
                    lastTimeOfAverage = keyValuePair.Key;
                }
                else
                {
                    if (keyValuePair.Value <= buyMaxPrice)
                    {
                        neededPrices.Add(keyValuePair);
                        lastTimeOfAverage = keyValuePair.Key;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (neededPrices.Count == 0)
                return false;

            var averagePrice = neededPrices.Select(e => e.Value).Average();
            if (averagePrice < 0)
                return false;
            
            var currentPrice = data.LastPrice();
            LogView.AddLog($"[Buy Skip] Still Waiting [{currentPrice:0.0000}]; " +
                           $"Average: [{averagePrice:0.0000}]; " +
                           $"Average From: [{lastTimeOfAverage:hh:mm:ss tt}]", LogView.ColorInfo.OnlySilent);
            return currentPrice <= averagePrice;
        }
    }
}
