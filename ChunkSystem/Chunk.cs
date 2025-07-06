using System.Collections.Generic;
using UnityEngine;

namespace Game.ChunkSystem
{
    /// <summary>
    /// Represents a single chunk in the game world as a 2D region (XZ plane).
    /// </summary>
    public class Chunk : ISpawnArea
    {
        public Vector2Int ChunkIndex;
        public Rect Area; // 2D area defined in XZ (Y from Rect corresponds to Z in 3D)
        public bool IsActive;
        public List<ChunkItem> Objects;
        public List<MovingGhostObject> MovingGhostObjects;

        private Vector2[] _vertices;
        public Vector2[] Vertices => _vertices ??= new Vector2[]
        {
            new Vector2(Area.xMin, Area.yMin),
            new Vector2(Area.xMax, Area.yMin),
            new Vector2(Area.xMax, Area.yMax),
            new Vector2(Area.xMin, Area.yMax)
        };

        public float Elevation => 0f;

        public Chunk(Vector2Int index, Rect area)
        {
            ChunkIndex = index;
            Area = area;
            IsActive = true;
            Objects = new();
            MovingGhostObjects = new List<MovingGhostObject>();
        }

        /// <summary>
        /// Checks if the given world position (using X and Z) is within the chunk area.
        /// </summary>
        public bool IsInChunk(Vector3 position)
        {
            return Area.Contains(new Vector2(position.x, position.z));
        }

        public void RegisterObject(ChunkItem item)
        {
            if (!Objects.Contains(item))
                Objects.Add(item);
        }

        public void RegisterObject(GameObject obj)
        {
            ChunkItem item = new ChunkItem { GameObject = obj };
            if (!Objects.Contains(item))
                Objects.Add(item);

        }

        public void UnregisterObject(GameObject obj)
        {
            ChunkItem item = new ChunkItem { GameObject = obj };
            if (Objects.Contains(item))
                Objects.Remove(item);
        }

        public void UnregisterObject(ChunkItem item)
        {
            if (Objects.Contains(item))
                Objects.Remove(item);
        }
        
        public Vector3 GetCenter()
        {
            return new Vector3(Area.center.x, 0, Area.center.y);
        }
    }
}
