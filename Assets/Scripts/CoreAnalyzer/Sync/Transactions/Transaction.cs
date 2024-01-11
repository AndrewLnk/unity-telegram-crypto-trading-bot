using Kucoin.Net.Enums;

namespace Assets.Scripts.CoreAnalyzer.Sync.Transactions
{
    public class Transaction
    {
        public int Id;
        public decimal Price;
        public decimal Amount;
        public OrderSide OrderSide;
    }
}
