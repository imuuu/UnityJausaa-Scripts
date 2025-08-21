using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.PoolSystem
{
    /// <summary>
    /// Main pooling manager that can create and manage multiple prefab pools.
    /// </summary>
    public class ManagerPrefabPooler : MonoBehaviour
    {
        public static ManagerPrefabPooler Instance { get; private set; }

        [Searchable]
        [TabGroup("Tab", "Skills", SdfIconType.Magic, TextColor = "green")]
        [SerializeField] private List<PrefabPoolDefinition> _prefabPoolSkills = new();
        [Searchable]
        [TabGroup("Tab", "Effects", SdfIconType.Lightning, TextColor = "blue")]
        [SerializeField] private List<PrefabPoolDefinition> _prefabEffectsSkills = new();
        [Searchable]
        [TabGroup("Tab", "Mobs", SdfIconType.Person, TextColor = "red")]
        [SerializeField] private List<PrefabPoolDefinition> _prefabMobsSkills = new();

        private Dictionary<int, Pool> _pools = new();
        private List<(GameObject, Pool)> _returningToHolder = new();

        private GameObject _poolGroupsHolder;

        #region Extra Classes
        [System.Serializable]
        public class PrefabPoolDefinition
        {
            [Tooltip("Prefab to pool. Must not be null.")]
            [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)]
            [BoxGroup("Prefab", ShowLabel = false)]
            [AssetsOnly]
            [Required]
            public GameObject Prefab;

            [BoxGroup("Options", ShowLabel = false)]
            public PoolOptions Options;
        }

        /// <summary>
        /// Internal structure to track each object in the pool
        /// </summary>
        public class PoolItem
        {
            public GameObject GameObject;
            public float ActivationTime;

            public PoolReturnOnDisabled PoolReturnOnDisabled;
            public Action<GameObject> OnReturnedCallback;
            public PoolHealthDetection PoolHealthDetection;
            public bool IsReturningFromDelayed = false;

        }

        /// <summary>
        /// A Pool holds a queue of inactive objects and a list of active objects.
        /// plus all associated settings.
        /// </summary>
        public class Pool
        {
            public Pool(GameObject prefab, PoolOptions options)
            {
                this.Prefab = prefab;
                this.Options = options;

                DeathController = prefab.GetComponent<IDeathController>();
                if (DeathController != null) DeathController.SetControlledByPool(true);
            }

            public GameObject Prefab;

            // Just for organization in the hierarchy
            public GameObject PoolHolder;

            public PoolOptions Options;

            public Queue<PoolItem> InactiveQueue = new();
            public List<PoolItem> ActiveList = new();  // track active items
            public Queue<PoolItem> ActiveSpawnOrder = new(); // used for RECYCLING
            public IDeathController DeathController;
        }

        #endregion Extra Classes
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Debug.LogWarning($"Multiple ManagerPrefabPooler instances in scene! Destroying {name}.");
                Destroy(this);
                return;
            }

            _poolGroupsHolder = new GameObject("PoolGroups(PrefabPooler)");
            InitPrefabLists();
        }

        //TODO remove this and use action scheduler!
        private void Update()
        {
            if (_returningToHolder.Count > 0)
            {
                for (int i = _returningToHolder.Count - 1; i >= 0; i--)
                {
                    (GameObject, Pool) item = _returningToHolder[i];
                    if (item.Item1 == null)
                    {
                        Debug.LogWarning("ManagerPrefabPooler: Returning null object to pool. Removing from returning list. PoolHolder: " + item.Item2.PoolHolder.name);
                        _returningToHolder.RemoveAt(i);
                        continue;
                    }
                    item.Item1.transform.SetParent(item.Item2.PoolHolder.transform, false);
                    _returningToHolder.RemoveAt(i);
                }
            }
            foreach (KeyValuePair<int, Pool> kvp in _pools)
            {
                Pool pool = kvp.Value;

                if (pool.Options.ReturnType == POOL_RETURN_TYPE.TIMED)
                {
                    float now = Time.time;
                    for (int i = 0; i < pool.ActiveList.Count; i++)
                    {
                        PoolItem item = pool.ActiveList[i];
                        float lifeTime = now - item.ActivationTime;
                        if (lifeTime >= pool.Options.ReturnDelay && !item.IsReturningFromDelayed)
                        {
                            //TODO
                            //ReturnObjectInternal(pool, item, forceImmediate:true);
                            ReturnToPool(pool, item, forceImmediate: true);
                            i--;
                        }
                    }
                }
            }
        }

        private void InitPrefabLists()
        {
            foreach (PrefabPoolDefinition def in _prefabPoolSkills)
            {
                if (def.Prefab)
                {
                    CreatePrefabPool(def.Prefab, def.Options);
                }
            }

            foreach (PrefabPoolDefinition def in _prefabEffectsSkills)
            {
                if (def.Prefab)
                {
                    CreatePrefabPool(def.Prefab, def.Options);
                }
            }

            foreach (PrefabPoolDefinition def in _prefabMobsSkills)
            {
                if (def.Prefab)
                {
                    CreatePrefabPool(def.Prefab, def.Options);
                }
            }
        }
        private void GeneratePoolHolder(Pool pool, GameObject CustomHolder = null)
        {
            GameObject poolHolder = null;
            if (CustomHolder != null)
            {
                poolHolder = CustomHolder;
                poolHolder.name = poolHolder.name + "(" + pool.Prefab.name + "_Pool)";
            }
            else
            {
                poolHolder = new GameObject(pool.Prefab.name + "_Pool");
                poolHolder.transform.SetParent(_poolGroupsHolder.transform);
            }

            pool.PoolHolder = poolHolder;
        }

        /// <summary>
        /// Call this to create a new pool for a given prefab with given options.
        /// If a pool already exists for this prefab, nothing is created.
        /// </summary>
        public void CreatePrefabPool(GameObject prefab, PoolOptions options, GameObject CustomHolder = null)
        {
            if (!prefab) return;

            int id = prefab.GetInstanceID();
            if (_pools.ContainsKey(id))
            {
                Debug.LogWarning($"ManagerPrefabPooler: Pool for '{prefab.name}' already exists.");
                return;
            }

            CheckCustomPoolOptions(prefab, options);

            Pool newPool = new Pool(prefab, options);

            GeneratePoolHolder(newPool, CustomHolder);

            for (int i = 0; i < options.Min; i++)
            {
                PoolItem poolItem = CreatePoolItem(newPool);
                newPool.InactiveQueue.Enqueue(poolItem);
            }

            _pools[id] = newPool;
        }

        public bool HasPoolCreated(GameObject prefab)
        {
            if (!prefab) return false;

            int id = prefab.GetInstanceID();
            if (_pools.ContainsKey(id))
            {
                return true;
            }

            return false;
        }

        private void CheckCustomPoolOptions(GameObject prefab, PoolOptions options)
        {
            PoolMonoOptions poolMonoOption = prefab.GetComponent<PoolMonoOptions>();
            if (poolMonoOption == null) return;

            List<OptionAddition> optionAdditions = poolMonoOption.GetOptionAdditions();
            foreach (OptionAddition optionAddition in optionAdditions)
            {
                optionAddition.LoadAddition(options);
            }
        }

        public PoolOptions GetPoolOptions(GameObject prefab)
        {
            if (!prefab) return null;

            int id = prefab.GetInstanceID();
            if (!_pools.ContainsKey(id))
            {
                Debug.LogWarning($"ManagerPrefabPooler: No pool found for '{prefab.name}'.");
                return null;
            }

            return _pools[id].Options;
        }

        /// <summary>
        /// Spawns an object from the specified prefab pool. If the pool doesn't exist yet,
        /// we create it with default settings (you can remove this behavior if you want).
        /// </summary>
        public GameObject GetFromPool(GameObject prefab, System.Action<GameObject> onReturnedCallback = null)
        {
            if (!prefab) return null;
            int id = prefab.GetInstanceID();

            if (!_pools.ContainsKey(id))
            {
                Debug.LogWarning($"ManagerPrefabPooler: No pool found for '{prefab.name}'. " +
                                 "Creating a default dynamic pool...");
                PoolOptions defaultOptions = new PoolOptions();
                CreatePrefabPool(prefab, defaultOptions);
            }

            Pool pool = _pools[id];
            GameObject gotObject = null;
            if (pool.InactiveQueue.Count > 0)
            {
                PoolItem item = pool.InactiveQueue.Dequeue();
                item.OnReturnedCallback = onReturnedCallback;
                ActivatePoolItem(pool, item);
                gotObject = item.GameObject;
            }
            else
            {
                // No inactive items available
                switch (pool.Options.PoolType)
                {
                    case POOL_TYPE.FIXED:
                        Debug.LogWarning($"ManagerPrefabPooler: Fixed pool for '{pool.Prefab.name}' is exhausted.");
                        return null;

                    case POOL_TYPE.DYNAMIC:
                        {
                            PoolItem newItem = CreatePoolItem(pool);
                            newItem.OnReturnedCallback = onReturnedCallback;
                            ActivatePoolItem(pool, newItem);
                            gotObject = newItem.GameObject;
                            break;
                            //only should be enabled if Dynamic and RECYCling same time!
                            //int totalInPool = pool.activeList.Count + pool.inactiveQueue.Count;
                            // if (totalInPool < pool.options.Max)
                            // {
                            //     PoolItem newItem = CreatePoolItem(pool);
                            //     ActivatePoolItem(pool, newItem);
                            //     return newItem.gameObject;
                            // }
                            // else
                            // {
                            //     Debug.LogWarning($"ManagerPrefabPooler: Dynamic pool for '{pool.prefab.name}' " +
                            //                      $"reached maxSize {pool.options.Max}.");
                            //     return null;
                            // }
                        }

                    case POOL_TYPE.RECYCLING:
                        {
                            int totalInPool = pool.ActiveList.Count + pool.InactiveQueue.Count;

                            if (totalInPool < pool.Options.Max)
                            {
                                PoolItem newItem = CreatePoolItem(pool);
                                newItem.OnReturnedCallback = onReturnedCallback;
                                ActivatePoolItem(pool, newItem);
                                gotObject = newItem.GameObject;
                                break;
                            }

                            // Recycle the oldest active one
                            if (pool.ActiveList.Count > 0)
                            {
                                // The oldest active item is the front of activeSpawnOrder
                                PoolItem oldestActive = pool.ActiveSpawnOrder.Dequeue();
                                // Force-return it (immediately):
                                ReturnObjectInternal(pool, oldestActive, /*forceImmediate=*/true);

                                Debug.Log("Recycling an item from pool: " + oldestActive.GameObject.name);

                                // Now something is in the inactiveQueue
                                if (pool.InactiveQueue.Count > 0)
                                {
                                    PoolItem item = pool.InactiveQueue.Dequeue();
                                    item.OnReturnedCallback = onReturnedCallback;
                                    if (item.PoolReturnOnDisabled)
                                    {
                                        item.PoolReturnOnDisabled.IsReturnEnabled = false;
                                    }
                                    ActivatePoolItem(pool, item);

                                    if (item.PoolReturnOnDisabled)
                                    {
                                        item.PoolReturnOnDisabled.IsReturnEnabled = false;
                                    }
                                    Debug.Log("Recycling an item from pool: " + item.GameObject.name);
                                    gotObject = item.GameObject;
                                    break;
                                }
                                else
                                {
                                    // If for some reason we can't get from inactive, create a new one
                                    PoolItem newItem = CreatePoolItem(pool);
                                    newItem.OnReturnedCallback = onReturnedCallback;
                                    ActivatePoolItem(pool, newItem);
                                    gotObject = newItem.GameObject;
                                    break;
                                }
                            }
                            else
                            {
                                // No active items to recycle => just create new
                                PoolItem newItem = CreatePoolItem(pool);
                                newItem.OnReturnedCallback = onReturnedCallback;
                                ActivatePoolItem(pool, newItem);
                                gotObject = newItem.GameObject;
                                break;
                            }
                        }
                }
            }

            if (gotObject == null) return null;

            if (pool.Options.LifeTimeDuration > 0f)
            {
                StartDelayedReturn(gotObject, pool.Options.LifeTimeDuration, POOL_ARRIVE_TYPE.LIFE_TIME);
            }

            return gotObject;
        }

        /// <summary>
        /// Manually return an object to its pool. 
        /// If the pool uses a returnDelay > 0, it will schedule a delayed return.
        /// </summary>
        /// <param name="obj">The object to return to its pool.</param>
        /// <param name="arriveType">The type of arrival of the object to the pool. Normally used inside</param>
        public bool ReturnToPool(GameObject obj, POOL_ARRIVE_TYPE arriveType = POOL_ARRIVE_TYPE.NONE)
        {
            if (!obj) return false;

            int poolId = FindPoolIdForObject(obj);
            if (poolId == 0 || !_pools.ContainsKey(poolId))
            {
                //Debug.LogWarning($"ManagerPrefabPooler: Object '{obj.name}' doesn't belong to any pool. Destroying.");
                //Destroy(obj);
                return false;
            }

            Pool pool = _pools[poolId];
            PoolItem item = FindPoolItemInActiveList(pool, obj);

            if (item == null)
            {
                Debug.LogWarning($"ManagerPrefabPooler: Object '{obj.name}' not found in active list? Destroying.");
                Destroy(obj);
            }
            // if (item == null)
            // {
            //     Debug.LogWarning($"ManagerPrefabPooler: Object '{obj.name}' not found in active list? Destroying.");
            //     Destroy(obj);
            //     return false;
            // }

            // if (pool.Options.DelayBeforeReturn.IsEnabled() && pool.Options.DelayBeforeReturn.Delay > 0f)
            // {
            //     StartDelayedReturn(pool, item, pool.Options.DelayBeforeReturn, POOL_ARRIVE_TYPE.DELAY_BEFORE_RETURN);
            //     return true;
            // }

            // ReturnObjectInternal(pool, item, forceImmediate: true, arriveType);

            // return true;

            return ReturnToPool(pool, item, forceImmediate: true, arriveType);
        }

        private bool ReturnToPool(Pool pool, PoolItem item, bool forceImmediate, POOL_ARRIVE_TYPE arriveType = POOL_ARRIVE_TYPE.NONE)
        {
            if (item == null)
            {
                //Debug.LogWarning($"ManagerPrefabPooler: Object '{obj.name}' not found in active list? Destroying.");
                //Destroy(obj);
                return false;
            }

            if (pool.Options.DelayBeforeReturn.IsEnabled() && pool.Options.DelayBeforeReturn.Delay > 0f)
            {
                item.IsReturningFromDelayed = true;
                StartDelayedReturn(pool, item, pool.Options.DelayBeforeReturn, POOL_ARRIVE_TYPE.DELAY_BEFORE_RETURN);
                return true;
            }

            ReturnObjectInternal(pool, item, forceImmediate, arriveType);

            return true;
        }

        public PoolOptions CreateDefaultPoolOption()
        {
            PoolOptions poolOptions = new PoolOptions()
            {
                Min = 10,
                Max = 0,
                PoolType = POOL_TYPE.DYNAMIC,
                ReturnType = POOL_RETURN_TYPE.MANUAL,
                EventTriggerType = POOL_EVENT_TRIGGER_TYPE.NONE,
                IsLifeTimeDeathEffect = false,
                LifeTimeDuration = 0f
            };

            return poolOptions;
        }

        #region Internal Helpers

        private PoolItem CreatePoolItem(Pool pool)
        {
            GameObject obj = Instantiate(pool.Prefab, pool.PoolHolder == null ? null : pool.PoolHolder.transform);
            obj.SetActive(false);

            // Optionally, store a small script that holds the pool ID for faster lookups
            // Or we do the O(N) search like we do below. 
            // In addition, if the pool’s returnBehavior is MANUAL
            // and the user wants auto-return on disable, we can add ReturnPoolOnDisabled:
            PoolReturnOnDisabled helper = null;
            if (pool.Options.ReturnType == POOL_RETURN_TYPE.DETECT_DISABLE)
            {
                helper = obj.GetComponent<PoolReturnOnDisabled>();
                if (!helper) helper = obj.AddComponent<PoolReturnOnDisabled>();

                helper.IsReturnEnabled = true;
            }

            PoolHealthDetection healthDetection = null;
            if (pool.Options.EnableHealthDetection)
            {
                healthDetection = obj.GetComponent<PoolHealthDetection>();

                if (!healthDetection)
                {
                    healthDetection = obj.AddComponent<PoolHealthDetection>();
                    healthDetection.Init(pool.Options.HealthThresholds);
                }

            }

            PoolItem poolItem = new PoolItem
            {
                GameObject = obj,
                PoolReturnOnDisabled = helper,
                PoolHealthDetection = healthDetection,
            };
            return poolItem;
        }

        private void ActivatePoolItem(Pool pool, PoolItem item)
        {
            item.ActivationTime = Time.time;
            pool.ActiveList.Add(item);
            pool.ActiveSpawnOrder.Enqueue(item);

            item.GameObject.SetActive(true);

            TriggerOnSpawnedEvent(item.GameObject, pool.Options.EventTriggerType);
        }

        private void ReturnObjectInternal(Pool pool, PoolItem item, bool forceImmediate, POOL_ARRIVE_TYPE arriveType = POOL_ARRIVE_TYPE.NONE)
        {
            // if there is a delayed return, cancel it
            ActionScheduler.CancelActions(item.GameObject.GetInstanceID());

            item.IsReturningFromDelayed = false;
            RemoveFromActiveList(pool, item);

            TriggerOnReturnedEvent(item.GameObject, pool.Options.EventTriggerType);

            if (pool.Options.DeathEffectOptions.IsEnabled())
            {
                if (arriveType == POOL_ARRIVE_TYPE.LIFE_TIME)
                {
                    if (pool.Options.IsLifeTimeDeathEffect)
                    {
                        SpawnDeathEffect(item.GameObject, pool.Options.DeathEffectOptions);
                    }
                }
                else
                {
                    SpawnDeathEffect(item.GameObject, pool.Options.DeathEffectOptions);
                }
            }

            item.GameObject.SetActive(false);

            //item.GameObject.transform.SetParent(pool.PoolHolder.transform, false);
            _returningToHolder.Add((item.GameObject, pool));

            // *** CALL THE CALLBACK ***
            item.OnReturnedCallback?.Invoke(item.GameObject);
            item.OnReturnedCallback = null;  // Clear it out, so next reuse doesn't call old callback

            pool.InactiveQueue.Enqueue(item);
        }


        private PoolItem FindPoolItemInActiveList(Pool pool, GameObject obj)
        {
            for (int i = 0; i < pool.ActiveList.Count; i++)
            {
                if (pool.ActiveList[i].GameObject == obj)
                {
                    return pool.ActiveList[i];
                }
            }
            return null;
        }

        private void RemoveFromActiveList(Pool pool, PoolItem item)
        {
            // remove from activeList
            pool.ActiveList.Remove(item);

            // Also remove from activeSpawnOrder if needed
            if (pool.Options.PoolType == POOL_TYPE.RECYCLING)
            {
                // We'll re-queue any that are not 'item'
                int count = pool.ActiveSpawnOrder.Count;
                Queue<PoolItem> tempQ = new Queue<PoolItem>();
                for (int i = 0; i < count; i++)
                {
                    PoolItem qItem = pool.ActiveSpawnOrder.Dequeue();
                    if (qItem != item)
                    {
                        tempQ.Enqueue(qItem);
                    }
                }
                pool.ActiveSpawnOrder = tempQ;
            }
        }

        private void StartDelayedReturn(Pool pool, PoolItem item, DelayBeforeReturn delayBeforeReturn, POOL_ARRIVE_TYPE arriveType)
        {
            // DelayedReturnHelper helper = item.GameObject.GetComponent<DelayedReturnHelper>();
            // if (!helper)
            // {
            //     helper = item.GameObject.AddComponent<DelayedReturnHelper>();
            // }
            // helper.Init(this, pool, item, returnAtTime);
            delayBeforeReturn.OnReturnStartInvoke(item.GameObject);
            ActionScheduler.CancelActions(item.GameObject.GetInstanceID());
            ActionScheduler.RunAfterDelay(delayBeforeReturn.Delay, () =>
            {
                delayBeforeReturn.OnReturnEndInvoke(item.GameObject);
                ReturnObjectInternal(pool, item, forceImmediate: true, arriveType);
            }, item.GameObject.GetInstanceID());
        }

        private void StartDelayedReturn(GameObject gameObject, float returnAtTime, POOL_ARRIVE_TYPE arriveType)
        {
            ActionScheduler.CancelActions(gameObject.GetInstanceID());

            ActionScheduler.RunAfterDelay(returnAtTime, () =>
            {
                ReturnToPool(gameObject, arriveType);
            }, gameObject.GetInstanceID());
        }

        private int FindPoolIdForObject(GameObject obj)
        {
            // Slow approach: look through each pool
            foreach (KeyValuePair<int, Pool> kvp in _pools)
            {
                Pool pool = kvp.Value;

                // activeList
                for (int i = 0; i < pool.ActiveList.Count; i++)
                {
                    if (pool.ActiveList[i].GameObject == obj)
                    {
                        return kvp.Key;
                    }
                }
                // inactiveQueue
                PoolItem[] inactiveArray = pool.InactiveQueue.ToArray();
                for (int i = 0; i < inactiveArray.Length; i++)
                {
                    if (inactiveArray[i].GameObject == obj)
                    {
                        return kvp.Key;
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// Spawns a death effect at the position of the current game object.
        /// If the death effect is pooled, it will be retrieved from the pool.
        /// If not, it will be instantiated as a new object.
        /// If the death effect has a particle system with a duration, it will be scheduled for destruction
        /// after that duration, or returned to the pool if it is pooled.
        /// </summary>
        /// <param name="currentGameobject"></param>
        /// <param name="deathEffectOptions"></param>
        public void SpawnDeathEffect(GameObject currentGameobject, DeathEffectOptions deathEffectOptions)
        {
            GameObject effectPrefab = deathEffectOptions.DeathEffectPrefab;
            GameObject newDeathPrefab = null;

            bool hasPool = false;
            if (_pools.ContainsKey(effectPrefab.GetInstanceID()))
            {
                newDeathPrefab = GetFromPool(effectPrefab);
                hasPool = true;
            }
            else
            {
                newDeathPrefab = Instantiate(effectPrefab);
                newDeathPrefab.SetActive(true);
            }

            if (newDeathPrefab == null) return;

            if (deathEffectOptions.IsDeathPosition)
                newDeathPrefab.transform.position = currentGameobject.transform.position;
            else
                newDeathPrefab.transform.position = Vector3.zero;

            if (deathEffectOptions.EnableParticleDuration && deathEffectOptions.GetDeathParticleDuration() > 0f)
            {
                if (hasPool) StartDelayedReturn(newDeathPrefab, deathEffectOptions.GetDeathParticleDuration(), POOL_ARRIVE_TYPE.NONE);
                else ActionScheduler.RunAfterDelay(deathEffectOptions.GetDeathParticleDuration(), () =>
                {
                    Destroy(newDeathPrefab);
                });
            }

        }
        private void TriggerOnSpawnedEvent(GameObject obj, POOL_EVENT_TRIGGER_TYPE triggerType)
        {
            if (triggerType == POOL_EVENT_TRIGGER_TYPE.NONE) return;

            if (triggerType == POOL_EVENT_TRIGGER_TYPE.ALL_CHILDREN)
            {
                Component[] comps = obj.GetComponentsInChildren<Component>(true);
                for (int i = 0; i < comps.Length; i++)
                {
                    IPoolEvents e = comps[i] as IPoolEvents;
                    if (e != null)
                    {
                        e.OnSpawnedFromPool();
                    }
                }
                return;
            }
            if (triggerType == POOL_EVENT_TRIGGER_TYPE.ROOT_ONLY)
            {
                Component[] comps = obj.GetComponents<Component>();
                for (int i = 0; i < comps.Length; i++)
                {
                    IPoolEvents e = comps[i] as IPoolEvents;
                    if (e != null)
                    {
                        e.OnSpawnedFromPool();
                    }
                }
                return;
            }
        }

        private void TriggerOnReturnedEvent(GameObject obj, POOL_EVENT_TRIGGER_TYPE triggerType)
        {
            if (triggerType == POOL_EVENT_TRIGGER_TYPE.NONE) return;

            if (triggerType == POOL_EVENT_TRIGGER_TYPE.ALL_CHILDREN)
            {
                Component[] comps = obj.GetComponentsInChildren<Component>(true);
                for (int i = 0; i < comps.Length; i++)
                {
                    IPoolEvents e = comps[i] as IPoolEvents;
                    if (e != null)
                    {
                        e.OnReturnedToPool();
                    }
                }
                return;
            }

            if (triggerType == POOL_EVENT_TRIGGER_TYPE.ROOT_ONLY)
            {
                Component[] comps = obj.GetComponents<Component>();
                for (int i = 0; i < comps.Length; i++)
                {
                    IPoolEvents e = comps[i] as IPoolEvents;
                    if (e != null)
                    {
                        e.OnReturnedToPool();
                    }
                }
                return;
            }
        }

        #endregion
    }
}
