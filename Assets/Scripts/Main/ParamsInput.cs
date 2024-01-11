using System;
using System.Reflection;
using Assets.Scripts.CoreAnalyzer;
using Assets.Scripts.CoreAnalyzer.Sync.FirebaseSync;
using Assets.Scripts.Tools;
using KlineInterval = Kucoin.Net.Enums.KlineInterval;

namespace Assets.Scripts.Main
{
    public class ParamsInput
    {
        public LocalKlineInterval MinBuyAverageTime = LocalKlineInterval.Minutes2;
        public LocalKlineInterval MinSellAverageTime = LocalKlineInterval.Minute;
        public LocalKlineInterval MaxAverageTime = LocalKlineInterval.Minutes30;
        
        public float AnchorPercentage = 2f;
        public float AbsoluteBuyPercentage = 3f;
        public KlineInterval BuyAnchorWaitingTime = KlineInterval.OneHour;
        public LocalKlineInterval BuyStopTimeout = LocalKlineInterval.Minutes30;

        public float BuyStopPrice = 1;
        public float BuyLimitPrice = 3f;
        public float BuyExchangeAmount = 5;
        public float BuyReserve = 100;
        
        public float SellPercentage = 2f;
        public float AbsoluteSellPercentage = 4f;

        public EventHandler UpdatedBuyDelta;

        public ParamsInput()
        {
            RestoreParams();
            Update();
        }

        public void Update()
        {
            ValidateParams();
            
            CoreParams.MinBuyAverageTime = IntervalToTimeSpan.GetAnalyzeInterval(MinBuyAverageTime);
            CoreParams.MinSellAverageTime = IntervalToTimeSpan.GetAnalyzeInterval(MinSellAverageTime);
            CoreParams.MaxAverageTime = IntervalToTimeSpan.GetAnalyzeInterval(MaxAverageTime);
            
            CoreParams.BuyAnchorWaitingTime = IntervalToTimeSpan.GetAnalyzeInterval(BuyAnchorWaitingTime);
            CoreParams.AnchorPercentage = (decimal) (AnchorPercentage / 100f);
            CoreParams.AbsoluteBuyPercentage = (decimal) (AbsoluteBuyPercentage / 100f);
            
            CoreParams.BuyStopTimeout = IntervalToTimeSpan.GetAnalyzeInterval(BuyStopTimeout);

            CoreParams.BuyStopPrice = (decimal) BuyStopPrice;
            CoreParams.BuyLimitPrice = (decimal) BuyLimitPrice;
            CoreParams.BuyExchangeAmount = (decimal) BuyExchangeAmount;
            CoreParams.BuyReserve = (decimal) BuyReserve;

            CoreParams.SellPercentage = (decimal) (SellPercentage / 100f);
            CoreParams.AbsoluteSellPercentage = (decimal) (AbsoluteSellPercentage / 100f);
        }

        private void ValidateParams()
        {
            if (AnchorPercentage < 0.01f)
                AnchorPercentage = 0.01f;
            
            if (AbsoluteBuyPercentage < 0.01f)
                AbsoluteBuyPercentage = 0.01f;

            if (BuyStopPrice < 0)
                BuyStopPrice = 0;
            
            if (BuyLimitPrice < BuyStopPrice)
                BuyLimitPrice = BuyStopPrice;

            if (BuyExchangeAmount < 1f)
                BuyExchangeAmount = 1f;

            if (SellPercentage < 0.01f)
                SellPercentage = 0.01f;
            
            if (AbsoluteSellPercentage < SellPercentage)
                AbsoluteSellPercentage = SellPercentage;
        }

        public void SaveField(string filed)
        {
            if (nameof(AnchorPercentage).Equals(filed) || nameof(BuyAnchorWaitingTime).Equals(filed))
            {
                UpdatedBuyDelta?.Invoke(this, EventArgs.Empty);
            }

            var value = typeof(ParamsInput).GetField(filed, BindingFlags.Instance | BindingFlags.Public)?.GetValue(this);

            if (value is KlineInterval interval)
            {
                var v = (int) interval;
                FirebaseFields.SetDecimal(filed, v);
            }
            
            if (value is LocalKlineInterval klineInterval)
            {
                var v = (int) klineInterval;
                FirebaseFields.SetDecimal(filed, v);
            }
            
            if (value is float p1)
            {
                FirebaseFields.SetDecimal(filed, (decimal) p1);
            }
        }
        
        private async void RestoreParams()
        {
            MinBuyAverageTime = (LocalKlineInterval) await FirebaseFields.GetDecimal(nameof(MinBuyAverageTime), (int) MinBuyAverageTime);
            MinSellAverageTime = (LocalKlineInterval) await FirebaseFields.GetDecimal(nameof(MinSellAverageTime), (int) MinSellAverageTime);
            MaxAverageTime = (LocalKlineInterval) await FirebaseFields.GetDecimal(nameof(MaxAverageTime), (int) MaxAverageTime);
            
            BuyAnchorWaitingTime = (KlineInterval) await FirebaseFields.GetDecimal(nameof(BuyAnchorWaitingTime), (int) BuyAnchorWaitingTime);
            AnchorPercentage = (float) await FirebaseFields.GetDecimal(nameof(AnchorPercentage), (decimal) AnchorPercentage);
            AbsoluteBuyPercentage = (float) await FirebaseFields.GetDecimal(nameof(AbsoluteBuyPercentage), (decimal) AbsoluteBuyPercentage);
            
            BuyStopTimeout = (LocalKlineInterval) await FirebaseFields.GetDecimal(nameof(BuyStopTimeout), (int) BuyStopTimeout);

            BuyStopPrice = (float) await FirebaseFields.GetDecimal(nameof(BuyStopPrice), (decimal) BuyStopPrice);
            BuyLimitPrice = (float) await FirebaseFields.GetDecimal(nameof(BuyLimitPrice), (decimal) BuyLimitPrice);
            BuyExchangeAmount = (float) await FirebaseFields.GetDecimal(nameof(BuyExchangeAmount), (decimal) BuyExchangeAmount);
            BuyReserve = (float) await FirebaseFields.GetDecimal(nameof(BuyReserve), (decimal) BuyReserve);
            
            SellPercentage = (float) await FirebaseFields.GetDecimal(nameof(SellPercentage), (decimal) SellPercentage);
            AbsoluteSellPercentage = (float) await FirebaseFields.GetDecimal(nameof(AbsoluteSellPercentage), (decimal) AbsoluteSellPercentage);
        }
    }
}
