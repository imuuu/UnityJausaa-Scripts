using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Sirenix.OdinInspector;
using Game.UI; // optional, safe to remove

/// <summary>
/// Listens to Events.OnBossSpawned and manages one UI_BossBar per boss.
/// - Instantiates bars under _container (or this.transform if null)
/// - Binds to IHealth found on the BossController (self/parents/children fallback)
/// - Updates bar.Percent every frame using a cached getter (no per-frame reflection)
/// - Removes the bar when the boss/health is gone or HP reaches 0
/// - MAYBE CHANGE THIS IN FUTURE, TOO COMPLEX for simple task xD.
/// </summary>
public sealed class UI_BossBarsController : MonoBehaviour
{
    [Header("Prefab & Parent")]
    [SerializeField] private UI_BossBar _bossBarPrefab;
    [SerializeField] private Transform _container;

    [Header("Removal")]
    [SerializeField, Min(0f)] private float _removeDelayOnDeath = 0.5f;

    // Internal bookkeeping; key = BossController instanceID
    private readonly Dictionary<int, int> _indexByBossId = new Dictionary<int, int>(8);
    private readonly List<Entry> _entries = new List<Entry>(8);

    private void Awake()
    {
        if (_container == null) _container = transform;
    }

    private void OnEnable()
    {
        Events.OnBossSpawned.AddListener(OnBossSpawned);
    }

