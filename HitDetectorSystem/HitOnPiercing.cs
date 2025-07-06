using Game.PoolSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.HitDetectorSystem
{
    [RequireComponent(typeof(IHitDetector))]
    public class HitOnPiercing : MonoBehaviour, IOnPiercing
    {
        [Title("Target Hit Detector")]
        [SerializeField] private HitDetector _hitDetector;
        [Title("Prefab")]
        [SerializeField] private GameObject _hitPierceEffectPrefab;
       
        [Tooltip("If true, the hit effect will be rotated to the hit direction.")]
        [SerializeField] private bool _useDirection = false;

        [Title("Pooling")]
        [SerializeField, ToggleLeft] private bool _usePooling = true;
        [SerializeField, ShowIf("_usePooling")] private bool _usePoolAutoReturn = false;
        [SerializeField, ShowIf("_usePooling"), ShowIf("_usePoolAutoReturn")] private float _poolAutoReturnDelay = 0f;

        private static bool _poolingInitialized = false;
        private void Start()
        {
            if (_hitPierceEffectPrefab == null) return;

            if (_poolingInitialized) return;
            _poolingInitialized = true;

            if (_usePooling && _usePoolAutoReturn && _poolAutoReturnDelay > 0)
            {
                PoolOptions options = ManagerPrefabPooler.Instance.GetPoolOptions(_hitPierceEffectPrefab);
                options.ReturnType = POOL_RETURN_TYPE.TIMED;
                options.ReturnDelay = _poolAutoReturnDelay;
            }
        }
        
        public void OnPiercing(HitCollisionInfo hitInfo)
        {
            if (!hitInfo.HasCollisionPoint) return;

            if(_hitPierceEffectPrefab == null) return;

            GameObject hitEffect;

            if (_usePooling) hitEffect = ManagerPrefabPooler.Instance.GetFromPool(_hitPierceEffectPrefab);
            else hitEffect = Instantiate(_hitPierceEffectPrefab);

            hitEffect.transform.position = hitInfo.CollisionPoint;

            if (_useDirection && hitInfo.HasDirection)
            {
                hitEffect.transform.rotation = Quaternion.LookRotation(hitInfo.Direction);
            }
        }

    }
}