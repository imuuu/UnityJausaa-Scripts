using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

[DefaultExecutionOrder(-200)]
public sealed class ManagerBosses : MonoBehaviour
{
    public static ManagerBosses Instance { get; private set; }

    // [SerializeField] private Transform _spawnRoot;
    // [SerializeField] private Transform _arenaRoot;
    [SerializeField] private float _aiTickRate = 8f; // Hz
    [SerializeField] private BossEncounterDefinition[] _bossEncounters;
    private bool[] _encountersTimeSpawned;

    private readonly List<BossController> _activeBosses = new List<BossController>(4);
    private float _aiTickAccum;

    private BossArenaController _arenaController;

    private bool _allowMobSpawn = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        _encountersTimeSpawned = new bool[_bossEncounters.Length];

        Events.OnPlayableSceneChangeEnter.AddListener(OnPlayableSceneChangeEnter);
    }

    private bool OnPlayableSceneChangeEnter(SCENE_NAME param)
    {
        _encountersTimeSpawned = new bool[_bossEncounters.Length];
        return true;
    }

    public void StartEncounter(BossEncounterDefinition enc, Vector3 center)
    {
        if (enc == null) return;

        if (enc.KillAllMobsOnStart)
        {
            _allowMobSpawn = false;
            TryKillAllNonBossMobs();
        }

        GameObject arenaRoot = new GameObject("BossArenaRoot");
        arenaRoot.transform.position = center;

        BossArenaDefinition arenaDef = enc.Bosses[0].Arena;
        if (_arenaController != null) { _arenaController.Despawn(); _arenaController = null; }
        _arenaController = BossArenaController.Spawn(arenaDef, center, arenaRoot.transform);

        _activeBosses.Clear();
        int count = enc.Bosses.Length;
        for (int i = 0; i < count; i++)
        {
            BossDefinition def = enc.Bosses[i];
            BossEncounterDefinition encounter = GetEncounterDefinition(def.MobData.MobType);

            Vector3 spawnPos = GetSpawnPosition(encounter.StartPosition);
            spawnPos.y = 0f;

            GameObject go = Instantiate(def.Prefab, spawnPos, Quaternion.identity);
            BossController ctrl = go.GetComponent<BossController>();
            ctrl.Initialize(def, this);
            _activeBosses.Add(ctrl);
        }
    }

    public void EndEncounter()
    {
        for (int i = 0; i < _activeBosses.Count; i++) if (_activeBosses[i] != null) Destroy(_activeBosses[i].gameObject);
        _activeBosses.Clear();
        if (_arenaController != null)
        {
            _arenaController.Despawn();
            _arenaController = null;
        }
    }

    private void Update()
    {
        if (ManagerPause.IsPaused()) return;

        if (SceneLoader.GetCurrentScene() == SCENE_NAME.Lobby) return;

        CheckEncounterTimers();

        if (_activeBosses.Count == 0) return;

        float dt = Time.deltaTime;
        _aiTickAccum += dt;
        float tickInterval = 1f / Mathf.Max(1f, _aiTickRate);
        if (_aiTickAccum >= tickInterval)
        {
            float step = _aiTickAccum; _aiTickAccum = 0f; // lumped dt so timers remain accurate
            for (int i = 0; i < _activeBosses.Count; i++)
            {
                BossController b = _activeBosses[i]; if (b != null) b.AITick(step);
            }
        }
    }

    public void OnBossDied(BossController who)
    {
        // Remove boss and optionally enrage others
        int idx = _activeBosses.IndexOf(who);

        if (idx >= 0) _activeBosses.RemoveAt(idx);


        if (_activeBosses.Count == 0) EndEncounter();

        _allowMobSpawn = true;
        for (int i = 0; i < _activeBosses.Count; i++)
        {
            BossController b = _activeBosses[i];
            BossEncounterDefinition encounter = GetEncounterDefinition(b.Definition.MobData.MobType);
            if (encounter.KillAllMobsOnStart)
            {
                _allowMobSpawn = false;
                break;
            }
        }
    }

    private void CheckEncounterTimers()
    {
        float currentRoundTime = ManagerGame.Instance.GetRoundTimer().CurrentTime;
        for (int i = 0; i < _bossEncounters.Count(); i++)
        {
            if(_encountersTimeSpawned[i]) continue;

            BossEncounterDefinition encounter = _bossEncounters[i];
            float startTime = encounter.SpawnTimeSeconds;
            if (startTime > 0f && currentRoundTime >= startTime)
            {
                Transform playerTransform = Player.Instance.transform;
                StartEncounter(encounter, playerTransform.position);
                _encountersTimeSpawned[i] = true;
            }
        }
    }

    public bool IsInsideArena(Vector3 pos) { return _arenaController != null && _arenaController.IsInside(pos); }

    private void TryKillAllNonBossMobs()
    {
        ManagerMob.Instance.DespawnAllNonBossMobs();
        // Hook to your enemy manager. Stub:
        //var enemyMgr = FindObjectOfType<ManagerEnemies>();
        //if (enemyMgr != null) enemyMgr.DespawnAllNonBossEnemies();
    }

    public Vector3 GetSpawnPosition(START_POSITION startPos)
    {
        Vector3 playerPos = Player.Instance.transform.position;
        switch (startPos)
        {
            case START_POSITION.FRONT_PLAYER:
                return playerPos + new Vector3(0f, 0f, 15f);
            case START_POSITION.AROUND_PLAYER:
                return playerPos + Random.insideUnitSphere * 15f;
            default:
                return playerPos;
        }
    }

    private BossEncounterDefinition GetEncounterDefinition(MOB_TYPE type)
    {
        foreach (BossEncounterDefinition encounter in _bossEncounters)
        {
            if (encounter.Bosses.Any(b => b.MobData.MobType == type))
            {
                return encounter;
            }
        }
        return null;
    }

    public bool IsMobSpawnAllowed()
    {
        return _allowMobSpawn;
    }

    

    [Button("Spawn Boss (To Player)")]
    private void SpawnBoss(MOB_TYPE type)
    {
        BossEncounterDefinition encounter = GetEncounterDefinition(type);
        if (encounter == null) return;

        Transform playerTransform = Player.Instance.transform;

        StartEncounter(encounter, playerTransform.position);
    }
}