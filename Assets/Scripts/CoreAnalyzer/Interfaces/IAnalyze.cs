namespace Assets.Scripts.CoreAnalyzer.Interfaces
{
    public interface IAnalyze
    {
        public bool Initialized();
        public void SetActive(bool active);
        public void ResetAnchor(bool afterBought);
        public decimal GetLastCurrentPrice();
    }
}
