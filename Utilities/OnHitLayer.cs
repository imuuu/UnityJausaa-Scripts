using System;
using System.Collections;
using Game.PoolSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Utility
{
    /// <summary>
    /// Versatile hit handler:
    /// - Triggers UnityEvents when this object hits specified layers.
    /// - (Optional) spawns one or more prefabs (using ManagerPrefabPooler if available).
    /// - (Optional) deactivates or destroys this object after the hit.
    /// Designed to be allocation-light and inspector-friendly.
    /// </summary>
    public sealed class OnHitLayer : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private bool _useCollision = true;
        [SerializeField] private bool _useTrigger = true;

        [Tooltip("Configured actions by layer. The first matching entries ALL run (top-to-bottom).")]
        [SerializeField] private LayerHitAction[] _actions = Array.Empty<LayerHitAction>();

        private bool[] _onceFired; // tracks Once-per-lifetime actions

        #region UnityEvents (serializable)
        [Serializable] public class UnityEventGO : UnityEvent<GameObject> { }
        #endregion

        #region Data
        public enum SelfAction
        {
            None = 0,
            Deactivate = 1,
            Destroy = 2,
        }

        [Serializable]
        public struct SpawnSpec
        {
            [Tooltip("Prefab to spawn (pooled if ManagerPrefabPooler is present).")]
            public GameObject Prefab;

            [Min(1)]
            [Tooltip("How many instances to spawn.")]
            public int Count;

            [Tooltip("If true, spawn at contact point; otherwise uses this object's position + offset.")]
            public bool UseContactPoint;

            [Tooltip("If true, forward aligns to contact normal before applying rotation offset.")]
            public bool AlignToNormal;

            [Tooltip("Local offset applied after picking base position and final rotation (rot * offset).")]
            [BoxGroup("Position")] public Vector3 PositionOffset;

            [Tooltip("If true, forces Y of final position to PositionY (world).")]
            [BoxGroup("Position")] public bool ForcePositionY;

            [BoxGroup("Position"), ShowIf(nameof(ForcePositionY))]
            public float PositionY;

            [Tooltip("Euler rotation added after base rotation (and optional normal alignment).")]
            [BoxGroup("Rotation")] public Vector3 RotationOffsetEuler;

            [Tooltip("Lock the corresponding world Euler axis to a specific value (in degrees).")]
            [BoxGroup("Rotation")] public bool LockRotationX;
            [BoxGroup("Rotation"), ShowIf(nameof(LockRotationX))] public float LockRotationXValue;

            [BoxGroup("Rotation")] public bool LockRotationY;
            [BoxGroup("Rotation"), ShowIf(nameof(LockRotationY))] public float LockRotationYValue;

            [BoxGroup("Rotation")] public bool LockRotationZ;
            [BoxGroup("Rotation"), ShowIf(nameof(LockRotationZ))] public float LockRotationZValue;

            [Tooltip("If true, adds a random Euler within [Min, Max].")]
            [BoxGroup("Rotation")] public bool RandomizeRotation;

            [BoxGroup("Rotation"), ShowIf(nameof(RandomizeRotation))]
            public Vector3 RandomRotationMin;

            [BoxGroup("Rotation"), ShowIf(nameof(RandomizeRotation))]
            public Vector3 RandomRotationMax;

            [Tooltip("Optional parent override for the spawned objects.")]
            public Transform ParentOverride;
        }

        [Serializable]
        public struct LayerHitAction
        {
            [Tooltip("Optional name for readability in the Inspector.")]
            public string Name;

            [Tooltip("Layers that will trigger this action.")]
            public LayerMask Layers;

            [Tooltip("Only fire if relative collision speed meets/exceeds this (ignored for triggers).")]
            public float MinRelativeVelocity;

            [Tooltip("If set, the other collider must have any of these tags (leave empty to accept any).")]
            public string[] RequiredTags;

            [Header("Self Action")]
            [Tooltip("What to do to THIS object after the hit.")]
            public SelfAction ActionOnSelf;

            [Tooltip("Delay before applying ActionOnSelf.")]
            public float DelaySeconds;

            [Tooltip("If true, this action runs only once for this component lifetime.")]
            public bool Once;

            [Header("Spawning")]
            [Tooltip("Prefabs to spawn on hit (pooled if possible).")]
            public SpawnSpec[] Spawns;

            [Header("Events")]
            public UnityEvent OnHit;
            public UnityEventGO OnHitOther;
        }
        #endregion

        private void Awake()
        {
            int len = _actions != null ? _actions.Length : 0;
            _onceFired = len > 0 ? new bool[len] : Array.Empty<bool>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_useCollision || collision == null) return;

            // Cache first contact (Collision always has at least one on enter)
            ContactPoint cp = collision.GetContact(0);
            HandleHit(
                other: collision.collider,
                contactPoint: cp.point,
                contactNormal: cp.normal,
                relativeVelocity: collision.relativeVelocity.magnitude,
                isTriggerEvent: false
            );
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_useTrigger || other == null) return;

            // Triggers have no contact point/normal; use sensible fallbacks.
            HandleHit(
                other: other,
                contactPoint: transform.position,
                contactNormal: Vector3.up,
                relativeVelocity: 0f,
                isTriggerEvent: true
            );
        }

        /// <summary>
        /// For manual testing from other scripts.
        /// </summary>
        public void TestHit(GameObject otherGO)
        {
            if (otherGO == null) return;
            var col = otherGO.GetComponent<Collider>();
            if (col == null) return;

            HandleHit(
                other: col,
                contactPoint: transform.position,
                contactNormal: Vector3.up,
                relativeVelocity: 0f,
                isTriggerEvent: true
            );
        }

        private void HandleHit(Collider other, Vector3 contactPoint, Vector3 contactNormal, float relativeVelocity, bool isTriggerEvent)
        {
            if (other == null || _actions == null) return;

            int otherLayer = other.gameObject.layer;

            for (int i = 0; i < _actions.Length; i++)
            {
                // Skip if Once and already fired
                if (_onceFired.Length > i && _onceFired[i]) continue;

                LayerHitAction a = _actions[i];
                if (!LayerContains(a.Layers, otherLayer)) continue;

                // Velocity threshold (triggers ignore it)
                if (!isTriggerEvent && a.MinRelativeVelocity > 0f && relativeVelocity < a.MinRelativeVelocity)
                    continue;

                // Tag filter
                if (a.RequiredTags != null && a.RequiredTags.Length > 0)
                {
                    bool tagOK = false;
                    for (int t = 0; t < a.RequiredTags.Length; t++)
                    {
                        string tag = a.RequiredTags[t];
                        if (!string.IsNullOrEmpty(tag) && other.CompareTag(tag))
                        {
                            tagOK = true;
                            break;
                        }
                    }
                    if (!tagOK) continue;
                }

                // Run events
                try { a.OnHit?.Invoke(); } catch { /* ignore user event exceptions */ }
                try { a.OnHitOther?.Invoke(other.gameObject); } catch { /* ignore user event exceptions */ }

                // Spawns
                if (a.Spawns != null)
                {
                    for (int s = 0; s < a.Spawns.Length; s++)
                    {
                        SpawnSpec spec = a.Spawns[s];
                        if (spec.Prefab == null || spec.Count <= 0) continue;

                        for (int c = 0; c < spec.Count; c++)
                        {
                            GameObject spawned = GetPooledOrInstantiate(spec.Prefab);
                            if (spawned == null) continue;

                            // ---------- Rotation ----------
                            // Base rotation: either spawner rotation or aligned to contact normal.
                            Quaternion baseRot = transform.rotation;
                            if (spec.AlignToNormal && contactNormal.sqrMagnitude > 0.0001f)
                                baseRot = Quaternion.LookRotation(contactNormal.normalized);

                            // Apply authored rotation offset.
                            Quaternion rot = baseRot * Quaternion.Euler(spec.RotationOffsetEuler);

                            // Optional randomization.
                            if (spec.RandomizeRotation)
                            {
                                Vector3 min = spec.RandomRotationMin;
                                Vector3 max = spec.RandomRotationMax;
                                if (max.x < min.x) (min.x, max.x) = (max.x, min.x);
                                if (max.y < min.y) (min.y, max.y) = (max.y, min.y);
                                if (max.z < min.z) (min.z, max.z) = (max.z, min.z);

                                Vector3 randEuler = new Vector3(
                                    UnityEngine.Random.Range(min.x, max.x),
                                    UnityEngine.Random.Range(min.y, max.y),
                                    UnityEngine.Random.Range(min.z, max.z)
                                );
                                rot *= Quaternion.Euler(randEuler);
                            }

                            // Apply axis locks LAST so they always win.
                            if (spec.LockRotationX || spec.LockRotationY || spec.LockRotationZ)
                            {
                                rot = ApplyRotationLocks(
                                    rot,
                                    spec.LockRotationX, spec.LockRotationXValue,
                                    spec.LockRotationY, spec.LockRotationYValue,
                                    spec.LockRotationZ, spec.LockRotationZValue
                                );
                            }

                            // ---------- Position ----------
                            Vector3 basePos = spec.UseContactPoint ? contactPoint : transform.position;

                            // Offset is applied in the final rotation's local space (rot * offset).
                            Vector3 pos = basePos + (rot * spec.PositionOffset);

                            // Optional Y override (world).
                            if (spec.ForcePositionY)
                                pos.y = spec.PositionY;

                            // ---------- Apply ----------
                            Transform parent = spec.ParentOverride != null ? spec.ParentOverride : null;
                            Transform tr = spawned.transform;

                            tr.SetParent(parent, worldPositionStays: true);
                            tr.SetPositionAndRotation(pos, rot);
                            if (!spawned.activeSelf) spawned.SetActive(true);
                        }
                    }
                }

                // Self action (after optional delay)
                if (a.ActionOnSelf != SelfAction.None)
                {
                    switch (a.ActionOnSelf)
                    {
                        case SelfAction.Deactivate:
                            StartCoroutine(CoDeactivate(a.DelaySeconds));
                            break;
                        case SelfAction.Destroy:
                            StartCoroutine(CoDestroy(a.DelaySeconds));
                            break;
                    }
                }

                if (a.Once && _onceFired.Length > i)
                    _onceFired[i] = true;
            }
        }

        private static bool LayerContains(LayerMask mask, int layer)
        {
            int m = mask.value;
            int bit = 1 << layer;
            return (m & bit) != 0;
        }

        private static GameObject GetPooledOrInstantiate(GameObject prefab)
        {
            // Uses your pooler if available; falls back to Instantiate.
            var pool = ManagerPrefabPooler.Instance;
            GameObject go = pool != null ? pool.GetFromPool(prefab) : null;
            if (go == null)
                go = Instantiate(prefab);
            return go;
        }

        /// <summary>
        /// Locks specific world Euler axes of <paramref name="rotToModify"/> to exact degree values.
        /// </summary>
        private static Quaternion ApplyRotationLocks(Quaternion rotToModify,
                                                     bool lockX, float lockXDeg,
                                                     bool lockY, float lockYDeg,
                                                     bool lockZ, float lockZDeg)
        {
            Vector3 e = rotToModify.eulerAngles;
            if (lockX) e.x = NormalizeAngle(lockXDeg);
            if (lockY) e.y = NormalizeAngle(lockYDeg);
            if (lockZ) e.z = NormalizeAngle(lockZDeg);
            return Quaternion.Euler(e);
        }

        private static float NormalizeAngle(float degrees)
        {
            // Keep angles in [0, 360) for stability.
            return Mathf.Repeat(degrees, 360f);
        }

        private IEnumerator CoDeactivate(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }

        private IEnumerator CoDestroy(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_actions == null) return;

            for (int i = 0; i < _actions.Length; i++)
            {
                if (_actions[i].Spawns == null) continue;

                for (int s = 0; s < _actions[i].Spawns.Length; s++)
                {
                    var spec = _actions[i].Spawns[s];

                    if (spec.Count < 1) spec.Count = 1;

                    // Sanitize random ranges
                    if (spec.RandomizeRotation)
                    {
                        if (spec.RandomRotationMax.x < spec.RandomRotationMin.x)
                            (spec.RandomRotationMin.x, spec.RandomRotationMax.x) = (spec.RandomRotationMax.x, spec.RandomRotationMin.x);
                        if (spec.RandomRotationMax.y < spec.RandomRotationMin.y)
                            (spec.RandomRotationMin.y, spec.RandomRotationMax.y) = (spec.RandomRotationMax.y, spec.RandomRotationMin.y);
                        if (spec.RandomRotationMax.z < spec.RandomRotationMin.z)
                            (spec.RandomRotationMin.z, spec.RandomRotationMax.z) = (spec.RandomRotationMax.z, spec.RandomRotationMin.z);
                    }

                    // Normalize lock angles for stability
                    if (spec.LockRotationX) spec.LockRotationXValue = NormalizeAngle(spec.LockRotationXValue);
                    if (spec.LockRotationY) spec.LockRotationYValue = NormalizeAngle(spec.LockRotationYValue);
                    if (spec.LockRotationZ) spec.LockRotationZValue = NormalizeAngle(spec.LockRotationZValue);

                    _actions[i].Spawns[s] = spec;
                }
            }
        }
#endif
    }
}
