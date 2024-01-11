using System;
using Assets.Scripts.CoreAnalyzer.Buy;
using Assets.Scripts.CoreAnalyzer.Interfaces;
using Assets.Scripts.CoreAnalyzer.Sell;
using Assets.Scripts.CoreAnalyzer.Sync.Transactions;
using Assets.Scripts.Main;
using Assets.Scripts.Main.Trading;
using Assets.Scripts.Tools;
using Kucoin.Net.Enums;

namespace Assets.Scripts.CoreAnalyzer
{
    public class CoreAnalyze : IAnalyze
    {
        private readonly MainAccount defaultAccount;
        private readonly MainAccount secondaryAccount;
        private readonly TransfersCenter transfersCenter;
        private readonly CoreLogic coreLogic;
        private readonly BuyAnchor buyAnchor;
        private bool activeExchange;

        public CoreAnalyze(MainAccount defaultAccount, MainAccount secondaryAccount, TransfersCenter transfersCenter, BuyAnchor buyAnchor)
        {
            this.defaultAccount = defaultAccount;
            this.secondaryAccount = secondaryAccount;
            this.transfersCenter = transfersCenter;
            this.buyAnchor = buyAnchor;
            transfersCenter.SuccessTransaction += SuccessTransaction;
            
            coreLogic = new CoreLogic(buyAnchor, new BuyMechanism(buyAnchor), new SellMechanism());
            coreLogic.Buy += Buy;
            coreLogic.Sell += Sell;
        }
        
        public CoreAnalyze SetupReceivers(Action<IDataReceive> action)
        {
            action.Invoke(coreLogic);
            action.Invoke(buyAnchor);
            return this;
        }

        public void ResetAnchor(bool afterBought) => coreLogic.ResetAnchor(afterBought);

        public bool Initialized() => coreLogic != null;

        void IAnalyze.SetActive(bool active)
        {
            activeExchange = active;
            if (coreLogic != null) coreLogic.ActiveAnalyze = active;
        }

        private void Buy(object sender, Transaction transaction)
        {
            if (!activeExchange)
                return;

            if (transfersCenter.LockedByTransfer)
            {
                LogView.AddLog("[Buy] Locked By Transfer", LogView.ColorInfo.Always);
                return;
            }
            
            if (transaction.Price < CoreParams.BuyStopPrice)
            {
                LogView.AddLog("[Buy] Ignore buy because of Stop Price", LogView.ColorInfo.Always);
                return;
            }

            var amount = GetCurrentAmount();
            transaction.Amount = amount / transaction.Price;
            transaction.OrderSide = OrderSide.Buy;
            transfersCenter.TransferBetweenAccounts(transaction, transaction.Amount, e =>
            {
                if (e.Equals(0)) coreLogic.Bought(null);
            });
        }

        private void Sell(object sender, Transaction transaction)
        {
            if (!activeExchange)
            {
                LogView.AddLog("Can Sell, however analyzer disabled...", LogView.ColorInfo.Always);
                return;    
            }
            
            if (transfersCenter.LockedByTransfer)
            {
                LogView.AddLog("[Sell] Locked By Transfer", LogView.ColorInfo.Always);
                return;
            }
            
            transaction.OrderSide = OrderSide.Sell;
            transfersCenter.TransferBetweenAccounts(transaction, transaction.Amount);
        }
        
        private void SuccessTransaction(object sender, Transaction transaction)
        {
            if (transaction.OrderSide == OrderSide.Buy)
            {
                coreLogic.Bought(transaction);
            }
            else
            {
                coreLogic.Sold(transaction);
            }
        }
        
        private decimal GetCurrentAmount()
        {
            var fullAmount = defaultAccount.Balance;
            var freeAmount = fullAmount - CoreParams.BuyReserve;
            if (freeAmount < 0)
                return 0;
            
            return freeAmount > CoreParams.BuyExchangeAmount ? CoreParams.BuyExchangeAmount : freeAmount;
        }
        
        public decimal GetLastCurrentPrice() => coreLogic.LastCurrentPrice;
    }
}
