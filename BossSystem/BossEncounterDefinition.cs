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
    [BoxGroup("Start Behavior", showLabel: false)]
    [SerializeField] private START_POSITION _startPosition = START_POSITION.FIND_BOSS_CLOSEST_ARENA;

    [SerializeField]
    [BoxGroup("Start Behavior", showLabel: false)]
    [ShowIf("_startPosition", START_POSITION.FIND_BOSS_CLOSEST_ARENA)]
    [Tooltip("Arena that will be spawned")]
    public GameObject ArenaPrefab;

    [SerializeField]
    [BoxGroup("Start Behavior", showLabel: false)]
    [ShowIf("_startPosition", START_POSITION.FIND_BOSS_CLOSEST_ARENA)]
    [Tooltip("Arena will be spawned/enabled when time is up")]
    public bool SpawnArenaOnStart = true;
    [SerializeField]
    [BoxGroup("Start Behavior", showLabel: false)]
    [ShowIf("_startPosition", START_POSITION.FIND_BOSS_CLOSEST_ARENA)]
    [Tooltip("Enable boss behavior when player is within range")]
    public bool EnableBossBehaviorOnRange = true;
    [SerializeField]
    [BoxGroup("Start Behavior", showLabel: false)]
    [ShowIf("@EnableBossBehaviorOnRange && _startPosition == START_POSITION.FIND_BOSS_CLOSEST_ARENA")]
    [Tooltip("Range within which the boss will become active")]
    public float BossBehaviorRange = 30f;

    public BossDefinition[] Bosses => _bosses;
    public bool KillAllMobsOnStart => _killAllMobsOnStart;
    public bool LockArenaBounds => _lockArenaBounds;

    public float SpawnTimeSeconds => _spawnTimeSeconds;

    public START_POSITION StartPosition => _startPosition;
}