using Unity.Mathematics;

namespace Game.ChunkSystem
{
    /// <summary>
    /// DOTS-friendly data for a moving ghost object.
    /// </summary>
    public struct MovingGhostObjectData
    {
        public float3 Position;
        public float Speed;
        public int OriginalObjectInstanceID;
    }
}
