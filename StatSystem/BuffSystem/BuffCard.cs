namespace Game.BuffSystem
{
    public abstract class BuffCard
    {
        public abstract BUFF_CARD_TYPE BuffType { get; protected set; }
        public abstract void ApplyBuffToVisual(int index, ChooseBuffCardVisual visual);

        public abstract RarityDefinition RarityDefinition { get; protected set; }

        public abstract void OnSelectBuff(int index);

        public abstract bool Roll();

    }
}
