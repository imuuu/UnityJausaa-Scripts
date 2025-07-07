using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Game;
using Game.DropSystem;
using Game.PoolSystem;
using Game.StatSystem;
using UnityEngine;

namespace Game.DropSystem
{
    public class ManagerDrops : MonoBehaviour, IEnabled
    {
        public static ManagerDrops Instance { get; private set; }

        [Header("Orb Movement Settings")]
        [SerializeField] private  float _orbMoveSpeed = 8f;
        [SerializeField] private float _activationDistance = 8f;
        [SerializeField] private float _collectionDistance = 1f;

        private LinkedList<IDropOrb> _orbs = new();
        private Transform _player;
        private CancellationTokenSource _cts;
        private Task _loopTask;
        private bool _isEnabled = true;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            Player.AssignTransformWhenAvailable((t) =>
            {
                _player = t;
                OnPlayerStatsUpdated();
            });
            Events.OnPlayableSceneChangeEnter.AddListener(OnPlayableSceneChange);
            Events.OnPlayerStatsUpdated.AddListener(OnPlayerStatsUpdated);
        }

        private bool OnPlayerStatsUpdated()
        {
            _activationDistance = Player.Instance.GetStatValue(STAT_TYPE.DROP_PICK_UP_RANGE);
            return true;
        }

        private bool OnPlayableSceneChange(SCENE_NAME param)
        {
            CleanAllOrbs();
            return true;
        }

        private void OnEnable() => RestartLoop();
        private void OnDisable() { _isEnabled = false; _cts?.Cancel(); }

        private void OnApplicationQuit()
        {
            _cts?.Cancel();
            _cts = null;
            _loopTask = null;
        }

        private void Update()
        {
            if (_isEnabled && (_loopTask == null || _loopTask.IsCompleted))
                RestartLoop();
        }

        private void RestartLoop()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _loopTask = OrbLoop(_cts.Token);
        }

        public void Register(IDropOrb orb, bool returnToPoolOnCollected = true)
        {
            orb.Collected += OnOrbCollected;
            if (returnToPoolOnCollected)
            {
                orb.Collected += HandleReturnToPool;
            }
            _orbs.AddLast(orb);
        }

        private void OnOrbCollected(IDropOrb orb)
        {
            _orbs.Remove(orb);

            orb.Collected -= OnOrbCollected;
            if (orb is DropOrb dropOrb)
            {
                dropOrb.OnCollected();
                dropOrb.Clear();
            }
        }

        private void HandleReturnToPool(IDropOrb orb)
        {
            if (orb is MonoBehaviour mono)
            {
                ManagerPrefabPooler.Instance.ReturnToPool(mono.gameObject);
            }
        }

        private async Task OrbLoop(CancellationToken token)
        {
            try
            {
                while (_isEnabled && !token.IsCancellationRequested)
                {
                    if (ManagerPause.IsPaused())
                    {
                        await Task.Delay(100, token);
                        continue;
                    }

                    var node = _orbs.First;
                    while (node != null)
                    {
                        var next = node.Next;
                        var orb = node.Value;
                        if (_player != null)
                        {
                            float d = Vector3.Distance(orb.Transform.position, _player.position);

                            if (d <= _collectionDistance)
                            {
                                OnOrbCollected(orb);
                            }
                            else if (d <= _activationDistance)
                            {
                                orb.Transform.position = Vector3.MoveTowards(
                                    orb.Transform.position,
                                    _player.position,
                                    _orbMoveSpeed * Time.deltaTime);
                            }
                        }
                        node = next;
                    }

                    await Task.Yield();
                    if (_orbs.Count == 0)
                        await Task.Delay(1000, token);
                }
            }
            catch (OperationCanceledException) { /* clean exit */ }
            catch (Exception ex)
            {
                Debug.LogError($"[ManagerDrops] loop error: {ex}");
            }
        }

        public void CleanAllOrbs()
        {
            foreach (var xpOrb in _orbs)
            {
                if (xpOrb != null)
                {
                    ManagerPrefabPooler.Instance.ReturnToPool(xpOrb.Transform.gameObject);
                }
            }
            _orbs.Clear();
        }

        public bool IsEnabled()
        {
            return _isEnabled;
        }

        public void SetEnable(bool enable)
        {
            _isEnabled = enable;
            if (enable)
            {
                RestartLoop();
            }
            else
            {
                _cts?.Cancel();
                _cts = null;
                _loopTask = null;
            }
        }
    }
}
