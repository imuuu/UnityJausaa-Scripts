using UnityEngine;
namespace Game.PoolSystem
{
    public class DelayedReturnHelper : MonoBehaviour
    {
        private ManagerPrefabPooler.Pool _pool;
        private ManagerPrefabPooler.PoolItem _item;
        private ManagerPrefabPooler _manager;
        private float _returnAtTime;
        private bool _initialized = false;

        public void Init(ManagerPrefabPooler manager, ManagerPrefabPooler.Pool pool, ManagerPrefabPooler.PoolItem item, float returnAtTime)
        {
            _manager = manager;
            _pool = pool;
            _item = item;
            _returnAtTime = returnAtTime;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;

            if(ManagerPause.IsPaused()) return;

            if (Time.time >= _returnAtTime)
            {
                //TODO is this best practice? Probably ActionScheduler is better,
                // Do the immediate return
                _manager.SendMessage("ReturnObjectInternal",
                    new object[] { _pool, _item, true },
                    SendMessageOptions.DontRequireReceiver);

                Destroy(this); // done
            }
        }
    }
}
