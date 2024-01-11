using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Main.Trading;

namespace Assets.Scripts.CoreAnalyzer.Sync.Telegram
{
    public static class TelegramWalletsStateNotification
    {
        private static readonly List<MainAccount> accounts = new List<MainAccount>();

        public static void AddAccount(MainAccount account)
        {
            if (accounts.Any(e=>e.Asset.Equals(account.Asset)))
                return;
                
            accounts.Add(account);
        }

        public static void SaveState(string log = null)
        {
            var state = string.Empty;
            foreach (var account in accounts)
            {
                if (state.Length > 0) state += ", ";
                state += $"{account.Asset}: {account.Balance:0.0000}";
            }
            
            TelegramNotifySync.SendNotification($"[Keep State] {state} {log}");
        }
    }
}
