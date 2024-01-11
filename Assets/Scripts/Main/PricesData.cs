using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Tools;
using UnityEngine;

namespace Assets.Scripts.Main
{
    public class PricesData
    {
        private Dictionary<DateTime, decimal> data;

        public void SetupData(Dictionary<DateTime, decimal> newData) => data = newData;

        public bool HasData() => data.Count > 0;

        public decimal LastPrice()
        {
            if (!HasData())
            {
                LogView.AddLog($"Failed get Last Price [No data]", LogView.ColorInfo.Exception);
                return 0;
            }

            var lastItem = data.Max(e => e.Key);
            return data[lastItem];
        }

        public IEnumerable<KeyValuePair<DateTime, decimal>> GetPricesNewerThan(DateTime time)
        {
            if (!data.Any(e => e.Key >= time))
            {
                LogView.AddLog($"Failed create list of price from data: {time:g}", LogView.ColorInfo.Always);
                return new List<KeyValuePair<DateTime, decimal>>();
            }

            return data.Where(e => e.Key >= time)
                .OrderByDescending(e=>e.Key);
        }
        
        public decimal GetAverageOfPricesNewerThan(DateTime time)
        {
            if (!data.Any(e => e.Key >= time))
            {
                var max = data.Max(e => e.Key);
                LogView.AddLog($"Failed create Average price from: {time:g}; Max at: {max} - {LastPrice()}; All count: {data.Count}", LogView.ColorInfo.Always);
                return -1;
            }

            return data.Where(e => e.Key >= time).Select(e => e.Value).Average();
        }
    }
}
