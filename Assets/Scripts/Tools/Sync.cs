namespace Assets.Scripts.Tools
{
    public abstract class Sync
    {
        protected Sync()
        {
            MainSync.Instance.Add(this);
        }

        public abstract void Update();
    }
}
