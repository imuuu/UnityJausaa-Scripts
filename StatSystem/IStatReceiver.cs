namespace Game.StatSystem
{
    public interface IStatReceiver
    {
        public bool HasStat(STAT_TYPE type);
        public void SetStat(Stat stat);
        public void SetStats(StatList statList);
        public StatList GetStats();
    }
}