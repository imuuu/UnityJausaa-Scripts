using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Bosses/Arena Definition")]
public sealed class BossArenaDefinition : ScriptableObject
{
    [SerializeField] private ARENA_TYPE _type = ARENA_TYPE.CIRCLE;
    [ShowIf("_type", ARENA_TYPE.CIRCLE)][SerializeField, Min(1f)] private float _radius = 20f;
    [ShowIf("_type", ARENA_TYPE.POLYGON)][SerializeField] private Vector3[] _points; // user prefers Vector3

    [Title("Bounds")][SerializeField] private OUT_OF_BOUNDS_RULE _rule = OUT_OF_BOUNDS_RULE.Pushback;
    [SerializeField, Min(0f)] private float _boundsDamagePerSecond = 20f; // used if DamageOverTime

    [Title("VFX")][SerializeField] private GameObject _ringPrefab;

    public ARENA_TYPE Type => _type; public float Radius => _radius; public Vector3[] Points => _points;
    public OUT_OF_BOUNDS_RULE Rule => _rule; public float BoundsDamagePerSecond => _boundsDamagePerSecond;
    public GameObject RingPrefab => _ringPrefab;
}