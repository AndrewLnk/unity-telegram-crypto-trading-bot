using System;
using System.Threading.Tasks;
using Assets.Scripts.CoreAnalyzer.Sync.Telegram;
using Assets.Scripts.CoreAnalyzer.Sync.Transactions;
using Assets.Scripts.Main.Trading;
using Assets.Scripts.Tools;
using Kucoin.Net.Clients;
using Kucoin.Net.Enums;

namespace Assets.Scripts.Main
{
    public class TransfersCenter
    {
        private readonly KucoinClient client;
        private readonly MainAccount defaultAccount;
        private readonly MainAccount secondaryAccount;
        public decimal FeesPercentage;
        public bool LockedByTransfer { get; private set; }
        
        public EventHandler<Transaction> SuccessTransaction;

        public TransfersCenter(KucoinClient client, MainAccount defaultAccount, MainAccount secondaryAccount)
        {
            this.defaultAccount = defaultAccount;
            this.secondaryAccount = secondaryAccount;
            this.client = client;
        }
        
        public async void TransferBetweenAccounts(Transaction transaction, decimal amount, Action<int> whenFinished = null)
        {
            if (LockedByTransfer)
            {
                LogView.AddLog($"[Transfer] Locked by transfer", LogView.ColorInfo.OnlySilent);
                whenFinished?.Invoke(-1);
                return;
            }
            
            if (!defaultAccount.Available || !secondaryAccount.Available)
            {
                LogView.AddLog($"[Transfer] Failed Transfer - Wallets not available", LogView.ColorInfo.Always);
                whenFinished?.Invoke(-1);
                return;
            }

            if (transaction.OrderSide == OrderSide.Buy)
            {
                amount = Math.Clamp(amount, 0, defaultAccount.Balance);
                transaction.Amount = amount;
            }
            else
            {
                amount = Math.Clamp(amount, 0, secondaryAccount.Balance);
                transaction.Amount = amount;
            }

            var intBalance = (int) (amount * 10000);
            amount = intBalance / 10000M;

            if (amount <= 0.1M)
            {
                whenFinished?.Invoke(0);
                LogView.AddLog($"[Transfer] Failed Transfer - Small Balance", LogView.ColorInfo.Always);
                return;
            }

            LockedByTransfer = true;
            var symbol = $"{secondaryAccount.Asset}-{defaultAccount.Asset}";
            var transfer = await client.SpotApi.Trading.PlaceOrderAsync(symbol, transaction.OrderSide, NewOrderType.Market, quantity: amount);

            if (!transfer.Success)
            {
                whenFinished?.Invoke(-2);
                LogView.AddLog($"[Transfer] Failed Transfer - Failed Request: {transfer.Error?.Message}", LogView.ColorInfo.Always);
                LockedByTransfer = false;
                return;
            }

            var defaultAccountBalance = defaultAccount.Balance;
            var secondaryAccountBalance = secondaryAccount.Balance;

            await Task.Delay(new TimeSpan(0,0,5));
            await defaultAccount.FetchOrCreate();
            await secondaryAccount.FetchOrCreate();

            if (transaction.OrderSide == OrderSide.Buy)
            {
                var spent = defaultAccountBalance - defaultAccount.Balance;
                var got = secondaryAccount.Balance - secondaryAccountBalance;
                transaction.Price = got > 0 ? (spent / got) * (1 + FeesPercentage) : 0;
                transaction.Amount = got;
            }
            else
            {
                var spent = secondaryAccountBalance - secondaryAccount.Balance;
                var got = defaultAccount.Balance - defaultAccountBalance;
                transaction.Price = spent > 0 ? (got / spent) : 0;
                transaction.Amount = got;
            }

            if (transaction.OrderSide == OrderSide.Buy)
            {
                TelegramWalletsStateNotification.SaveState($"[{transaction.Id}] <= {transaction.Price:0.0000}");    
            }
            else
            {
                TelegramWalletsStateNotification.SaveState($"[{transaction.Id}] => {transaction.Price:0.0000}");
            }

            whenFinished?.Invoke(1);
            SuccessTransaction?.Invoke(this, transaction);
            LockedByTransfer = false;
        }
        
