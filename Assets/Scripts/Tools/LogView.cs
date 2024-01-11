using System;
using System.Collections.Generic;
using Assets.Scripts.CoreAnalyzer.Sync.Telegram;
using Assets.Scripts.Telegram;
using UnityEngine;

namespace Assets.Scripts.Tools
{
    public static class LogView
    {
        private static AppState appState;

        public enum ColorInfo
        {
            Exception,
            Always,
            OnlySilent,
        }

        public static void SetAppState(AppState state) => appState = state;
        
        public static void AddLog(string text, ColorInfo colorInfo)
        {
            if (colorInfo == ColorInfo.Exception)
                appState.Active = false;

            if (colorInfo == ColorInfo.OnlySilent)
            {
                if (appState.WatchLog) TelegramNotifySync.SendNotification(text);    
            }
            else
            {
                TelegramNotifySync.SendNotification(text);    
            }
        }
    }
}
