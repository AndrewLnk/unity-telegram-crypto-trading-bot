using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.CoreAnalyzer;
using Assets.Scripts.CoreAnalyzer.Interfaces;
using Assets.Scripts.Main.Trading;
using Assets.Scripts.Tools;
using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using UnityEngine;

namespace Assets.Scripts.Main
{
    public class PriceFetchingForAverage
    {
        private readonly TradingTargetData targetData;
        private readonly KucoinClient client;
        private readonly AnalyzeTimer analyzeTimer;
        private readonly PricesData pricesData;
        private readonly List<IDataReceive> dataReceives = new List<IDataReceive>();
        private readonly Dictionary<DateTime, decimal> dataList;
        public decimal Fees { get; private set; }
        
        public PriceFetchingForAverage(KucoinClient client, TradingTargetData targetData)
        {
            this.client = client;
            this.targetData = targetData;

            dataList = new Dictionary<DateTime, decimal>();
            analyzeTimer = AnalyzeTimer.CreateInstance("Price Analyze Timer");
            pricesData = new PricesData();
        }

        public void AddDataReceiver(params IDataReceive[] dataReceive) => dataReceives.AddRange(dataReceive);

        public async Task InitializeAndStart()
        {
            var currentTime = await client.SpotApi.ExchangeData.GetServerTimeAsync();
            var localTime = DateTime.Now;
            var localiseSpan = localTime.Subtract(currentTime.Data);

            if (!currentTime.Success)
                return;

            var pair = targetData.GetPair();
            var timeAnalyzeFrom = currentTime.Data.Subtract(CoreParams.GetMaxAverageTime());
            
            var asyncDataRow = await client.SpotApi.ExchangeData.GetKlinesAsync(pair, KlineInterval.OneMinute, timeAnalyzeFrom);
            var asyncDataLast = await client.SpotApi.ExchangeData.GetTickerAsync(pair);
            var asyncDataFees = await client.SpotApi.Account.GetSymbolTradingFeesAsync(pair);
            
            if (!asyncDataRow.Success ||
                !asyncDataLast.Success || 
                !asyncDataLast.Data.BestBidPrice.HasValue ||
                !asyncDataFees.Success)
                return;

            // Setup fees
            var feesData = asyncDataFees.Data.FirstOrDefault(e => e.Symbol.Equals(pair));
            if (feesData == null)
            {
                LogView.AddLog("No fees symbol found.", LogView.ColorInfo.Exception);
                return;
            }

            Fees = (feesData.TakerFeeRate + feesData.MakerFeeRate) / 2;
            
            // Setup row data
            foreach (var kline in asyncDataRow.Data)
            {
                var klineTime = kline.OpenTime.Add(localiseSpan);
                for (var i = 0; i < 60; i += CoreParams.TickerOfAveragePriceFetching)
                {
                    dataList.Add(klineTime, kline.ClosePrice);
                    klineTime = klineTime.AddSeconds(CoreParams.TickerOfAveragePriceFetching);
                }
            }

            if (dataList.Count == 0)
            {
                LogView.AddLog("Failed load prices...", LogView.ColorInfo.Exception);
                return;
            }

            // Setup last data
            var lastTime = dataList.Max(e => e.Key);
            for (var i = 0; i < 60; i += CoreParams.TickerOfAveragePriceFetching)
            {
                if (lastTime < DateTime.Now)
                {
                    lastTime = lastTime.AddSeconds(CoreParams.TickerOfAveragePriceFetching);
                    dataList.Add(lastTime, asyncDataLast.Data.BestBidPrice.Value);
                }
                else break;
            }

            // Start loop
            analyzeTimer.StartLoopTimer(UpdatePrice);
            SendFreshData();
        }

        private async void UpdatePrice()
        {
            var pair = targetData.GetPair();
            var asyncData = await client.SpotApi.ExchangeData.GetTickerAsync(pair);
            
            if (!asyncData.Success)
            {
                LogView.AddLog($"Failed load price [1]", LogView.ColorInfo.Always);
                return;
            }
            
            var price = asyncData.Data.BestBidPrice;

            if (!price.HasValue)
            {
                LogView.AddLog("Failed load price [2]...", LogView.ColorInfo.Always);
                return;
            }
            
            dataList.Add(DateTime.Now, price.Value);
            ClearOldData();
            SendFreshData();
        }

        private void SendFreshData()
        {
            pricesData.SetupData(dataList);
            foreach (var dataReceive in dataReceives) dataReceive.ReceiveData(pricesData);
        }

        private void ClearOldData()
        {
            var clearTo = DateTime.Now.Subtract(CoreParams.GetMaxAverageTime());
            var removeList = dataList.Where(e => e.Key < clearTo);
            foreach (var valuePair in removeList.ToList()) dataList.Remove(valuePair.Key);
        }

        private void DebugList()
        {
            var info = string.Empty;
            
            foreach (var data in dataList)
            {
                info += $"{data.Key}: {data.Value} \n";
            }
            
            Debug.Log(dataList.Count);
            Debug.Log(info);
        }
    }
}
