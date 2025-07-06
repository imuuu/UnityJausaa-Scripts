using Game.StatSystem;
using Sirenix.OdinInspector;
using UnityEngine;
namespace Game.BuffSystem
{
    [System.Serializable]
    public class BuffModifier : Modifier, IWeightedLoot
    {
        [Title("Loot Table")]
        [SerializeField,Min(1)] private float _weight = 1f;

        public float GetWeight()
        {
            return _weight;
        }
    }
}