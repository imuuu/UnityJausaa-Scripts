using Sirenix.Serialization;
using UnityEngine;

/// <summary>
/// Holds a reference to a boss ability. And Boss Controller Triggers
/// </summary>
public class BossAbilityHolder : MonoBehaviour
{
    [OdinSerialize, SerializeReference]
    [SerializeField] private IAbilityBoss[] _abilities;

    private void Reset()
    {
        if (_abilities == null || _abilities.Length == 0)
        {
            _abilities = new IAbilityBoss[0];
        }
    }

    public IAbilityBoss[] GetAbilities()
    {
        return _abilities;
    }

}