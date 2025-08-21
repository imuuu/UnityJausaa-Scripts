using System;
using System.Collections;
using Game.PoolSystem;
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

            [Tooltip("Local/world offset applied after picking base position.")]
            public Vector3 PositionOffset;

            [Tooltip("Euler rotation added after base rotation (and optional normal alignment).")]
            public Vector3 RotationOffsetEuler;

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

                            Vector3 basePos = spec.UseContactPoint ? contactPoint : transform.position;
                            Vector3 pos = basePos + spec.PositionOffset;

                            Quaternion rot = transform.rotation;
                            if (spec.AlignToNormal)
                            {
                                // Align forward to impact normal (fallback to current rotation if zero)
                                if (contactNormal.sqrMagnitude > 0.0001f)
                                    rot = Quaternion.LookRotation(contactNormal);
                            }
                            rot *= Quaternion.Euler(spec.RotationOffsetEuler);

                            Transform parent = spec.ParentOverride != null ? spec.ParentOverride : null;

                            Transform tr = spawned.transform;
                            tr.SetPositionAndRotation(pos, rot);
                            tr.SetParent(parent, worldPositionStays: true);
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
            // Example from user: ManagerPrefabPooler.Instance.GetFromPool(_spikeyBallsPrefab);
            var pool = ManagerPrefabPooler.Instance;
            GameObject go = pool != null ? pool.GetFromPool(prefab) : null;
            if (go == null)
            {
                go = Instantiate(prefab);
            }
            return go;
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
                    if (_actions[i].Spawns[s].Count < 1)
                        _actions[i].Spawns[s].Count = 1;
                }
            }
        }
#endif
    }
}
