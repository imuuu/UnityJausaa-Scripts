using UnityEngine;

namespace Game.ChunkSystem
{
    /// <summary>
    /// Data structure representing a moving ghost object.
    /// This can represent any object (e.g., projectiles, mobs) that moves with a given speed.
    /// </summary>
    public struct MovingGhostObject
    {
        public Vector3 Position;
        public float Speed;
        public int OriginalObjectInstanceID;
    }
}
