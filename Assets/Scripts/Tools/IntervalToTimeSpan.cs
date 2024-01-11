using System;
using Kucoin.Net.Enums;
using UnityEngine;

namespace Assets.Scripts.Tools
{
    public static class IntervalToTimeSpan
    {
        public static TimeSpan GetAnalyzeInterval(KlineInterval interval)
        {
            switch (interval)
            {
                case KlineInterval.OneMinute:
                    return new TimeSpan(0, 1, 0);
                case KlineInterval.ThreeMinutes:
                    return new TimeSpan(0, 3, 0);
                case KlineInterval.FiveMinutes:
                    return new TimeSpan(0, 5, 0);
                case KlineInterval.FifteenMinutes:
                    return new TimeSpan(0, 15, 0);
                case KlineInterval.ThirtyMinutes:
                    return new TimeSpan(0, 30, 0);
                case KlineInterval.OneHour:
                    return new TimeSpan(1, 0, 0);
                case KlineInterval.TwoHours:
                    return new TimeSpan(2, 0, 0);
                case KlineInterval.FourHours:
                    return new TimeSpan(4, 0, 0);
                case KlineInterval.SixHours:
                    return new TimeSpan(6, 0, 0);
                case KlineInterval.EightHours:
                    return new TimeSpan(8, 0, 0);
                case KlineInterval.TwelveHours:
                    return new TimeSpan(12, 0, 0);
                case KlineInterval.OneDay:
                    return new TimeSpan(1, 0, 0, 0);
                case KlineInterval.OneWeek:
                    return new TimeSpan(7, 0, 0, 0);
                default:
                    return new TimeSpan(0, 5, 0);
            }
        }
        
        public static TimeSpan GetAnalyzeInterval(LocalKlineInterval interval)
        {
            return new TimeSpan(0, (int) interval, 0);
        }
    }
}
