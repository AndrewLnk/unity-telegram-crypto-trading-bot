namespace Assets.Scripts
{
    public static class UserData
    {
        // KuCoin Params
        public const string ApiKey = "69954fd110bc3300015f0bb7"; // API Key from KuCoin 
        public const string ApiSecret = "24351c73-pca5-41c2-01b2-c132ebc396ee"; // API Secret from KuCoin 
        public const string ApiPassword = "TempTemp"; // API Password (created with API)
        
        // Trading Pair. Should exist in KuCoin pairs
        public const string DefaultCoin = "USDT"; // Default Coin USD
        public const string SecondaryCoin = "TON"; // Trading Coin (TON as example)
        
        // Telegram Params
        public const string TelegramToken = "6641884907:AAH9MpYpHBDruSPNKZHbp5x4ZPyloA_k3Q8"; // Get from Father Bot
        public const string TelegramLoginPassword = "1"; // Your user password for login to bot
        
        // Firebase Storage API
        public const string FirebaseStorageUrl = "other-ea8ff.appspot.com"; // Firebase Storage project
    }
}
