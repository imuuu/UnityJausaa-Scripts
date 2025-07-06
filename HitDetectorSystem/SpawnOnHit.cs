using Game.PoolSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.HitDetectorSystem
{
    public class SpawnOnHit : MonoBehaviour, IOnHit
    {
        [Title("Target Hit Detector")]
        [SerializeField] private HitDetector _hitDetector;
        [Title("Layer")]
        [SerializeField] private LayerMask _layerMask;
        [Title("Prefab")]
        [SerializeField] private GameObject _hitEffectPrefab;
        [Title("Options")]
        [Tooltip("If true, the hit effect will be rotated to the hit direction.")]
        [SerializeField] private bool _useDirection = false;
        [SerializeField,ShowIf("_useDirection")] private bool _invertDirection = false;
        [SerializeField, ToggleLeft] private bool _usePooling = true;
        [SerializeField, ShowIf("_usePooling")] private bool _usePoolAutoReturn = false;
        [SerializeField, ShowIf("_usePoolAutoReturn")] private float _poolAutoReturnDelay = 0f;

        //private static bool _poolingInitialized = false;

        private void Start()
        {
            if (_hitEffectPrefab == null) return;

            _hitDetector.RegisterOnHit(this);

            // TODO probably shouldn't be like this, but for now it is fine
            // if (_poolingInitialized) return;
            // _poolingInitialized = true;

            // if (_usePooling && _usePoolAutoReturn && _poolAutoReturnDelay > 0)
            // {
            //     PoolOptions options = ManagerPrefabPooler.Instance.GetPoolOptions(_hitEffectPrefab);
            //     options.ReturnType = POOL_RETURN_TYPE.TIMED;
            //     options.ReturnDelay = _poolAutoReturnDelay;
            // }
        }

        public void OnHit(HitCollisionInfo hitInfo)
        {
            if (!hitInfo.HasCollisionPoint) return;

            if (_hitEffectPrefab == null) return;

            if (!hitInfo.HitLayer.ContainsAny(_layerMask))
            {
                return;
            }

            GameObject hitEffect;

            if (_usePooling)
            {
                hitEffect = ManagerPrefabPooler.Instance.GetFromPool(_hitEffectPrefab);

                if (_usePoolAutoReturn && _poolAutoReturnDelay > 0)
                {
                    ActionScheduler.RunAfterDelay(_poolAutoReturnDelay, () =>
                    {
                        ManagerPrefabPooler.Instance.ReturnToPool(hitEffect);
                    });
                }

            }
            else hitEffect = Instantiate(_hitEffectPrefab);

            hitEffect.transform.position = hitInfo.CollisionPoint;

            if (_useDirection && hitInfo.HasDirection)
            {
                Vector3 dir = _invertDirection ? -hitInfo.Direction : hitInfo.Direction;
                if (dir != Vector3.zero)
                    hitEffect.transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }
}