        public async void TransferBuyLite(decimal amount, decimal price, Action whenFinished)
        {
            if (LockedByTransfer)
            {
                LogView.AddLog($"[Buy Lite] Locked by transfer", LogView.ColorInfo.OnlySilent);
                whenFinished?.Invoke();
                return;
            }
            
            if (!defaultAccount.Available || !secondaryAccount.Available)
            {
                whenFinished?.Invoke();
                return;
            }
            var buyAmount = (amount * (1 - FeesPercentage) / price);

            var intBalance = (int) (buyAmount * 10000);
            buyAmount = intBalance / 10000M;
            
            if (buyAmount <= 0.1M)
            {
                whenFinished?.Invoke();
                LogView.AddLog($"[Buy Lite] Failed Transfer - Small Balance. Price: [{price}]", LogView.ColorInfo.Always);
                return;
            }

            LockedByTransfer = true;
            var symbol = $"{secondaryAccount.Asset}-{defaultAccount.Asset}";
            var transfer = await client.SpotApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, NewOrderType.Market, quantity: buyAmount);
            
            if (!transfer.Success)
            {
                whenFinished?.Invoke();
                LogView.AddLog($"[Buy Lite] Failed Transfer - Failed Request: {transfer.Error?.Message}", LogView.ColorInfo.Always);
                LockedByTransfer = false;
                return;
            }

            var defaultAccountBalance = defaultAccount.Balance;
            var secondaryAccountBalance = secondaryAccount.Balance;

            await Task.Delay(new TimeSpan(0,0,5));
            await defaultAccount.FetchOrCreate();
            await secondaryAccount.FetchOrCreate();

            var transaction = TransactionsKeeper.CreateFreshTransaction();
            transaction.Amount = buyAmount;
            transaction.OrderSide = OrderSide.Buy;
            if (transaction.OrderSide == OrderSide.Buy)
            {
                var spent = defaultAccountBalance - defaultAccount.Balance;
                var got = secondaryAccount.Balance - secondaryAccountBalance;
                transaction.Price = got > 0 ? (spent / got) * (1 + FeesPercentage) : price;
                transaction.Amount = got;
            }
            else
            {
                var spent = secondaryAccountBalance - secondaryAccount.Balance;
                var got = defaultAccount.Balance - defaultAccountBalance;
                transaction.Price = spent > 0 ? (got / spent) : 0;
                transaction.Amount = got;
            }

            if (transaction.OrderSide == OrderSide.Buy)
            {
                TelegramWalletsStateNotification.SaveState($"[{transaction.Id}] <= {transaction.Price:0.0000}");    
            }
            else
            {
                TelegramWalletsStateNotification.SaveState($"[{transaction.Id}] => {transaction.Price:0.0000}");
            }
            
            SuccessTransaction?.Invoke(this, transaction);
            whenFinished?.Invoke();
            LockedByTransfer = false;
        }
        
        public async void TransferSellLite(decimal amount, decimal price, Action whenFinished)
        {
            if (LockedByTransfer)
            {
                LogView.AddLog($"[Sell Lite] Failed Transfer - Locked by transfer", LogView.ColorInfo.OnlySilent);
                whenFinished?.Invoke();
                return;
            }
            
            if (!defaultAccount.Available || !secondaryAccount.Available)
            {
                whenFinished?.Invoke();
                return;
            }
            
            var intBalance = (int) (amount * 10000);
            amount = intBalance / 10000M;
            
            if (amount <= 0.1M)
            {
                whenFinished?.Invoke();
                LogView.AddLog($"[Sell Lite] Failed Transfer - Small Balance. Price: [{price}]", LogView.ColorInfo.Always);
                return;
            }

            LockedByTransfer = true;
            var symbol = $"{secondaryAccount.Asset}-{defaultAccount.Asset}";
            var transfer = await client.SpotApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, NewOrderType.Market, quantity: amount);
            
            if (!transfer.Success)
            {
                whenFinished?.Invoke();
                LogView.AddLog($"[Sell Lite] Failed Transfer - Failed Request: {transfer.Error?.Message}", LogView.ColorInfo.Always);
                LockedByTransfer = false;
                return;
            }

            var defaultAccountBalance = defaultAccount.Balance;
            var secondaryAccountBalance = secondaryAccount.Balance;
            
            await Task.Delay(new TimeSpan(0,0,5));
            await defaultAccount.FetchOrCreate();
            await secondaryAccount.FetchOrCreate();

            var spent = secondaryAccountBalance - secondaryAccount.Balance;
            var got = defaultAccount.Balance - defaultAccountBalance;
            var finalPrice = spent > 0 ? (got / spent) : 0;
            
            TelegramWalletsStateNotification.SaveState($"[-] => {finalPrice:0.0000}");
            
            LogView.AddLog($"[Sold Lite]\nPrice: [{finalPrice:0.0000}]\nAmount: [{got:0.0000}]", LogView.ColorInfo.Always);
            
            whenFinished?.Invoke();
            LockedByTransfer = false;
        }
    }
}
