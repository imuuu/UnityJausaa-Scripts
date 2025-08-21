using UnityEngine;
using Sirenix.OdinInspector;
using Game.Mobs;

[CreateAssetMenu(menuName = "Bosses/Boss Definition")]
public sealed class BossDefinition : ScriptableObject
{
    [SerializeField] private MobData _mobData;

    [Title("Arena")][SerializeField] private BossArenaDefinition _arena;


    [Title("Phases in order")]
    [SerializeField]
    private BossPhaseDefinition[] _phases;

    public GameObject Prefab => _mobData.Prefab;
    public BossArenaDefinition Arena => _arena;
    public BossPhaseDefinition[] Phases => _phases;
    public MobData MobData => _mobData;
}