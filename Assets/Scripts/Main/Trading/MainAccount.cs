using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.CoreAnalyzer.Sync.Telegram;
using Kucoin.Net.Clients;
using Kucoin.Net.Enums;

namespace Assets.Scripts.Main.Trading
{
    public class MainAccount
    {
        private readonly KucoinClient client;
        public readonly string Asset;
        public decimal Balance { get; private set; }
        public bool Available { get; private set; }
        
        public MainAccount(KucoinClient client, string assetName)
        {
            Asset = assetName;
            this.client = client;
            TelegramWalletsStateNotification.AddAccount(this);
        }

        public async Task<bool> FetchOrCreate()
        {
            var tryFetch = await TryFetch();

            if (tryFetch)
            {
                Available = true;
                return true;
            }

            var createdNew = await TryCreate();

            if (!createdNew)
            {
                Available = false;
                return false;
            }
            
            tryFetch = await TryFetch();

            if (tryFetch)
            {
                Available = true;
                return true;
            }
            
            Available = false;
            return false;
        }

        private async Task<bool> TryFetch()
        {
            var accounts = await client.SpotApi.Account.GetAccountsAsync();
            var targetAccount = accounts.Data.FirstOrDefault(e 
                => e.Asset.Equals(Asset) && e.Type.Equals(AccountType.Trade));

            if (targetAccount == null)
                return false;
            
            Balance = targetAccount.Available;
            return true;
        }
        
        private async Task<bool> TryCreate()
        {
            var newAccount = await client.SpotApi.Account.CreateAccountAsync(AccountType.Trade, Asset);
            return newAccount.Success;
        }
    }
}
