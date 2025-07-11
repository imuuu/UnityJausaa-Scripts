using System.Collections.Generic;
using Game.PoolSystem;
using Game.StatSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.HitDetectorSystem
{
    [DefaultExecutionOrder(-100)]
    public partial class ManagerHitDectors : MonoBehaviour
    {
        public static ManagerHitDectors Instance { get; private set; }
        private Dictionary<int, IHitDetector> _detectors = new();
        private HashSet<int> _initialized = new();
        private List<IHitDetector> _pendingAdds = new();
        private List<IHitDetector> _pendingRemoves = new();
        private List<IHitDetector> _pendingToDestroy = new();
        private List<int> _idsToRemove = new();

        private Dictionary<int, HashSet<int>> _hitHistory = new();
        private Dictionary<IHitDetector, float> _hitHistoryTimers = new();

        [SerializeField] private bool _debug = false;
        [SerializeField] private bool _debugHitRays = false;

        private static LayerMask HitLayerMask;
        public static int ENEMY_LAYER;
        public static int WALL_LAYER;
        public static int OBSTACLE_LAYER;
        public static int PLAYER_LAYER;
        private static bool _layersInitialized = false;

        [SerializeField][ShowIf("_debug")] private List<GameObject> _debugHitDetectors = new();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            InitializeLayers();
        }

        private static void InitializeLayers()
        {
            if (_layersInitialized) return;
            _layersInitialized = true;

            HitLayerMask = LayerMask.GetMask("Enemy", "Wall", "Obstacle", "Player");
            ENEMY_LAYER = LayerMask.NameToLayer("Enemy");
            WALL_LAYER = LayerMask.NameToLayer("Wall");
            OBSTACLE_LAYER = LayerMask.NameToLayer("Obstacle");
            PLAYER_LAYER = LayerMask.NameToLayer("Player");

            Debug.Log($"HitLayerMask: {HitLayerMask}");
            Debug.Log($"Hitlayermask names: {HitLayerMask.PrintNames()}");
        }

        private void Update()
        {
            if (ManagerPause.IsPaused())
            {
                return;
            }

            for (int i = 0; i < _pendingAdds.Count; i++)
            {
                IHitDetector detector = _pendingAdds[i];
                //int id = detector.GetGameObject().GetInstanceID();
                int id = detector.GetID();
                if (!_detectors.ContainsKey(id))
                {
                    _detectors.Add(id, detector);
                    _initialized.Add(id);
                }

                if (detector.GetHitHistoryTimer() > 0f && !_hitHistoryTimers.ContainsKey(detector))
                {
                    _hitHistoryTimers.Add(detector, detector.GetHitHistoryTimer());
                }
            }
            _pendingAdds.Clear();

            for (int i = 0; i < _pendingRemoves.Count; i++)
            {
                IHitDetector detector = _pendingRemoves[i];
                //int id = detector.GetGameObject().GetInstanceID();
                int id = detector.GetID();
                _detectors.Remove(id);

                if (_hitHistoryTimers.ContainsKey(detector))
                {
                    _hitHistoryTimers.Remove(detector);
                }

                if (_pendingToDestroy.Contains(detector))
                {
                    _pendingToDestroy.Remove(detector);
                    Destroy(detector as Component);
                }
            }
            _pendingRemoves.Clear();

            if (_idsToRemove.Count > 0)
            {
                foreach (int id in _idsToRemove)
                {
                    _detectors.Remove(id);
                }
                _idsToRemove.Clear();
            }

            //float deltaTime = Time.deltaTime;
            foreach (KeyValuePair<int, IHitDetector> kvp in _detectors)
            {
                IHitDetector detector = kvp.Value;

                if (!detector.IsEnabled())
                    continue;

                if (detector.IsManual)
                    continue;

                if (_initialized.Contains(kvp.Key))
                {
                    _initialized.Remove(kvp.Key);
                    continue;
                }


                if (_hitHistoryTimers.ContainsKey(detector))
                {
                    _hitHistoryTimers[detector] -= Time.deltaTime;
                    if (_hitHistoryTimers[detector] <= 0f)
                    {
                        ClearHitHistory(kvp.Value);
                        _hitHistoryTimers[detector] = kvp.Value.GetHitHistoryTimer();
                    }
                }

                if (!detector.ShouldPerformHitCheck(Time.deltaTime))
                {
                    continue;
                }

                CallPerformHitCheck(detector);

                // if (detector.PerformHitCheck(out HitCollisionInfo hitInfo))
                // {
                //     bool hitHappened = HandleHit(kvp.Value, hitInfo);
                // }
            }

#if UNITY_EDITOR
            if (!_debug) return;

            _debugHitDetectors.Clear();
            foreach (var detector in _detectors.Values)
            {
                if (detector == null || detector.GetGameObject() == null) continue;

                _debugHitDetectors.Add(detector.GetGameObject());
            }
#endif
        }

        public static LayerMask GetHitLayerMask()
        {
            return HitLayerMask;
        }

        public void RegisterDetector(IHitDetector detector)
        {
            //Debug.Log($"Registering detector: {(detector as Component)?.name}");
            if (detector == null) return;

            _pendingRemoves.Remove(detector);

            _pendingAdds.Add(detector);
        }

        public void UnregisterDetector(IHitDetector detector)
        {
            if (detector == null) return;

            if (detector.IsBeginDestroyed()) return;

            _pendingAdds.Remove(detector);

            if (_idsToRemove.Contains(detector.GetID()))
            {
                return;
            }

            _pendingRemoves.Add(detector);
        }

        public void DestroyDetectorScript(IHitDetector detector)
        {
            if (detector == null) return;

            if (!_pendingRemoves.Contains(detector))
            {
                _pendingRemoves.Add(detector);
            }

            _pendingToDestroy.Add(detector);

            if (_idsToRemove.Contains(detector.GetID()))
            {
                return;
            }

            _idsToRemove.Add(detector.GetID());

            detector.SetBeginDestroyed(true);
        }

        public void CallPerformHitCheck(IHitDetector detector)
        {
            // if (detector.PerformHitCheck(out HitCollisionInfo hitInfo))
            // {
            //     HandleHit(detector, hitInfo);
            // }
            if (detector is IMultiHitDetector multi)
            {
                List<HitCollisionInfo> hits = multi.PerformHitChecks();
                detector.SetFinalHit(false);
                for (int i = 0; i < hits.Count; i++)
                {
                    if (i == hits.Count - 1)
                    {
                        detector.SetFinalHit(true);
                    }

                    HandleHit(detector, hits[i]);
                }
                
            }
            else if (detector.PerformHitCheck(out HitCollisionInfo hitInfo))
            {
                HandleHit(detector, hitInfo);
            }
        }

        /// <summary>
        /// Handles the hit detection logic for a given detector and hit information.
        /// It checks if the hit object has an owner and whether the hit should be processed based on piercing logic.
        /// </summary>
        /// <param name="detector"></param>
        /// <param name="hitInfo"></param>
        /// <returns> returns false if hit didnt happen. Returns true if hit happened</returns>
        public bool HandleHit(IHitDetector detector, HitCollisionInfo hitInfo)
        {
            if (detector == null || hitInfo == null)
            {
                Debug.LogWarning("ManagerHitDectors: HandleHit called with null detector or hitInfo");
                return false;
            }

            if (_debugHitRays)
            {
                Debug.DrawLine(detector.GetGameObject().transform.position, hitInfo.CollisionPoint, Color.red, 1f);
            }

            if (_debug)
            {
                Debug.Log($"ManagerHitDectors: {detector.GetGameObject().name} - Hit Info: {hitInfo}");
            }

            GameObject hitObject = hitInfo.HitObject;

            // hit ground or target got destroyed
            if (hitObject == null)
            {
                if (_debug)
                    Debug.Log($"OOOOOOOOOOO ManagerHitDectors: {detector.GetGameObject().name} hit null object");
                return true;
            }

            bool hitHasOwner = hitObject.TryGetComponent(out IOwner hitObjectOwner);

            if (_debug)
            {
                Debug.Log($"ManagerHitDectors: {detector.GetGameObject().name} hit {hitObject.name} - Has Owner: {hitHasOwner}");
            }

            // if (IsHitHistory(detector, hitObject))
            // {
            //     return false;
            // }

            if (detector.MaxPiercing > -1 && IsHitHistory(detector, hitObject))
            {
                //Debug.Log($"ManagerHitDectors: {detector.GetGameObject().name} hit {hitObject.name} - Already hit this object");
                return false;
            }

            GameObject detectorObject = detector.GetOwner().GetRootOwner().GetGameObject();
            IDamageDealer damageDealer = hitInfo.CustomDamageDealer ?? detectorObject.GetComponent<IDamageDealer>();

            if (hitHasOwner)
            {
                OWNER_TYPE dealerOwnerType = detector.GetOwner().GetRootOwner().GetOwnerType();
                OWNER_TYPE receiverOwnerType = hitObjectOwner.GetRootOwner().GetOwnerType();
                if (dealerOwnerType == receiverOwnerType)
                    return false;

                if (_debug)
                {
                    Debug.Log($"ManagerHitDectors: {hitObjectOwner.GetRootOwner().GetGameObject().name} hit {hitObject.name} - OwnerType: {hitObjectOwner.GetRootOwner().GetGameObject().name}");
                }

                IDamageReceiver receiver = hitObjectOwner.GetRootOwner().GetGameObject().GetComponent<IDamageReceiver>();

                if (receiverOwnerType == OWNER_TYPE.PLAYER)
                {
                    float playerBlockChance = Player.Instance.GetStatList().GetValueOfStat(StatSystem.STAT_TYPE.BLOCK_CHANCE);

                    if (playerBlockChance > 0f && Random.value * 100f < playerBlockChance)
                    {
                        if (_debug)
                        {
                            Debug.Log($"ManagerHitDectors: {detector.GetGameObject().name} - Player blocked the hit from {detectorObject.name}");
                        }

                        Events.OnBlockHappened.Invoke( damageDealer, receiver);

                        return true; // hit happened but was blocked
                    }
                }
                
                // todo if mobs have block

                if (receiver != null && damageDealer != null)
                {
                    // if (_debug)
                    // {
                    //     Debug.Log("==> DamageDealer: " + detector.GetOwner().GetRootOwner().GetGameObject().name
                    //         + " - DamageReceiver: " + hitObjectOwner.GetRootOwner().GetGameObject().name);
                    // }

                    StatList dealerStats = null;
                    StatList receiverStats = null;

                    if (detector is IStatReceiver dealerStatReceiver) dealerStats = dealerStatReceiver.GetStats();

                    if (receiver is IStatReceiver receiverStatReceiver) receiverStats = receiverStatReceiver.GetStats();

                    if (dealerStats != null)
                    {
                        if (dealerStats != null && detector.TotalPierces > 0 && dealerStats.TryGetStat(StatSystem.STAT_TYPE.DAMAGE_PERCENT_EACH_PIERCE, out Stat stat))
                        {
                            float damage = damageDealer.GetDamage();
                            damage = damage * (1f + stat.GetValue() * 0.01f * detector.TotalPierces);

                            if (damage < 1f) damage = 1f;

                            damageDealer.SetTemporaryDamage(damage);
                            //Debug.Log($"ManagerHitDectors: {detector.GetGameObject().name} - Total Pierces: {detector.TotalPierces} - New Damage: {damage}");
                        }
                    }

                    if (_debug)
                    {
                        if (detector is HitDetector_AreaDamage)
                            Debug.Log($"ManagerHitDectors: AreaDamage detected for {detector.GetGameObject().name} - DamageDealer: {detector.GetOwner().GetRootOwner().GetGameObject().name} - DamageReceiver: {hitObjectOwner.GetRootOwner().GetGameObject().name} - Damage: {damageDealer.GetDamage()}");
                        else
                            Debug.Log($"====> DamageDealer: {detector.GetOwner().GetRootOwner().GetGameObject().name} - DamageReceiver: {hitObjectOwner.GetRootOwner().GetGameObject().name} - Damage: {damageDealer.GetDamage()}");
                    }
                    bool damageHappened = DamageCalculator.CalculateDamage(damageDealer, receiver, dealerStats, receiverStats);

                    //Debug.Log($"ManagerHitDectors: {detector.GetGameObject().name} hit {hitObject.name} - DamageHappened: {damageHappened} - ownerType: {hitObjectOwner2.GetRootOwner().GetOwnerType()} - orginal owner {hitObjectOwner.GetRootOwner().GetOwnerType()}");
                    if (damageHappened && hitObjectOwner.GetRootOwner().GetOwnerType() == OWNER_TYPE.PLAYER)
                    {
                        Events.OnPlayerDamageTaken.Invoke(damageDealer.GetDamage());
                    }

                    damageDealer.RemoveTemporaryDamage();
                }
            }
            else
            {
                if (_debug)
                    Debug.Log($"ManagerHitDectors: {detector.GetGameObject().name} hit {hitObject.name} - No Owner");
            }

           

            

            detector.OnHit(hitInfo);

            if (!detector.IsFinalHit()) return true;

            if (!hitHasOwner)
            {
                detector.OnFinalHit(hitInfo);
                EndHitDetector(detector);
                return true;
            }

            float piercingChance = detector.PiercingChance;

            if (piercingChance < 100)
            {
                if (piercingChance > -1f)
                {
                    float randomValue = Random.value * 100f;
                    if (randomValue > piercingChance)
                    {
                        detector.DecrementPiercing();
                    }
                }
                else
                {
                    detector.DecrementPiercing();
                }
            }

            if (detector.RemainingPiercing >= 1)
            {
                detector.TotalPierces++;
                detector.OnPierceHit(hitInfo);
            }

            if (_debug)
            {
                float distance = Vector3.Distance(detector.GetGameObject().transform.position, hitObject.transform.position);
                Debug.Log($"ManagerHitDectors: Distance {distance} Remaining piercing for {detector.GetGameObject().name}: {detector.RemainingPiercing}");
            }

            if (detector.RemainingPiercing <= 0)
            {
                //Debug.Log($"ManagerHitDectors: {detector.GetGameObject().name} - No remaining piercing, ending hit detector for {hitObject.name}");
                detector.OnFinalHit(hitInfo);
                EndHitDetector(detector);
                return true;
            }

            AddHitHistory(detector, hitObject);
            return true;
        }

        // TODO: there might have problems if there is multiple detectors with the same GameObject,
        private void EndHitDetector(IHitDetector detector)
        {
            
            ClearHitHistory(detector);

            // TODO might be wrong place??
            _idsToRemove.Add(detector.GetID());

            if (detector.IsManualDestroy()) return;

            //Debug.Log($"ManagerHitDectors: EndHitDetector for {detector.GetGameObject().name}");

            if (!detector.GetGameObject().activeInHierarchy)
            {
                Debug.LogWarning($"ManagerHitDectors: EndHitDetector for {detector.GetGameObject().name} - GameObject is not active in hierarchy!");
                return;
            }

            if (ManagerPrefabPooler.Instance.ReturnToPool(detector.GetGameObject()))
            {
                //Debug.Log($"ManagerHitDectors: Returned {detector.GetGameObject().name} to pool");
                return;
            }

            //_idsToRemove.Add(detector.GetGameObject().GetInstanceID());

            // handles destroy etc
            if (detector.GetGameObject().GetComponent<DeathController>() != null) return;


            Destroy(detector.GetGameObject());
        }

        public bool IsHitHistory(IHitDetector hitDetector, GameObject hitObject)
        {
            int detectorId = hitDetector.GetID();
            int hitObjectId = hitObject.GetInstanceID();
            if (_hitHistory.ContainsKey(detectorId))
            {
                return _hitHistory[detectorId].Contains(hitObjectId);
            }
            return false;
        }

        public void ClearHitHistory(IHitDetector hitDetector)
        {
            int detectorId = hitDetector.GetID();
            if (_hitHistory.ContainsKey(detectorId))
            {
                _hitHistory[detectorId].Clear();
            }
        }

        private void AddHitHistory(IHitDetector hitDetector, GameObject hitObject)
        {
            int detectorId = hitDetector.GetID();
            int hitObjectId = hitObject.GetInstanceID();
            if (!_hitHistory.ContainsKey(detectorId))
            {
                _hitHistory.Add(detectorId, new HashSet<int>());
            }

            //Debug.Log($"ManagerHitDectors: Adding hit history for {hitDetector.GetGameObject().name} - Hit Object: {hitObject.name}");
            _hitHistory[detectorId].Add(hitObjectId);
        }

        /// <summary>
        /// Checks a series of points (interpreted as connected line segments) for collisions.
        /// It iterates over each segment (from points[i] to points[i+1]) and uses Physics.Linecast.
        /// 
        /// If stopAtNoOwner is true, the method will ignore collisions where the hit object has an owner (IOwner)
        /// and will only return when a collision is detected with an object that does not have an owner.
        /// Otherwise, it returns on the first collision regardless of ownership.
        /// 
        /// Returns true if a collision is found (based on the criteria); otherwise false.
        /// The collision point and hit GameObject are output.
        /// </summary>
        /// <param name="points">Array of points defining the line segments.</param>
        /// <param name="stopAtNoOwner">If true, only collisions with objects that do not have an owner will cause an immediate stop.</param>
        /// <param name="collisionPoint">Output collision point where the hit occurred.</param>
        /// <param name="hitObject">Output GameObject that was hit.</param>
        /// <returns>True if a collision is found (per criteria), false otherwise.</returns>
        public bool CheckPointsCollision(Vector3[] points, bool stopAtNoOwner, out Vector3 collisionPoint, out GameObject hitObject)
        {
            collisionPoint = Vector3.zero;
            hitObject = null;

            // Check each line segment defined by consecutive points.
            for (int i = 0; i < points.Length - 1; i++)
            {
                // Perform a linecast from points[i] to points[i+1].
                if (Physics.Linecast(points[i], points[i + 1], out RaycastHit hit))
                {
                    // A collision was detected.
                    // If stopAtNoOwner is true, then only consider collisions where the hit object does not have an owner.
                    if (stopAtNoOwner)
                    {
                        if (!hit.collider.gameObject.TryGetComponent<IOwner>(out IOwner owner))
                        {
                            // No owner found—stop immediately.
                            collisionPoint = hit.point;
                            hitObject = hit.collider.gameObject;
                            if (_debug)
                            {
                                Debug.Log($"CheckPointsCollision: Hit object without owner: {hitObject.name} at {collisionPoint}");
                            }
                            return true;
                        }
                        else
                        {
                            // The hit object has an owner; ignore this collision and continue checking subsequent segments.
                            continue;
                        }
                    }
                    else
                    {
                        // Without the stopAtNoOwner requirement, return on any collision.
                        collisionPoint = hit.point;
                        hitObject = hit.collider.gameObject;
                        if (_debug)
                        {
                            Debug.Log($"CheckPointsCollision: Collision detected with {hitObject.name} at {collisionPoint}");
                        }
                        return true;
                    }
                }
            }
            // No collision was detected along any segment.
            return false;
        }

        /// <summary>
        /// Checks a series of points (defining connected line segments) for collisions and “clips” the points if needed.
        /// It iterates over each segment (from points[i] to points[i+1]) using Physics.Linecast.
        /// When a collision is found, it returns a new array containing the points from the beginning up to that segment,
        /// with the final point replaced by the collision point.
        /// 
        /// Additionally, any collisions on objects that have an IOwner component are recorded (in the output list).
        /// If stopAtNoOwner is true, collisions on objects with an owner are ignored for clipping purposes (but still recorded).
        /// If no valid collision is found, the original points array is returned.
        /// </summary>
        /// <param name="points">Array of points defining the line segments.</param>
        /// <param name="stopAtNoOwner">
        /// If true, collisions with objects that have an IOwner are ignored for clipping purposes;
        /// only a collision with an object without an owner will stop the line.
        /// </param>
        /// <param name="ownerHitObjects">
        /// Output list containing all GameObjects that were hit along the line and had an IOwner component.
        /// </param>
        /// <returns>
        /// A new Vector3[] with the segments clipped at the collision point (if one is found),
        /// or the original points array if no clipping collision is detected.
        /// </returns>
        public Vector3[] ClipPointsToCollision(Vector3[] points, out List<GameObject> ownerHitObjects)
        {
            ownerHitObjects = new List<GameObject>();
            if (points == null || points.Length < 2)
                return points;

            // Iterate over each segment.
            for (int i = 0; i < points.Length - 1; i++)
            {
                if (Physics.Linecast(points[i], points[i + 1], out RaycastHit hit))
                {
                    // if (hit.collider.gameObject.TryGetComponent<IOwner>(out IOwner owner)) // && stopAtNoOwner
                    // {
                    //     ownerHitObjects.Add(hit.collider.gameObject);
                    //     continue;
                    // }
                    // else
                    // {
                    if (hit.collider.gameObject.TryGetComponent<IOwner>(out IOwner temp))
                    {
                        ownerHitObjects.Add(hit.collider.gameObject);
                        continue;
                    }
                    List<Vector3> clippedPoints = new List<Vector3>();
                    for (int j = 0; j <= i; j++)
                    {
                        clippedPoints.Add(points[j]);
                    }
                    clippedPoints.Add(hit.point);

                    if (_debug)
                    {
                        Debug.Log($"ClipPointsToCollision: Collision detected on segment {i}-{i + 1}. Clipped at {hit.point}.");
                        MarkHelper.DrawSphereTimed(hit.point, 0.2f, 10f, Color.green);
                    }
                    return clippedPoints.ToArray();
                    //}
                }
            }
            // No valid collision was found; return the original points.
            return points;
        }

        /// <summary>
        /// Iterates over each segment defined by <paramref name="points"/> and gathers all collisions using Physics.RaycastAll.
        /// If stopAtNoOwner is true, collisions on objects with an IOwner are skipped.
        /// Returns a list of CollisionInfo for each valid collision.
        /// </summary>
        public List<HitCollisionInfo> GetAllCollisionsOnLine(Vector3[] points)
        {
            List<HitCollisionInfo> collisions = new();
            if (points == null || points.Length < 2)
                return collisions;

            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 segmentStart = points[i];
                Vector3 segmentEnd = points[i + 1];
                Vector3 direction = (segmentEnd - segmentStart).normalized;
                float distance = Vector3.Distance(segmentStart, segmentEnd);

                RaycastHit[] hits = Physics.RaycastAll(segmentStart, direction, distance);

                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (RaycastHit hit in hits)
                {
                    //if pooling this need to be checked when is correct time to return it!
                    HitCollisionInfo info = GetNewHitCollisionInfo(hit.collider.gameObject);

                    info.SetCollisionPoint(hit.point);
                    info.SetDirection(direction);

                    collisions.Add(info);
                }
            }
            return collisions;
        }

        public HitCollisionInfo GetNewHitCollisionInfo(GameObject hitObject)
        {
            //TODO Pooling
            return new HitCollisionInfo
            {
                HitObject = hitObject,
                HasCollisionPoint = false,
                HasDirection = false,
                HitLayer = 1 << hitObject.layer,
            };
        }
        public bool IsDebug()
        {
            return _debug;
        }

        public bool IsDebugHitRays()
        {
            return _debugHitRays;
        }
    }
}
