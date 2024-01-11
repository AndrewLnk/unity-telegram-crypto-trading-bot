using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.CoreAnalyzer;
using Assets.Scripts.CoreAnalyzer.Buy;
using Assets.Scripts.CoreAnalyzer.Interfaces;
using Assets.Scripts.CoreAnalyzer.Sync.Transactions;
using Assets.Scripts.Tools;
using Kucoin.Net.Clients;
using Kucoin.Net.Objects;

namespace Assets.Scripts.Main.Trading
{
    public class ClientProcess
    {
        private readonly TradingTargetData targetData;
        private KucoinClient client;
        private MainAccount defaultAccount;
        private MainAccount secondaryAccount;
        private PriceFetchingForAverage priceAnalyze;
        private BuyAnchor buyAnchor;
        public IAnalyze Analyze { get; private set; }
        public TransfersCenter TransfersCenter { get; private set; }

        public ClientProcess(TradingTargetData targetData)
        {
            this.targetData = targetData;
        }

        public void Initialize()
        {
            CreateClient();
            CreateAccounts();
            CreateTransfersCenter();
            SetupAccountsAndStartProcess();
        }

        private void CreateClient()
        {
            client = new KucoinClient(new KucoinClientOptions());
            var credentials = new KucoinApiCredentials(targetData.ApiKey, targetData.ApiSecret, targetData.ApiPassword);
            client.SetApiCredentials(credentials);
        }
        
        private void CreateAccounts()
        {
            defaultAccount = new MainAccount(client, targetData.DefaultCoin);
            secondaryAccount = new MainAccount(client, targetData.SecondaryCoin);
        }

        private void CreateTransfersCenter()
        {
            TransfersCenter = new TransfersCenter(client, defaultAccount, secondaryAccount);
        }

        private async void SetupAccountsAndStartProcess()
        {
            var preparedDefault = await defaultAccount.FetchOrCreate();
            var preparedSecondary = await secondaryAccount.FetchOrCreate();
            
            if (!preparedDefault || !preparedSecondary)
            {
                LogView.AddLog("Started Failed!", LogView.ColorInfo.Exception);
                return;
            }

            priceAnalyze = new PriceFetchingForAverage(client, targetData);
            await priceAnalyze.InitializeAndStart();

            TransfersCenter.FeesPercentage = priceAnalyze.Fees;
            buyAnchor = new BuyAnchor(client, targetData);
            Analyze = new CoreAnalyze(defaultAccount, secondaryAccount, TransfersCenter, buyAnchor)
                .SetupReceivers(e => priceAnalyze.AddDataReceiver(e));
        }

        public async Task RefreshWallets()
        {
            await defaultAccount.FetchOrCreate();
            await secondaryAccount.FetchOrCreate();
        }

        public string GetWalletsInfo()
        {
            return $"{defaultAccount.Asset}: {defaultAccount.Balance:0.0000}\n{secondaryAccount.Asset}: {secondaryAccount.Balance:0.0000}";
        }

        public decimal GetBuyAnchorPrice()
        {
            if (buyAnchor?.MaxPrice == null)
                return -1M;

            return buyAnchor.MaxPrice.Value;
        }
        
        public decimal GetBuyAbsolutePrice()
        {
            if (buyAnchor?.MaxPrice == null)
                return -1M;

            return buyAnchor.MaxPrice.Value * (1 - CoreParams.AbsoluteBuyPercentage);
        }
        
        public string GetBuyAnchorExpiration()
        {
            if (buyAnchor == null)
                return "No Buy Anchor";

            return $"{buyAnchor.ExpirationTime:hh:mm tt}";
        }

        public decimal GetLastPrice()
        {
            if (Analyze == null)
                return -1M;

            return Analyze.GetLastCurrentPrice();
        }

        public string GetDefaultAssetName()
        {
            return defaultAccount.Asset;
        }
        
        public string GetSecondaryAssetName()
        {
            return secondaryAccount.Asset;
        }
        
        public decimal GetDefaultBalanceAvailable()
        {
            return defaultAccount.Balance;
        }
        
        public decimal GetSecondaryBalanceAvailable()
        {
            var transaction = TransactionsKeeper.GetTransactions();
            var sum = transaction.Length > 0 ? transaction.Sum(e => e.Amount) : 0;
            return secondaryAccount.Balance - sum;
        }

        public void UpdateAnchor(bool afterBoughtFlag) => buyAnchor.Reset(afterBoughtFlag);

        public string GetTransactionsLog()
        {
            var transactions = TransactionsKeeper.GetTransactions().OrderByDescending(e=>e.Price);
            var log = string.Empty;
            foreach (var transaction in transactions)
            {
                var price = transaction.Price;
                var sell = price * (1 + CoreParams.SellPercentage);
                var absoluteSell = price * (1 + CoreParams.AbsoluteSellPercentage);
                log += $"\n{transaction.Id:00}. {transaction.Amount:0.0000}: {price:0.0000}↝{sell:0.0000}/{absoluteSell:0.0000}";
            }

            return log;
        }
    }
}
