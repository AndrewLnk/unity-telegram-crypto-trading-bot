using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Main;
using Assets.Scripts.Tools;

namespace Assets.Scripts.CoreAnalyzer.Sell
{
    public class SellMechanism
    {
        public bool NeedSell(PricesData data, decimal buyPrice, int transactionId)
        {
            if (!data.HasData())
                return false;
            
            // Price when ready to sell
            var sellMinPrice = buyPrice * (1 + CoreParams.SellPercentage);
            
            // Check if not ready to sell
            if (OutOfSellArea(data, sellMinPrice))
                return false;
            
            // Check if ready to sell by absolute percentage
            var sellReadyPrice = buyPrice * (1 + CoreParams.AbsoluteSellPercentage);
            if (InSellArea(data, sellReadyPrice))
                return true;
            
            // Check if price ready to sell but price more than average dynamic price
            if (StillGrowByAverage(data, sellMinPrice, transactionId))
                return false;
            
            return true;
        }
        
        private static bool OutOfSellArea(PricesData data, decimal sellPrice)
        {
            var currentPrice = data.LastPrice();
            return currentPrice < sellPrice;
        }

        private static bool InSellArea(PricesData data, decimal sellPrice)
        {
            var currentPrice = data.LastPrice();
            return currentPrice >= sellPrice;
        }
        
        private static bool StillGrowByAverage(PricesData data, decimal sellMinPrice, int transactionId)
        {
            // Max time for get prices
            var averageFromMax = DateTime.Now.Subtract(CoreParams.MaxAverageTime);
            // Get all ordered prices that older than max time period
            var allDataCollected = data.GetPricesNewerThan(averageFromMax);
            
            var averageFromMin = DateTime.Now.Subtract(CoreParams.MinSellAverageTime);

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
                    if (keyValuePair.Value >= sellMinPrice)
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
            LogView.AddLog($"[Sell Skip] [{transactionId}] " +
                           $"Still Waiting [{currentPrice:0.0000}]; " +
                           $"Average: [{averagePrice:0.0000}]; " +
                           $"Average From: [{lastTimeOfAverage:hh:mm:ss tt}]", LogView.ColorInfo.OnlySilent);
            return currentPrice >= averagePrice;
        }
    }
}
