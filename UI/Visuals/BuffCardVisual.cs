
using Game.BuffSystem;
using Nova;

[System.Serializable]
public abstract class BuffCardVisual : ItemVisuals
{

    public virtual void Bind(int index, BuffCard buffCard)
    {
        DisableAll();
        buffCard.ApplyBuffToVisual(index, this);

    }

    protected abstract void DisableAll();
}