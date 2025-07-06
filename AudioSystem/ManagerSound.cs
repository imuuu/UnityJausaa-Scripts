using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly Dictionary<AudioSoundData, SoundEmitterQueue> _frequentBuckets = new ();

        [SerializeField] private SoundEmitter _soundEmitterPrefab;
        [SerializeField] private bool _collectionCheck = true;
        [SerializeField] private int _defaultCapacity = 10;
        [SerializeField] private int _maxPoolSize = 100;
        [SerializeField] private int _maxSoundInstances = 30;

        [SerializeField] private bool _debug = false;

        private void Awake()
        {
            //Events.OnPlayableSceneChange.AddListener(OnPlayableSceneChange);
        }

        private void Start()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializePool();
        }

        private bool OnPlayableSceneChange(SCENE_NAME sceneName)
        {
            ClearSoundEmitters();
            return true;
        }

        public SoundBuilder CreateSound()
        {
            return new SoundBuilder(this);
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
            Debug.Log("[ManagerSounds] PlayableSceneChange: Clearing sound emitters");

            // 1) Stop all emitters without releasing them to the pool
            foreach (var emitter in _activeSoundEmitters)
                emitter.Stop(returnPool: false);

            // 2) Now return them all to the pool (this will remove them from _activeSoundEmitters)
            foreach (var emitter in _activeSoundEmitters.ToArray())
                ReturnToPool(emitter);

            // 3) Clear any lingering trackers
            _activeSoundEmitters.Clear();
            _frequentBuckets.Clear();

            // 4) Clean up custom emitters (if you want to stop & unregister them, too)
            foreach (var custom in _activeSoundEmitterCustom.ToArray())
            {
                custom.Stop(returnPool: false);   // assuming SoundEmitterCustom also has that overload
                UnregisterCustomSoundEmitter(custom);
            }
        }


        /// <summary>
        /// Returns true if we can play (and will stop the oldest if we’re at capacity), false if something went wrong.
        /// </summary>
        public bool CanPlaySound(AudioSoundData soundData)
        {
            // non-frequent sounds always allowed
            if (!soundData.FrequentSound)
                return true;

            // get or create the queue for this clip
            if (!_frequentBuckets.TryGetValue(soundData, out var queue))
            {
                queue = new SoundEmitterQueue(_maxSoundInstances);
                _frequentBuckets[soundData] = queue;
            }

            // if we’re full, pop & stop the oldest emitter (freeing up one slot)
            if (queue.Count >= _maxSoundInstances)
            {
                // if we can dequeue, stop the oldest emitter
                if (!TryDequeueAndStop(queue))
                {
                    // if we couldn't dequeue, we can't play this sound
                    Debug.LogWarning($"Cannot play sound {soundData.name}: max instances reached.");
                    return false;
                }
            }
            {
                var oldest = queue.Dequeue();
                if (oldest != null)
                {
                    oldest.Stop(returnPool: true);
                    return true;
                }
                else
                {
                    // somehow we dequeued a null – bail
                    Debug.LogWarning("Frequent queue returned null emitter");
                    return false;
                }
            }


            // slot available
            return true;
        }
        public SoundEmitter GetSoundEmitter()
        {
            if (_activeSoundEmitters.Count >= _maxSoundInstances)
            {
                Debug.LogWarning("Max sound instances reached. Cannot create more emitters.");
                return null;
            }

            return _soundEmitterPool.Get();
        }

        public void ReturnToPool(SoundEmitter emitter)
        {
            var data = emitter.GetSoundData();
            if (data.FrequentSound && _frequentBuckets.TryGetValue(data, out var bucket))
            {
                // rebuild queue without this emitter
                _frequentBuckets[data]
                    = new Queue<SoundEmitter>(bucket.Where(e => e != emitter));
            }
            _soundEmitterPool.Release(emitter);
        }

        private void OnDestroyPoolObject(SoundEmitter emitter)
        {
            Destroy(emitter.gameObject);
        }

        private void OnReturnedToPool(SoundEmitter emitter)
        {
            emitter.gameObject.SetActive(false);
            _activeSoundEmitters.Remove(emitter);
        }

        private void OnTakeFromPool(SoundEmitter emitter)
        {
            emitter.gameObject.SetActive(true);
            _activeSoundEmitters.Add(emitter);
        }

        private SoundEmitter CreateSoundEmitter()
        {
            SoundEmitter soundEmitter = Instantiate(_soundEmitterPrefab, transform);
            soundEmitter.gameObject.SetActive(false);
            return soundEmitter;
        }

        public bool IsDebug()
        {
            return _debug;
        }

        public void RegisterCustomSoundEmitter(SoundEmitterCustom soundEmitterCustom)
        {
            if (_activeSoundEmitterCustom.Contains(soundEmitterCustom)) return;
            _activeSoundEmitterCustom.Add(soundEmitterCustom);
        }

        public void UnregisterCustomSoundEmitter(SoundEmitterCustom soundEmitterCustom)
        {
            if (!_activeSoundEmitterCustom.Contains(soundEmitterCustom)) return;
            _activeSoundEmitterCustom.Remove(soundEmitterCustom);
        }
    }
    
    // Simple fixed-capacity queue without any Linq or allocations
public class SoundEmitterQueue
{
    private readonly SoundEmitter[] _buffer;
    private int _head;   // index of oldest
    private int _tail;   // next free slot
    private int _count;

    public int Count => _count;

    public SoundEmitterQueue(int capacity)
    {
        _buffer = new SoundEmitter[capacity];
        _head = 0;
        _tail = 0;
        _count = 0;
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
        _buffer[_head] = null; // help GC
        _head = (_head + 1) % _buffer.Length;
        _count--;
        return e;
    }

    public void Remove(SoundEmitter target)
    {
        // find it, then shift everything down
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

        // shift everything between head and tail
        int next = (idx + 1) % _buffer.Length;
        while (idx != _tail)
        {
            _buffer[idx] = _buffer[next];
            idx = next;
            next = (next + 1) % _buffer.Length;
        }
        // back up tail one slot
        _tail = (_tail - 1 + _buffer.Length) % _buffer.Length;
        _buffer[_tail] = null;
        _count--;
    }
}
}