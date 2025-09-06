using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Bosses/Arena Definition")]
public sealed class BossArenaDefinition : ScriptableObject
{
    public ARENA_TYPE Type = ARENA_TYPE.CIRCLE;
    [ShowIf("Type", ARENA_TYPE.CIRCLE)][Min(1f)] public float CircleRadius = 20f;
    [ShowIf("Type", ARENA_TYPE.POLYGON)]public Vector3[] PolygonPoints;

    [ShowIf("Type", ARENA_TYPE.SQUARE)] public float SquareSize = 20f;

    [Title("Bounds")][SerializeField] public OUT_OF_BOUNDS_RULE Rule = OUT_OF_BOUNDS_RULE.Pushback;
    [Min(0f)] public float BoundsDamagePerSecond = 20f; // used if DamageOverTime

    public bool TeleportPlayerToArenaEdge = true;

    [BoxGroup("VFX", showLabel: false)]
    public VFX_TYPE VfxShowType;

    [BoxGroup("VFX", showLabel: false)]
    public GameObject RingPrefab;

    [BoxGroup("VFX", showLabel: false)]
    [ShowIf("VfxShowType", VFX_TYPE.MULTIPLE_EFFECTS_ON_CIRCLE)]
    public int VfxShowCount;

    public enum BOSS_SPAWN_POS
    {
        ARENA_CENTER,
        CUSTOM_LOCAL_OFFSET
    }

    [Header("Boss Spawn")]
    [SerializeField] public BOSS_SPAWN_POS BossSpawnPosition = BOSS_SPAWN_POS.ARENA_CENTER;

    // Local offset in the arena's local XZ plane.
    // Example: (2, 0) spawns 2 units to the arena's +X side.
    [SerializeField]
    [ShowIf("BossSpawnPosition", BOSS_SPAWN_POS.CUSTOM_LOCAL_OFFSET)]
    public Vector2 BossSpawnLocalOffsetXZ = Vector2.zero;





    public enum VFX_TYPE
    {
        SINGLE_EFFECT,
        MULTIPLE_EFFECTS_ON_CIRCLE,
    }
}