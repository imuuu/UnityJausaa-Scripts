using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Transform))]
public class HydraHeadAttack : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Breath particle system to play when attacking.")]
    [SerializeField] private ParticleSystem _toxicBreathPS;

    [Tooltip("Audio source used for the breath sound (optional).")]
    [SerializeField] private AudioSource _breathSound;

    [Tooltip("Prefab spawned where the breath ray hits the ground (splatter).")]
    [SerializeField] private GameObject _toxicSplatterPrefab;

    [Tooltip("The mouth position and forward direction of the hydra head.")]
    [SerializeField] private Transform _mouthPoint;

    [Header("Settings")]
    [Tooltip("Random interval before each attack: x = min seconds, y = max seconds.")]
    [SerializeField] private Vector2 _attackIntervalRange = new Vector2(4f, 6f);

    [Tooltip("Maximum raycast distance from the mouth forward.")]
    [SerializeField] private float _rayDistance = 20f;

    [Tooltip("Optional LayerMask for the ground/hit detection. Leave empty to hit everything.")]
    [SerializeField] private LayerMask _hitMask = ~0;

    [Tooltip("If false, the head will skip attacking until you call EnableAttacking(true).")]
    [SerializeField] private bool _canAttack = true;

    private Coroutine _attackRoutine;

    private void Awake()
    {
        // Basic validation to help catch setup issues early
        if (_mouthPoint == null)
            _mouthPoint = transform;

        // Clamp nonsensical values
        if (_attackIntervalRange.x < 0f || _attackIntervalRange.y < 0f)
            _attackIntervalRange = new Vector2(4f, 6f);
    }

    private void OnEnable()
    {
        StartRoutineIfNeeded();
    }

    private void OnDisable()
    {
        StopRoutineIfRunning();
    }

    /// <summary>
    /// Public toggle to enable/disable attacks at runtime.
    /// </summary>
    public void EnableAttacking(bool enable)
    {
        _canAttack = enable;
    }

    /// <summary>
    /// Forces the next attack immediately (cancels any waiting).
    /// </summary>
    public void Next()
    {
        StartAttackNow();
    }

    // --- Internals ---

    private void StartRoutineIfNeeded()
    {
        if (_attackRoutine == null)
            _attackRoutine = StartCoroutine(AttackLoop());
    }

    private void StopRoutineIfRunning()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }
    }

    private IEnumerator AttackLoop()
    {
        while (true)
        {
            // Wait a random interval between attacks.
            float min = Mathf.Min(_attackIntervalRange.x, _attackIntervalRange.y);
            float max = Mathf.Max(_attackIntervalRange.x, _attackIntervalRange.y);
            float wait = Random.Range(min, max);
            yield return new WaitForSeconds(wait);

            if (_canAttack)
                StartAttackNow();
        }
    }

    private void StartAttackNow()
    {
        // Play breath particles
        if (_toxicBreathPS != null)
            _toxicBreathPS.Play();

        // Play sound
        if (_breathSound != null)
            _breathSound.Play();

        // Spawn splatter where the breath ray hits the ground
        if (_toxicSplatterPrefab != null && _mouthPoint != null)
        {
            if (Physics.Raycast(_mouthPoint.position, _mouthPoint.forward, out var hit, _rayDistance, _hitMask, QueryTriggerInteraction.Ignore))
            {
                Instantiate(_toxicSplatterPrefab, hit.point, Quaternion.identity);
            }
        }
    }
}
