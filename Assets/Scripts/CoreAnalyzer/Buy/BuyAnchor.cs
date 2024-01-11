using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.CoreAnalyzer.Interfaces;
using Assets.Scripts.CoreAnalyzer.Sync.FirebaseSync;
using Assets.Scripts.CoreAnalyzer.Sync.Transactions;
using Assets.Scripts.Main;
using Assets.Scripts.Main.Trading;
using Assets.Scripts.Tools;
using Kucoin.Net.Clients;
using Kucoin.Net.Enums;

namespace Assets.Scripts.CoreAnalyzer.Buy
{
    public class BuyAnchor : IDataReceive
    {
        private readonly TradingTargetData targetData;
        private readonly KucoinClient client;
        
        public decimal? MaxPrice { get; private set; }
        public DateTime ExpirationTime = DateTime.Now.Subtract(new TimeSpan(1,0,0,0));
        private bool inProcess;
        private bool expiredAlready;
        private bool afterBought;
        
        private const string maxPriceKey = "BuyAnchorMaxPrice";
        private const string expirationTimeKey = "BuyAnchorExpirationTime";
        private static string symbolUpdated;

        public BuyAnchor(KucoinClient client, TradingTargetData targetData)
        {
            this.client = client;
            this.targetData = targetData;
            
            RestoreState();
        }
        
        public void ReceiveData(PricesData data)
        {
            var lastPrice = data.LastPrice();
            
            if (inProcess)
                return;
            
            CheckWhenBought(lastPrice);
            
            CheckIfExpired();
            CheckIfExpiredAndInWaiting(lastPrice);
            CheckIfNeedUpdate();
        }

        private void CheckWhenBought(decimal lastPrice)
        {
            if (!afterBought)
                return;
            
            var minPrice = lastPrice * (1 - CoreParams.AnchorPercentage);
            var limitPrice = CoreParams.BuyLimitPrice;
            
            MaxPrice = Math.Min(minPrice, limitPrice);
            ExpirationTime = DateTime.Now.Add(CoreParams.BuyAnchorWaitingTime);
            expiredAlready = false;
            afterBought = false;
            symbolUpdated = "Bo";
            SaveState();

            LogView.AddLog($"Anchor Updated [{symbolUpdated}]\nPrice: {MaxPrice:0.0000}\nExpire: {ExpirationTime:hh:mm tt}", LogView.ColorInfo.Always);
        }

        private void CheckIfExpired()
        {
            if (DateTime.Now > ExpirationTime)
            {
                expiredAlready = true;
            }
        }
        
        private void CheckIfExpiredAndInWaiting(decimal lastPrice)
        {
            if (expiredAlready && MaxPrice > lastPrice)
            {
                ExpirationTime = DateTime.Now.Add(CoreParams.BuyAnchorWaitingTime);
                expiredAlready = false;
                symbolUpdated = "Co";
                SaveState();
                
                LogView.AddLog($"Anchor Updated [{symbolUpdated}]\nPrice: {MaxPrice:0.0000}\nExpire: {ExpirationTime:hh:mm tt}", LogView.ColorInfo.Always);
            }
        }

        private async void CheckIfNeedUpdate()
        {
            if (!expiredAlready && MaxPrice != null && MaxPrice > 0)
                return;
            
            inProcess = true;

            MaxPrice = null;
            var minPrice = await LoadMinAnchor();
            var minByTransactions = TransactionsKeeper.GetMinPrice() * (1 - CoreParams.AnchorPercentage);
            if (minByTransactions < 0) minByTransactions = minPrice;
            
            var minByState = Math.Min(minPrice, minByTransactions);
            var limitPrice = CoreParams.BuyLimitPrice;
            
            MaxPrice = Math.Min(minByState, limitPrice);
            ExpirationTime = DateTime.Now.Add(CoreParams.BuyAnchorWaitingTime);
            expiredAlready = false;
            inProcess = false;
            symbolUpdated = "Up" + (MaxPrice.Equals(minPrice) ? "M" : "T");
            SaveState();
            
            LogView.AddLog($"Anchor Updated [{symbolUpdated}]\nPrice:{MaxPrice:0.0000}\nExpire: {ExpirationTime:hh:mm tt}", LogView.ColorInfo.Always);
        }

        private async Task<decimal> LoadMinAnchor()
        {
            var currentTime = await client.SpotApi.ExchangeData.GetServerTimeAsync();
            
            if (!currentTime.Success)
                return -1;
            
            var localTime = DateTime.Now;
            var localiseSpan = localTime.Subtract(currentTime.Data);
            var pair = targetData.GetPair();
            var timeAnalyzeFrom = currentTime.Data.Subtract(new TimeSpan(1,0,0,0));
            var asyncDataRow = await client.SpotApi.ExchangeData.GetKlinesAsync(pair, KlineInterval.FiveMinutes, timeAnalyzeFrom);

            if (!asyncDataRow.Success)
                return -1;

            var dataList = new Dictionary<DateTime, decimal>();
            foreach (var kline in asyncDataRow.Data)
            {
                var time = kline.OpenTime.Add(localiseSpan);
                dataList.Add(time, kline.ClosePrice);
            }

            var maxTime = dataList.Max(e=>e.Key);
            var minTime = dataList.Min(e=>e.Key);
            var maxPrice = decimal.MinValue;
            var minPrice = decimal.MaxValue;
            
            while (maxTime > minTime)
            {
                maxTime = maxTime.Subtract(CoreParams.BuyAnchorWaitingTime);

                var analyzeRange = dataList.Where(e => e.Key > maxTime).ToArray();
                var max = analyzeRange.Max(e=>e.Value);
                var min = analyzeRange.Min(e=>e.Value);

                if (max > maxPrice) maxPrice = max;
                if (min < minPrice) minPrice = min;
                
                if (maxPrice * (1 - CoreParams.AnchorPercentage) > minPrice)
                    return minPrice;
                
                foreach (var valuePair in analyzeRange) dataList.Remove(valuePair.Key);
            }
            
            return minPrice;
        }

        public void Reset(bool afterBuyExchange)
        {
            expiredAlready = true;
            afterBought = afterBuyExchange;
        }

        private async void RestoreState()
        {
            var price = await FirebaseFields.GetDecimal(maxPriceKey, -1M);
            var time = await FirebaseFields.GetString(expirationTimeKey, string.Empty);

            if (string.IsNullOrEmpty(time) || price < 0)
                return;

            MaxPrice = price;
            ExpirationTime = DateTime.Parse(time);
            symbolUpdated = "Re";
        }

        private void SaveState()
        {
            FirebaseFields.SetDecimal(maxPriceKey, MaxPrice ?? -1M);
            FirebaseFields.SetString(expirationTimeKey, ExpirationTime.ToString(CultureInfo.CurrentCulture));
        }
    }
}
