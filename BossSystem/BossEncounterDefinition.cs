using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Bosses/Encounter Definition")]
public sealed partial class BossEncounterDefinition : ScriptableObject
{

    [SerializeField] private BossDefinition[] _bosses;

    [Title("Global Flags")]
    [Tooltip("Time in seconds after which the boss encounter starts. -1 = never")]
    [SerializeField] private float _spawnTimeSeconds = 60 * 20;
    [SerializeField] private bool _killAllMobsOnStart = true;
    [SerializeField] private bool _lockArenaBounds = true;
    [SerializeField] private START_POSITION _startPosition = START_POSITION.AROUND_PLAYER;

    public BossDefinition[] Bosses => _bosses;
    public bool KillAllMobsOnStart => _killAllMobsOnStart;
    public bool LockArenaBounds => _lockArenaBounds;

    public float SpawnTimeSeconds => _spawnTimeSeconds;

    public START_POSITION StartPosition => _startPosition;
}