    private void OnDisable()
    {
        Events.OnBossSpawned.RemoveListener(OnBossSpawned);
        // Optional: clear immediately on disable
        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            if (e.Bar != null) Destroy(e.Bar.gameObject);
        }
        _entries.Clear();
        _indexByBossId.Clear();
    }

    private bool OnBossSpawned(BossController boss)
    {
        if (boss == null) return false;

        int id = boss.GetInstanceID();
        if (_indexByBossId.ContainsKey(id))
            return true; // already have a bar for this boss

        // Instantiate bar
        UI_BossBar bar = Instantiate(_bossBarPrefab, _container);
        bar.gameObject.SetActive(true);

        // Resolve IHealth (prefer on BossController, fallback to search)
        IHealth health = boss.GetComponent<IHealth>();
        if (health == null)
        {
            // Fallback: try UI_ProgressBar helper search (upwards)
            health = bar.FindHealthComponent(boss.gameObject);
            if (health == null)
            {
                // As a last resort, scan children (non-alloc loop)
                Transform t = boss.transform;
                for (int i = 0, c = t.childCount; i < c && health == null; i++)
                    health = t.GetChild(i).GetComponentInChildren<IHealth>(true);
            }
        }

        bar.SetBossName(boss.Definition.MobData.MobName);

        // Build cached percent getter (reflection only once per health type)
        Func<object, float> percentGetter = HealthAccessorCache.GetPercentGetter(health);
        if (percentGetter == null)
        {
            Debug.LogWarning("[UI_BossBarsController] Could not build health percent getter. Bar will stay full.");
            percentGetter = _ => 1f;
        }

        // Init entry
        var entry = new Entry
        {
            BossId = id,
            Boss = boss,
            HealthObj = (object)health,
            Bar = bar,
            PercentGetter = percentGetter,
            RemoveAtTime = -1f
        };

        _indexByBossId[id] = _entries.Count;
        _entries.Add(entry);

        // Force initial visual
        TryUpdateEntry(_entries.Count - 1, Time.unscaledTime);

        return true;
    }

    private void Update()
    {
        float now = Time.unscaledTime; // UI often uses unscaled time
        // Manual for-loop; may remove entries in-place
        for (int i = 0; i < _entries.Count; /* i incremented inside */)
        {
            // If TryUpdateEntry requests removal, we remove without incrementing i
            if (TryUpdateEntry(i, now))
            {
                i++;
            }
        }
    }

    /// <summary>
    /// Updates one entry. Returns true if the entry remains, false if it was removed.
    /// </summary>
    private bool TryUpdateEntry(int index, float now)
    {
        if ((uint)index >= (uint)_entries.Count) return false;

        Entry e = _entries[index];

        // If boss or bar is gone -> remove immediately
        if (e.Boss == null || e.Bar == null)
        {
            RemoveAt(index);
            return false;
        }

        // If health object missing, keep bar at 100% but schedule removal if configured
        if (e.HealthObj == null)
        {
            e.Bar.Percent = 1f;
            // nothing else to do
            _entries[index] = e;
            return true;
        }

        // Sample percent via cached getter (clamped)
        float p = Mathf.Clamp01(e.PercentGetter(e.HealthObj));
        if (Mathf.Abs(p - e.Bar.Percent) > 0.0001f)
            e.Bar.Percent = p;

        // Death/removal handling
        if (p <= 0f)
        {
            if (e.RemoveAtTime < 0f)
            {
                e.RemoveAtTime = now + _removeDelayOnDeath;
                _entries[index] = e;
                return true;
            }

            if (now >= e.RemoveAtTime)
            {
                RemoveAt(index);
                return false;
            }
        }
        else
        {
            // If alive again, clear pending removal timer
            if (e.RemoveAtTime >= 0f)
            {
                e.RemoveAtTime = -1f;
                _entries[index] = e;
            }
        }

        return true;
    }

    private void RemoveAt(int index)
    {
        Entry e = _entries[index];

        if (e.Bar != null)
            Destroy(e.Bar.gameObject);

        if (_indexByBossId.TryGetValue(e.BossId, out int _))
            _indexByBossId.Remove(e.BossId);

        int lastIdx = _entries.Count - 1;
        if (index != lastIdx)
        {
            // Move last into removed slot
            Entry moved = _entries[lastIdx];
            _entries[index] = moved;
            _indexByBossId[moved.BossId] = index;
        }

        _entries.RemoveAt(lastIdx);
    }

    // ---- Internal types ----

    private struct Entry
    {
        public int BossId;
        public BossController Boss;
        public object HealthObj;                // keep as object; getter knows how to read it
        public UI_BossBar Bar;
        public Func<object, float> PercentGetter;
        public float RemoveAtTime;              // -1 = not scheduled
    }

    /// <summary>
    /// Caches "how to read percent" for each health type (reflection one-time per type).
    /// Looks for common method/property names, or Current/Max pairs.
    /// </summary>
    private static class HealthAccessorCache
    {
        private static readonly Dictionary<Type, Func<object, float>> _cache = new Dictionary<Type, Func<object, float>>(16);

        public static Func<object, float> GetPercentGetter(object health)
        {
            if (health == null) return null;

            Type t = health.GetType();
            if (_cache.TryGetValue(t, out var getter))
                return getter;

            getter = BuildGetter(t);
            _cache[t] = getter;
            return getter;
        }

        private static Func<object, float> BuildGetter(Type t)
        {
            const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // 1) Methods that directly return [0..1] percent
            string[] percentMethods =
            {
                "GetHealthPercent", "GetPercent", "GetHealthNormalized",
                "GetNormalized", "GetNormalizedHealth", "GetHealthRatio", "GetHPPercent"
            };
            for (int i = 0; i < percentMethods.Length; i++)
            {
                var m = t.GetMethod(percentMethods[i], BF, null, Type.EmptyTypes, null);
                if (m != null && (m.ReturnType == typeof(float) || m.ReturnType == typeof(double)))
                    return (obj) => Convert.ToSingle(m.Invoke(obj, null));
            }

            // 2) Properties that directly expose [0..1] percent
            string[] percentProps = { "HealthPercent", "Percent", "Normalized", "NormalizedHealth", "HealthRatio", "HPPercent" };
            for (int i = 0; i < percentProps.Length; i++)
            {
                var p = t.GetProperty(percentProps[i], BF);
                if (p != null && (p.PropertyType == typeof(float) || p.PropertyType == typeof(double)))
                    return (obj) => Convert.ToSingle(p.GetValue(obj, null));
            }

            // 3) Current/Max pairs -> compute percent
            string[] curNames = { "CurrentHealth", "Health", "Current", "HP" };
            string[] maxNames = { "MaxHealth", "HealthMax", "Max", "MaxHP" };

            // Try property pairs
            for (int ci = 0; ci < curNames.Length; ci++)
            {
                var curP = t.GetProperty(curNames[ci], BF);
                if (curP == null) continue;

                for (int mi = 0; mi < maxNames.Length; mi++)
                {
                    var maxP = t.GetProperty(maxNames[mi], BF);
                    if (maxP == null) continue;

                    if (IsNumeric(curP.PropertyType) && IsNumeric(maxP.PropertyType))
                    {
                        return (obj) =>
                        {
                            float cur = ToFloat(curP.GetValue(obj, null));
                            float max = ToFloat(maxP.GetValue(obj, null));
                            return max > 0f ? Mathf.Clamp01(cur / max) : 0f;
                        };
                    }
                }
            }

            // Try method pairs
            for (int ci = 0; ci < curNames.Length; ci++)
            {
                var curM = t.GetMethod("Get" + curNames[ci], BF, null, Type.EmptyTypes, null);
                if (curM == null) continue;

                for (int mi = 0; mi < maxNames.Length; mi++)
                {
                    var maxM = t.GetMethod("Get" + maxNames[mi], BF, null, Type.EmptyTypes, null);
                    if (maxM == null) continue;

                    if (IsNumeric(curM.ReturnType) && IsNumeric(maxM.ReturnType))
                    {
                        return (obj) =>
                        {
                            float cur = ToFloat(curM.Invoke(obj, null));
                            float max = ToFloat(maxM.Invoke(obj, null));
                            return max > 0f ? Mathf.Clamp01(cur / max) : 0f;
                        };
                    }
                }
            }

            // Fallback: always full (prevents NaNs)
            return _ => 1f;
        }

        private static bool IsNumeric(Type t)
        {
            return t == typeof(float) || t == typeof(double) || t == typeof(int) || t == typeof(uint) ||
                   t == typeof(long) || t == typeof(ulong) || t == typeof(short) || t == typeof(ushort) ||
                   t == typeof(byte) || t == typeof(sbyte);
        }

        private static float ToFloat(object v) => Convert.ToSingle(v);
    }

    // ------- Optional debug helpers (Odin buttons) -------

    [Button, DisableInEditorMode]
    private void ClearAll()
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
            RemoveAt(i);
    }
}
