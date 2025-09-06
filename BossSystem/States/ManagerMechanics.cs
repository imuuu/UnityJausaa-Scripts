using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Game.BossSystem
{
    [DefaultExecutionOrder(-1000)]
    public sealed class ManagerMechanics : MonoBehaviour
    {
        public static ManagerMechanics Instance { get; private set; }

        private static readonly Dictionary<int, float> _groupReadyTimes = new Dictionary<int, float>(64);
        private static readonly object _lock = new object();

        // ---------------- Debug/Inspector ----------------
        [FoldoutGroup("Debug"), SerializeField, Tooltip("Write Push/Clear/Prune to console and keep a short history below.")]
        private bool _debugLogOperations = false;

        [FoldoutGroup("Debug"), SerializeField, Tooltip("Automatically remove expired entries (for a tidy view).")]
        private bool _debugAutoPruneExpired = true;

        [FoldoutGroup("Debug"), SerializeField, Min(0.05f), Tooltip("How often to auto-prune expired groups (unscaled time).")]
        private float _debugPruneInterval = 1.0f;

        // ring buffer of recent ops (for inspector)
        private const int _maxRecentOps = 64;
        private static readonly List<string> _recentOps = new List<string>(_maxRecentOps);

        // mirror static flag for logging (since core API is static)
        private static bool s_LogOps = false;

        private float _nextPruneAt;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            s_LogOps = _debugLogOperations;
            _nextPruneAt = Time.unscaledTime + _debugPruneInterval;
            // Optional: DontDestroyOnLoad(gameObject);
        }

        private void OnValidate()
        {
            // keep static in sync with inspector toggle
            s_LogOps = _debugLogOperations;
            if (_debugPruneInterval < 0.05f) _debugPruneInterval = 0.05f;
        }

        private void Update()
        {
            if (!_debugAutoPruneExpired) return;

            float now = Time.unscaledTime;
            if (now >= _nextPruneAt)
            {
                int removed = PruneExpiredInternal();
                if (removed > 0 && s_LogOps)
                    Debug.Log($"[ManagerMechanics] Auto-pruned {removed} expired cooldown group(s).");
                _nextPruneAt = now + _debugPruneInterval;
            }
        }

        // ---------------- Core API ----------------

        /// <summary>Returns true if group has no cooldown or cooldown has expired.</summary>
        public static bool IsGroupReady(int groupId)
        {
            float now = Time.time;
            lock (_lock)
            {
                if (_groupReadyTimes.TryGetValue(groupId, out float readyAt))
                    return now >= readyAt;
                return true;
            }
        }

        /// <summary>Remaining seconds until group is ready (0 when ready).</summary>
        public static float GetRemaining(int groupId)
        {
            float now = Time.time;
            lock (_lock)
            {
                if (_groupReadyTimes.TryGetValue(groupId, out float readyAt))
                    return Mathf.Max(0f, readyAt - now);
                return 0f;
            }
        }

        /// <summary>Push or extend group cooldown: readyAt = max(existing, now + durationSec).</summary>
        public static void PushCooldown(int groupId, float durationSec)
        {
            if (groupId == 0 || durationSec <= 0f) return;

            float now = Time.time;
            float target = now + durationSec;
            float newReady;
            bool extended = false;

            lock (_lock)
            {
                if (_groupReadyTimes.TryGetValue(groupId, out float current))
                {
                    if (target > current)
                    {
                        _groupReadyTimes[groupId] = target; // highest wins
                        extended = true;
                        newReady = target;
                    }
                    else
                    {
                        newReady = current; // unchanged
                    }
                }
                else
                {
                    _groupReadyTimes[groupId] = target;
                    newReady = target;
                    extended = true;
                }
            }

            if (s_LogOps)
            {
                string msg = extended
                    ? $"[ManagerMechanics] PushCooldown gid={groupId} dur={durationSec:0.###}s -> readyAt={newReady:0.###} (t={now:0.###})"
                    : $"[ManagerMechanics] PushCooldown gid={groupId} dur={durationSec:0.###}s -> unchanged (already later)";
                Debug.Log(msg);
                EnqueueRecent(msg);
            }
        }

        public static void ClearGroup(int groupId)
        {
            bool hadEntry;
            lock (_lock) { hadEntry = _groupReadyTimes.Remove(groupId); }

            if (s_LogOps)
            {
                string msg = hadEntry
                    ? $"[ManagerMechanics] ClearGroup gid={groupId} (removed)"
                    : $"[ManagerMechanics] ClearGroup gid={groupId} (no entry)";
                Debug.Log(msg);
                EnqueueRecent(msg);
            }
        }

        public static void ClearAll()
        {
            lock (_lock) { _groupReadyTimes.Clear(); }

            if (s_LogOps)
            {
                const string msg = "[ManagerMechanics] ClearAll (all cooldown groups removed)";
                Debug.Log(msg);
                EnqueueRecent(msg);
            }
        }

        /// <summary>
        /// Get a thread-safe snapshot of all groups.
        /// </summary>
        public static void GetSnapshot(List<(int groupId, float readyAt, float remaining)> buffer)
        {
            if (buffer == null) return;
            buffer.Clear();
            float now = Time.time;

            lock (_lock)
            {
                foreach (var kvp in _groupReadyTimes)
                {
                    float remaining = Mathf.Max(0f, kvp.Value - now);
                    buffer.Add((kvp.Key, kvp.Value, remaining));
                }
            }
        }

        /// <summary>Finds an IOwner root up the hierarchy (max 16 steps).</summary>
        public static Transform FindOwnerRoot(Transform start)
        {
            Transform t = start;
            for (int i = 0; i < 16; i++)
            {
                if (t == null) break;
                if (t.GetComponent<IOwner>() != null) return t;
                t = t.parent;
            }
            return start; // fallback
        }

        public static int ComputeGroupId(Transform from, Transform customTransform, int customInt, CooldownGroupSource source)
        {
            switch (source)
            {
                case CooldownGroupSource.None:
                    return 0; // treated as no grouping
                case CooldownGroupSource.AutoOwnerRoot:
                    {
                        var root = FindOwnerRoot(from);
                        return root != null ? root.gameObject.GetInstanceID() : 0;
                    }
                case CooldownGroupSource.CustomTransform:
                    return customTransform != null ? customTransform.gameObject.GetInstanceID() : 0;
                case CooldownGroupSource.CustomInt:
                    return customInt;
                default:
                    return 0;
            }
        }

        // ---------------- Debug helpers ----------------

        /// <summary>Enable/disable console logging at runtime.</summary>
        public static void SetDebugLogging(bool enabled)
        {
            s_LogOps = enabled;
            if (Instance != null) Instance._debugLogOperations = enabled;
        }

        /// <summary>Manually prune expired groups. Returns count removed.</summary>
        [FoldoutGroup("Debug"), Button, PropertyOrder(100)]
        public int Debug_PruneExpiredNow() => PruneExpiredInternal();

        /// <summary>Dump a snapshot of current groups to the console.</summary>
        [FoldoutGroup("Debug"), Button, PropertyOrder(101)]
        public void Debug_DumpSnapshot()
        {
            var tmp = new List<(int groupId, float readyAt, float remaining)>(64);
            GetSnapshot(tmp);
            if (tmp.Count == 0)
            {
                Debug.Log("[ManagerMechanics] Snapshot: (empty)");
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("[ManagerMechanics] Snapshot:");
            for (int i = 0; i < tmp.Count; i++)
                sb.AppendLine($"  gid={tmp[i].groupId}, readyAt={tmp[i].readyAt:0.###}, remaining={tmp[i].remaining:0.###}s");
            Debug.Log(sb.ToString());
        }

#if UNITY_EDITOR
        // Odin table of groups (read-only)
        [FoldoutGroup("Debug"), ShowInInspector, ReadOnly, PropertyOrder(110)]
        [LabelText("Groups (live)")]
        private List<DebugRow> _groupsView
        {
            get
            {
                var list = _debugListCache ?? (_debugListCache = new List<DebugRow>(64));
                list.Clear();

                float now = Time.time;
                lock (_lock)
                {
                    foreach (var kvp in _groupReadyTimes)
                    {
                        list.Add(new DebugRow
                        {
                            GroupId = kvp.Key,
                            ReadyAt = kvp.Value,
                            Remaining = Mathf.Max(0f, kvp.Value - now)
                        });
                    }
                }
                // sort by remaining desc for readability
                list.Sort((a, b) => b.Remaining.CompareTo(a.Remaining));
                return list;
            }
        }
        private List<DebugRow> _debugListCache;

        // Odin view of recent operations
        [FoldoutGroup("Debug"), ShowInInspector, ReadOnly, PropertyOrder(111)]
        [LabelText("Recent Ops")]
        private string[] _recentOpsView => _recentOps.ToArray();

        [System.Serializable]
        private class DebugRow
        {
            [ShowInInspector, ReadOnly] public int GroupId;
            [ShowInInspector, ReadOnly] public float ReadyAt;
            [ShowInInspector, ReadOnly] public float Remaining;
        }
#endif

        // internal prune
        private static int PruneExpiredInternal()
        {
            int removed = 0;
            float now = Time.time;

            lock (_lock)
            {
                // gather first to avoid modifying during enumeration
                _toRemove ??= new List<int>(32);
                _toRemove.Clear();

                foreach (var kv in _groupReadyTimes)
                {
                    if (now >= kv.Value) _toRemove.Add(kv.Key);
                }

                for (int i = 0; i < _toRemove.Count; i++)
                {
                    _groupReadyTimes.Remove(_toRemove[i]);
                    removed++;
                }
            }

            if (removed > 0 && s_LogOps)
            {
                string msg = $"[ManagerMechanics] Pruned {removed} expired cooldown group(s).";
                Debug.Log(msg);
                EnqueueRecent(msg);
            }

            return removed;
        }

        private static List<int> _toRemove;

        private static void EnqueueRecent(string line)
        {
            _recentOps.Add(line);
            if (_recentOps.Count > _maxRecentOps)
                _recentOps.RemoveAt(0);
        }
    }
}
