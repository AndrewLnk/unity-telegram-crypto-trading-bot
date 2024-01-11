using System;
using Kucoin.Net.Enums;

namespace Assets.Scripts.Main.Trading
{
    public class TradingTargetData
    {
        public string ApiKey;
        public string ApiSecret;
        public string ApiPassword;
        public string DefaultCoin;
        public string SecondaryCoin;

        public string GetPair() => $"{SecondaryCoin}-{DefaultCoin}";
    }
}
