using UnityEngine;
namespace Game.ChunkSystem
{
    public class ChunkItemMono : MonoBehaviour
    {
        [SerializeField] private ChunkItem _chunkItem;

        private void Awake()
        {
            if(_chunkItem == null)
                _chunkItem = new ();
            
            _chunkItem.GameObject = this.gameObject;
        }

        public void Start()
        {
            if(ManagerChunks.Instance == null) return;

            Chunk chunk = ManagerChunks.Instance.RegisterObject(_chunkItem);

            if(!chunk.IsActive)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if(ManagerChunks.Instance == null) return;

            ManagerChunks.Instance.UnregisterObject(_chunkItem);
        }

        public ChunkItem GetChunkItem()
        {
            return _chunkItem;
        }
    }
}