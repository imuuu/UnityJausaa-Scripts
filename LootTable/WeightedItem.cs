using System;
using Sirenix.OdinInspector;

[Serializable]
public struct WeightedItem<T> : IWeightedLoot
{
    [HideLabel] public T Item;
    [LabelText("Weight"), MinValue(0f)] public float Weight;

    public float GetWeight() => Weight;
}