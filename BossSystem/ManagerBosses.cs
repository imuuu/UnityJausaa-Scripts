using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;


[DefaultExecutionOrder(-200)]
public sealed class ManagerBosses : MonoBehaviour
{
    public static ManagerBosses Instance { get; private set; }

    [Header("Encounters")]
    [SerializeField] private BossEncounterDefinition[] _bossEncounters;

    [Header("AI")]
    [SerializeField, Min(1f), Tooltip("How many times per second bosses get their AITick.")]
    private float _aiTickRate = 8f;

    // ---------- Runtime state ----------
    private bool[] _encountersLaunched;
    private readonly List<BossController> _activeBosses = new(4);
    private readonly List<BossArenaController> _preplacedArenas = new();
    private readonly List<BossEncounterProximityTrigger> _proximityTriggers = new();

    private BossArenaController _activeArena;
    private float _aiTickAccum;
    private bool _allowMobSpawn = true;
    private bool _registryInitialized;

    // ---------- Debug ----------
    [FoldoutGroup("Debug/Logs"), SerializeField] private bool _log = false;
    [FoldoutGroup("Debug/Runtime"), ShowInInspector, ReadOnly] private string _lastStartLog = "(none)";

    private void Log(string msg)
    {
        _lastStartLog = msg;
        if (_log) Debug.Log($"[ManagerBosses] {msg}");
    }

    // ============================================================
    //                         LIFECYCLE
    // ============================================================

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        _encountersLaunched = new bool[_bossEncounters != null ? _bossEncounters.Length : 0];

