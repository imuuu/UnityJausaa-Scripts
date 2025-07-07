using System;
using System.Collections.Generic;
using Nova;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.AudioSystem
{
    [DefaultExecutionOrder(-100)]
    public class ManagerSound : MonoBehaviour
    {
        public static ManagerSound Instance { get; private set; }

        private IObjectPool<SoundEmitter> _soundEmitterPool;
        [ShowInInspector] private readonly List<SoundEmitter> _activeSoundEmitters = new();
        [ShowInInspector] private readonly List<SoundEmitterCustom> _activeSoundEmitterCustom = new();

        private readonly Dictionary<AudioSoundData, SoundEmitterQueue> _frequentQueues
            = new Dictionary<AudioSoundData, SoundEmitterQueue>();

        [SerializeField] private SoundEmitter _soundEmitterPrefab;
        [SerializeField] private bool _collectionCheck = true;
        [SerializeField] private int _defaultCapacity = 10;
        [SerializeField] private int _maxPoolSize = 100;
        [SerializeField] private int _maxSoundInstances = 30;
        [SerializeField] private bool _debug = false;

        private void Awake()
        {
            Application.runInBackground = true;
        }

        private void Start()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }
            Events.OnPlayableScenePreloadReady.AddListener(OnPlayableSceneChange);
            InitializePool();
        }

        private bool OnPlayableSceneChange(SCENE_NAME param)
        {
            ClearSoundEmitters();
            return true;
        }

        private void InitializePool()
        {
            _soundEmitterPool = new ObjectPool<SoundEmitter>(
                CreateSoundEmitter,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPoolObject,
                _collectionCheck,
                _defaultCapacity,
                _maxPoolSize);
        }

        [Button]
        private void ClearSoundEmitters()
        {
            Debug.Log("[ManagerSounds] Clearing all sound emitters");

            foreach (var e in _activeSoundEmitters)
                e.Stop(returnPool: false);

            foreach (var e in _activeSoundEmitters.ToArray())
                ReturnToPool(e);

            _activeSoundEmitters.Clear();
            _frequentQueues.Clear();

            foreach (var custom in _activeSoundEmitterCustom.ToArray())
            {
                custom.Stop(returnPool: false);
                UnregisterCustomSoundEmitter(custom);
            }
        }

        public SoundBuilder CreateSound()
            => new SoundBuilder(this);

        /// <summary>
        /// Should we evict the oldest “frequent” emitter so you can play again?
        /// </summary>
        public bool CanPlaySound(AudioSoundData data)
        {
            if (!data.FrequentSound) return true;

            var queue = GetOrCreateQueue(data);
            if (queue.Count >= _maxSoundInstances)
            {
                var oldest = queue.Dequeue();
                if (oldest != null)
                {
                    oldest.Stop(returnPool: true);
                    return true;
                }
                Debug.LogWarning("Frequent queue returned null emitter");
                return false;
            }
            return true;
        }

        public SoundEmitter GetSoundEmitter()
        {
            if (_activeSoundEmitters.Count >= _maxSoundInstances)
            {
                Debug.LogWarning("Max global sound instances reached");
                return null;
            }
            return _soundEmitterPool.Get();
        }

        /// <summary>
        /// Call this immediately after you Get() and Play() a frequent emitter.
        /// </summary>
        public void RegisterFrequentEmitter(SoundEmitter emitter)
        {
            var data = emitter.GetSoundData();
            if (!data.FrequentSound) return;
            GetOrCreateQueue(data).Enqueue(emitter);
        }

        public void ReturnToPool(SoundEmitter emitter)
        {
            var data = emitter.GetSoundData();
            if (data.FrequentSound && _frequentQueues.TryGetValue(data, out var q))
                q.Remove(emitter);

            _soundEmitterPool.Release(emitter);
        }

        private void OnDestroyPoolObject(SoundEmitter e) => Destroy(e.gameObject);

        private void OnReturnedToPool(SoundEmitter e)
        {
            e.gameObject.SetActive(false);
            _activeSoundEmitters.Remove(e);
        }

        private void OnTakeFromPool(SoundEmitter e)
        {
            e.gameObject.SetActive(true);
            _activeSoundEmitters.Add(e);
        }

        private SoundEmitter CreateSoundEmitter()
        {
            var e = Instantiate(_soundEmitterPrefab, transform);
            e.gameObject.SetActive(false);
            return e;
        }

        private SoundEmitterQueue GetOrCreateQueue(AudioSoundData data)
        {
            if (!_frequentQueues.TryGetValue(data, out var q))
            {
                q = new SoundEmitterQueue(_maxSoundInstances);
                _frequentQueues[data] = q;
            }
            return q;
        }

        public bool IsDebug() => _debug;

        public void RegisterCustomSoundEmitter(SoundEmitterCustom c)
        {
            if (!_activeSoundEmitterCustom.Contains(c))
                _activeSoundEmitterCustom.Add(c);
        }

        public void UnregisterCustomSoundEmitter(SoundEmitterCustom c)
        {
            if (_activeSoundEmitterCustom.Contains(c))
                _activeSoundEmitterCustom.Remove(c);
        }
    }

    /// <summary>
    /// Fixed-capacity ring buffer for SoundEmitter, no LINQ or extra allocations.
    /// </summary>
    public class SoundEmitterQueue
    {
        private readonly SoundEmitter[] _buffer;
        private int _head, _tail, _count;

        public int Count => _count;

        public SoundEmitterQueue(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            _buffer = new SoundEmitter[capacity];
            _head = _tail = _count = 0;
        }

        public void Enqueue(SoundEmitter e)
        {
            if (_count == _buffer.Length)
                throw new InvalidOperationException("Queue is full");
            _buffer[_tail] = e;
            _tail = (_tail + 1) % _buffer.Length;
            _count++;
        }

        public SoundEmitter Dequeue()
        {
            if (_count == 0) return null;
            var e = _buffer[_head];
            _buffer[_head] = null;
            _head = (_head + 1) % _buffer.Length;
            _count--;
            return e;
        }

        public void Remove(SoundEmitter target)
        {
            if (_count == 0) return;
            // find it
            int idx = -1;
            for (int i = 0; i < _count; i++)
            {
                int pos = (_head + i) % _buffer.Length;
                if (_buffer[pos] == target)
                {
                    idx = pos;
                    break;
                }
            }
            if (idx < 0) return;
            // shift down
            int next = (idx + 1) % _buffer.Length;
            while (idx != _tail)
            {
                _buffer[idx] = _buffer[next];
                idx = next;
                next = (next + 1) % _buffer.Length;
            }
            // back up tail one
            _tail = (_tail - 1 + _buffer.Length) % _buffer.Length;
            _buffer[_tail] = null;
            _count--;
        }
    }
}
