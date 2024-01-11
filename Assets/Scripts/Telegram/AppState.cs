namespace Assets.Scripts.Telegram
{
    public class AppState
    {
        public bool Initialized;
        public bool Active;
        public bool WatchLog;
        public long WaitingBuyChatId;
        public decimal BuyAmount;
        public long WaitingSellAmountChatId;
        public decimal SellAmount;
        public long WaitingSellTransactionChatId;
        public long WaitingSettingsChatId;
        public int InSettings;
    }
}