        Events.OnPlayableSceneChangeEnter.AddListener(OnPlayableSceneChangeEnter);
    }

    private void Start()
    {
        RebuildPreplacedArenaRegistry();
        _registryInitialized = true;

        if (_preplacedArenas.Count > 0)
        {
            Log($"Found {_preplacedArenas.Count} pre-placed arena(s) on Start. Closest: {FindClosestArena(PlayerPos())?.name ?? "(none)"}");
        }
        else
        {
            Log("No pre-placed arenas found on Start.");
        }
    }

    private bool OnPlayableSceneChangeEnter(SCENE_NAME _)
    {
        _encountersLaunched = new bool[_bossEncounters != null ? _bossEncounters.Length : 0];
        ClearActiveEncounter();
        ClearAllProximityTriggers();
        _preplacedArenas.Clear();
        _registryInitialized = false;
        return true;
    }

    /// <summary>
    /// Called by a BossController when it dies. Removes it from the active list
    /// and ends the encounter if that was the last boss.
    /// </summary>
    public void OnBossDied(BossController who)
    {
        // Remove the reference (and any nulls that might be left behind)
        if (who != null)
        {
            int idx = _activeBosses.IndexOf(who);
            if (idx >= 0) _activeBosses.RemoveAt(idx);
        }
        _activeBosses.RemoveAll(b => b == null);

        if (_log)
            Debug.Log($"[ManagerBosses] Boss died: {(who ? who.name : "null")} | remaining={_activeBosses.Count}");

        // If no bosses remain, end the encounter (clears arena, resets flags)
        if (_activeBosses.Count == 0)
        {
            Log("All bosses defeated. Ending encounter.");
            EndEncounter();
            return;
        }

        // If you disabled mob spawns at start, you can re-enable based on your rules here.
        // Keeping current behavior consistent with earlier logic:
        _allowMobSpawn = !_bossEncounters.Any(e => e != null && e.KillAllMobsOnStart);
    }

    private void Update()
    {
        if (ManagerPause.IsPaused()) return;
        if (SceneLoader.GetCurrentScene() == SCENE_NAME.Lobby) return;

        // If something registered after us, ensure we have them
        if (!_registryInitialized)
        {
            RebuildPreplacedArenaRegistry();
            _registryInitialized = true;
            Log($"Registry initialized in Update: {_preplacedArenas.Count} arena(s).");
        }

        CheckEncounterStarts();

        // Boss AI tick
        if (_activeBosses.Count > 0)
        {
            float dt = Time.deltaTime;
            _aiTickAccum += dt;
            float tickInterval = 1f / Mathf.Max(1f, _aiTickRate);
            if (_aiTickAccum >= tickInterval)
            {
                float step = _aiTickAccum;
                _aiTickAccum = 0f;
                for (int i = 0; i < _activeBosses.Count; i++)
                {
                    var b = _activeBosses[i];
                    if (b != null) b.AITick(step);
                }
            }
        }

        // Arena enforcement (player minimum)
        if (_activeArena != null && Player.Instance != null)
            _activeArena.EnforceFor(Player.Instance.transform, Time.deltaTime);
    }

    // ============================================================
    //                     ARENA REGISTRY (PRE-PLACED)
    // ============================================================

    public void RegisterPreplacedArena(BossArenaController arena)
    {

        if (arena == null) return;

        Debug.Log("RegisterPreplacedArena: " + arena.name);

        if (!_preplacedArenas.Contains(arena))
        {
            _preplacedArenas.Add(arena);
            if (_log) Debug.Log($"[ManagerBosses] Registered arena: {arena.name} @ {arena.GetCenter()}");
        }
        else
        {
            Debug.LogWarning($"[ManagerBosses] Arena already registered: {arena.name}");
        }
    }

    public void UnregisterPreplacedArena(BossArenaController arena)
    {
        if (arena == null) return;
        _preplacedArenas.Remove(arena);
        if (_activeArena == arena) _activeArena = null;
        if (_log) Debug.Log($"[ManagerBosses] Unregistered arena: {arena.name}");
    }

    [Button("Rebuild Arena Registry"), FoldoutGroup("Debug/Actions")]
    private void RebuildPreplacedArenaRegistry()
    {
        _preplacedArenas.Clear();
        foreach (var a in Object.FindObjectsByType<BossArenaController>(FindObjectsSortMode.None))
            if (a != null) _preplacedArenas.Add(a);
    }
    

    private BossArenaController FindClosestArena(Vector3 pos)
    {
        if (_preplacedArenas.Count == 0) return null;

        float best = float.PositiveInfinity;
        BossArenaController bestA = null;
        for (int i = 0; i < _preplacedArenas.Count; i++)
        {
            var a = _preplacedArenas[i];
            if (a == null) continue;
            float d = (a.GetCenter() - pos).sqrMagnitude;
            if (d < best) { best = d; bestA = a; }
        }
        return bestA;
    }

    private void UseActiveArena(BossArenaController arena)
    {
        if (arena == null) return;
        if (_activeArena != null && _activeArena != arena)
            _activeArena.Deactivate();

        _activeArena = arena;
        _activeArena.Activate();

        Log($"Using pre-placed arena: {_activeArena.name} @ {_activeArena.GetCenter()}");
    }

    

    /// <summary>
    /// Prefer an existing pre-placed arena. Spawn dynamically only if encounter allows it and none exists.
    /// </summary>
    private void EnsureActiveArena(BossEncounterDefinition enc, Vector3 center)
    {
        var closest = FindClosestArena(center);
        if (closest != null)
        {
            UseActiveArena(closest);
            return;
        }

        if (enc != null && enc.SpawnArenaOnStart && enc.Bosses.Length > 0 && enc.Bosses[0]?.Arena != null)
        {
            _activeArena = BossArenaController.Spawn(enc.Bosses[0].Arena, center, null);
            Log($"Spawned dynamic arena at {center}");
        }
        else
        {
            Log("No arena available (and dynamic spawning disabled). Proceeding without arena.");
        }
    }

    // ============================================================
    //                         ENCOUNTERS
    // ============================================================

    private void CheckEncounterStarts()
    {
        if (_bossEncounters == null || _bossEncounters.Length == 0) return;

        float t = ManagerGame.Instance.GetRoundTimer().CurrentTime;
        Vector3 playerPos = PlayerPos();

        for (int i = 0; i < _bossEncounters.Length; i++)
        {
            if (_encountersLaunched[i]) continue;

            var enc = _bossEncounters[i];
            if (enc == null) { _encountersLaunched[i] = true; continue; }

            float startTime = enc.SpawnTimeSeconds;

            // --- Pure proximity encounter (no timer) ---
            if (startTime < 0f && enc.EnableBossBehaviorOnRange && enc.StartPosition == START_POSITION.FIND_BOSS_CLOSEST_ARENA)
            {
                if (_preplacedArenas.Count > 0)
                {
                    // Visualize arenas and arm proximity triggers at each
                    foreach (var a in _preplacedArenas) if (a != null) a.Activate();
                    ArmProximityAtAllPreplacedArenas(enc);
                    Log($"Armed proximity at {_preplacedArenas.Count} pre-placed arena(s). Range={enc.BossBehaviorRange}");
                }
                else
                {
                    // No pre-placed; spawn one if allowed, then arm
                    EnsureActiveArena(enc, playerPos);
                    if (_activeArena != null)
                    {
                        ArmProximityAtPoint(enc, _activeArena.GetCenter());
                        Log($"Armed proximity at dynamic arena @ {_activeArena.GetCenter()}");
                    }
                }

                if (enc.KillAllMobsOnStart)
                {
                    _allowMobSpawn = false;
                    TryKillAllNonBossMobs();
                }

                _encountersLaunched[i] = true; // armed
                continue;
            }

            // --- Time-based start (NOW supports 0) ---
            if (startTime >= 0f && t >= startTime)
            {
                if (enc.StartPosition == START_POSITION.FIND_BOSS_CLOSEST_ARENA)
                {
                    Vector3 desiredCenter = playerPos;
                    EnsureActiveArena(enc, desiredCenter);

                    if (enc.EnableBossBehaviorOnRange)
                    {
                        Vector3 center = _activeArena != null ? _activeArena.GetCenter() : desiredCenter;
                        ArmProximityAtPoint(enc, center);
                        Log($"Armed time-based proximity at {center} (range={enc.BossBehaviorRange})");
                    }
                    else
                    {
                        Vector3 center = _activeArena != null ? _activeArena.GetCenter() : desiredCenter;
                        Log($"Starting encounter (time-based) at {center}");
                        StartEncounter(enc, center: center);
                    }
                }
                else
                {
                    Vector3 center = playerPos;
                    EnsureActiveArena(enc, center);
                    Log($"Starting encounter (mode {enc.StartPosition}) at {center}");
                    StartEncounter(enc, center);
                }

                if (enc.KillAllMobsOnStart)
                {
                    _allowMobSpawn = false;
                    TryKillAllNonBossMobs();
                }

                _encountersLaunched[i] = true;
            }
        }
    }

    public void StartEncounter(BossEncounterDefinition enc, Vector3 center)
    {
        Debug.Log($"StartEncounter: {enc} at {center}");
        if (enc == null) return;

        if (enc.KillAllMobsOnStart)
        {
            _allowMobSpawn = false;
            TryKillAllNonBossMobs();
        }

        EnsureActiveArena(enc, center);

        Vector3 spawnBase = (_activeArena != null)
            ? _activeArena.GetBossSpawnWorldPosition()
            : center;

        _activeBosses.Clear();
        for (int i = 0; i < enc.Bosses.Length; i++)
        {
            var def = enc.Bosses[i];
            if (def == null || def.Prefab == null) { Log($"Boss {i} skipped (missing def or prefab)"); continue; }

            Vector3 spawnPos = (enc.StartPosition == START_POSITION.FIND_BOSS_CLOSEST_ARENA)
                ? spawnBase
                : GetSpawnPosition(enc.StartPosition, _activeArena != null ? _activeArena.GetCenter() : center);

            spawnPos.y = 0f;

            var go = Instantiate(def.Prefab, spawnPos, Quaternion.identity);
            var ctrl = go.GetComponent<BossController>();
            ctrl.Initialize(def, this);
            _activeBosses.Add(ctrl);

            Events.OnBossSpawned.Invoke(ctrl);
            Log($"Spawned boss '{def.Prefab.name}' at {spawnPos}");
        }
    }

    public void EndEncounter()
    {
        ClearActiveEncounter();
    }

    private void ClearActiveEncounter()
    {
        for (int i = 0; i < _activeBosses.Count; i++)
            if (_activeBosses[i] != null)
                Destroy(_activeBosses[i].gameObject);
        _activeBosses.Clear();

        if (_activeArena != null)
        {
            _activeArena.Deactivate();
            _activeArena = null;
        }

        _allowMobSpawn = true;
        Log("Encounter cleared.");
    }

    public void OnProximityTriggered(BossEncounterDefinition enc, Vector3 center)
    {
        var closest = FindClosestArena(center);
        if (closest != null) UseActiveArena(closest);

        Log($"|||Proximity triggered at {center}");
        StartEncounter(enc, center);
        ClearAllProximityTriggers();
    }

    private void TryKillAllNonBossMobs()
    {
        ManagerMob.Instance.DespawnAllNonBossMobs();
    }

    public Vector3 GetSpawnPosition(START_POSITION startPos, Vector3 arenaCenter)
    {
        Vector3 playerPos = PlayerPos();

        switch (startPos)
        {
            case START_POSITION.FRONT_PLAYER:
                return playerPos + new Vector3(0f, 0f, 15f);

            case START_POSITION.AROUND_PLAYER:
                return playerPos + Random.insideUnitSphere * 15f;

            case START_POSITION.FIND_BOSS_CLOSEST_ARENA:
                if (_activeArena != null)
                    return _activeArena.GetBossSpawnWorldPosition();
                return arenaCenter;
        }
        return playerPos;
    }

    public bool IsInsideArena(Vector3 pos)
    {
        return _activeArena != null && _activeArena.IsInside(pos);
    }

    public bool IsMobSpawnAllowed() => _allowMobSpawn;

    private Vector3 PlayerPos()
    {
        return Player.Instance != null ? Player.Instance.transform.position : Vector3.zero;
    }

    // ============================================================
    //                     PROXIMITY TRIGGERS
    // ============================================================

    private void ArmProximityAtAllPreplacedArenas(BossEncounterDefinition enc)
    {
        for (int i = 0; i < _preplacedArenas.Count; i++)
        {
            var a = _preplacedArenas[i];
            if (a == null) continue;
            ArmProximityAtPoint(enc, a.GetCenter());
        }
    }

    private void ArmProximityAtPoint(BossEncounterDefinition enc, Vector3 center)
    {
        var trigger = BossEncounterProximityTrigger.Create(this, enc, center, Mathf.Max(0.1f, enc.BossBehaviorRange));
        _proximityTriggers.Add(trigger);
    }

    private void ClearAllProximityTriggers()
    {
        for (int i = 0; i < _proximityTriggers.Count; i++)
            if (_proximityTriggers[i] != null)
                Destroy(_proximityTriggers[i].gameObject);
        _proximityTriggers.Clear();
    }

    // ============================================================
    //                           DEBUG UI
    // ============================================================

    [FoldoutGroup("Debug/Runtime"), ShowInInspector, ReadOnly]
    private string ActiveArena =>
        _activeArena == null ? "(none)" : $"{_activeArena.name} @ {_activeArena.GetCenter()}";

    [System.Serializable]
    public struct ArenaDebugRow
    {
        [ReadOnly] public string Name;
        [ReadOnly] public Vector3 Position;
        [ReadOnly] public float DistanceToPlayer;
        [ReadOnly] public bool IsActive;
    }

    [FoldoutGroup("Debug/Runtime"), ShowInInspector, ReadOnly]
    private List<ArenaDebugRow> CurrentArenas
    {
        get
        {
            var rows = new List<ArenaDebugRow>(_preplacedArenas.Count);
            Vector3 player = PlayerPos();

            for (int i = 0; i < _preplacedArenas.Count; i++)
            {
                var a = _preplacedArenas[i];
                if (a == null) continue;

                rows.Add(new ArenaDebugRow
                {
                    Name = a.name,
                    Position = a.GetCenter(),
                    DistanceToPlayer = Vector3.Distance(player, a.GetCenter()),
                    IsActive = ReferenceEquals(a, _activeArena)
                });
            }
            return rows.OrderBy(r => r.DistanceToPlayer).ToList();
        }
    }

    [FoldoutGroup("Debug/Runtime"), ShowInInspector, ReadOnly]
    private int ActiveBossCount => _activeBosses.Count;

    [FoldoutGroup("Debug/Runtime"), ShowInInspector, ReadOnly]
    private int PreplacedArenaCount => _preplacedArenas.Count;

    [FoldoutGroup("Debug/Runtime"), ShowInInspector, ReadOnly]
    private int ProximityTriggersCount => _proximityTriggers.Count;

    [Button("Log Arenas To Console"), FoldoutGroup("Debug/Actions")]
    private void LogArenas()
    {
        if (_preplacedArenas.Count == 0)
        {
            Debug.Log("[ManagerBosses] No pre-placed arenas registered.");
            return;
        }

        var lines = new System.Text.StringBuilder();
        lines.AppendLine("[ManagerBosses] Registered arenas:");
        for (int i = 0; i < _preplacedArenas.Count; i++)
        {
            var a = _preplacedArenas[i];
            if (a == null) continue;
            float dist = Vector3.Distance(PlayerPos(), a.GetCenter());
            lines.AppendLine($"  [{i}] {a.name} pos={a.GetCenter()} distToPlayer={dist:0.0} active={(a == _activeArena)}");
        }
        Debug.Log(lines.ToString());
    }
}
