using System;
using System.Globalization;
using Assets.Scripts.CoreAnalyzer.Buy;
using Assets.Scripts.CoreAnalyzer.Interfaces;
using Assets.Scripts.CoreAnalyzer.Sell;
using Assets.Scripts.CoreAnalyzer.Sync.FirebaseSync;
using Assets.Scripts.CoreAnalyzer.Sync.Transactions;
using Assets.Scripts.Main;
using Assets.Scripts.Tools;

namespace Assets.Scripts.CoreAnalyzer
{
    public class CoreLogic: IDataReceive
    {
        private readonly BuyMechanism buyMechanism;
        private readonly SellMechanism sellMechanism;
        private readonly BuyAnchor buyAnchor;

        public EventHandler<Transaction> Buy;
        public EventHandler<Transaction> Sell;

        public bool ActiveAnalyze;
        public decimal LastCurrentPrice;
        
        private DateTime lastBoughtTime;

        public CoreLogic(BuyAnchor buyAnchor, BuyMechanism buyMechanism, SellMechanism sellMechanism)
        {
            this.buyAnchor = buyAnchor;
            this.buyMechanism = buyMechanism;
            this.sellMechanism = sellMechanism;

            RestoreParams();
        }

        public void ResetAnchor(bool afterBought) => buyAnchor.Reset(afterBought);

        void IDataReceive.ReceiveData(PricesData data)
        {
            if (!data.HasData())
            {
                LogView.AddLog("Data in Core Logic is Empty", LogView.ColorInfo.Exception);
                return;
            }
            
            LastCurrentPrice = data.LastPrice();
            
            if (!ActiveAnalyze)
                return;
            
            // Check if need buy
            
            var needBuy = buyMechanism.NeedBuy(data, lastBoughtTime);

            if (needBuy && lastBoughtTime + CoreParams.BuyStopTimeout > DateTime.Now)
            {
                buyAnchor.Reset(true);
                lastBoughtTime = DateTime.Now;
                SaveParams();
                needBuy = false;
            }
            
            if (needBuy)
            {
                var transaction = TransactionsKeeper.CreateFreshTransaction();
                transaction.Price = LastCurrentPrice;
                Buy?.Invoke(this, transaction);
            }
            
            // Check if need sell

            var transactions = TransactionsKeeper.GetTransactions();
            foreach (var transaction in transactions)
            {
                var needSell = sellMechanism.NeedSell(data, transaction.Price, transaction.Id);
                if (!needSell)
                    continue;
                
                Sell?.Invoke(this, transaction);
            }
        }

        public void Bought(Transaction transaction)
        {
            buyAnchor.Reset(true);
            lastBoughtTime = DateTime.Now;
            SaveParams();
            
            if (transaction != null)
            {
                TransactionsKeeper.AddTransaction(transaction);
                LogView.AddLog($"[Bought] [{transaction.Id}]\nPrice: [{transaction.Price:0.0000}]\nAmount: [{transaction.Amount:0.0000}]" +
                               $"\nUnlock buy at [{(lastBoughtTime + CoreParams.BuyStopTimeout):hh:mm tt}]", LogView.ColorInfo.Always);
                CheckForUnexpectedPrice(transaction.Price);
            }
            else
            {
                LogView.AddLog($"[Empty Bought] Unlock buy at [{(lastBoughtTime + CoreParams.BuyStopTimeout):hh:mm tt}]", LogView.ColorInfo.Always);
            }
        }

        public void Sold(Transaction transaction)
        {
            TransactionsKeeper.RemoveTransaction(transaction);
            SaveParams();
            LogView.AddLog($"[Sold] [{transaction.Id}]\nPrice: [{transaction.Price:0.0000}]", LogView.ColorInfo.Always);
            CheckForUnexpectedPrice(transaction.Price);
        }
        
        private void SaveParams()
        {
            FirebaseFields.SetString(nameof(lastBoughtTime), lastBoughtTime.ToString(CultureInfo.CurrentCulture));
        }

        private async void RestoreParams()
        {
            lastBoughtTime = DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0));
            
            var time = await FirebaseFields.GetString(nameof(lastBoughtTime), string.Empty);
            if (string.IsNullOrEmpty(time))
                return;

            lastBoughtTime = DateTime.Parse(time);
            var unlockTime = lastBoughtTime + CoreParams.BuyStopTimeout;
            if (unlockTime > DateTime.Now)
            {
                LogView.AddLog($"Buy unlock at [{unlockTime:hh:mm tt}];", LogView.ColorInfo.Always);
            }
        }

        private void CheckForUnexpectedPrice(decimal priceInTransfer)
        {
            if (priceInTransfer < LastCurrentPrice * 0.7M || priceInTransfer > LastCurrentPrice * 1.3M)
            {
                LogView.AddLog($"Stopped after unexpected transfer result...\n" +
                               $"Price is [{priceInTransfer:0.0000}]. But last current price is [{LastCurrentPrice:0.0000}]", LogView.ColorInfo.Exception);
            }
        }
    }
}
