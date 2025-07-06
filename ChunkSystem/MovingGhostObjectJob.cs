using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

namespace Game.ChunkSystem
{
    /// <summary>
    /// Burst-compiled job to update moving ghost objects toward the player.
    /// </summary>
    [BurstCompile]
    public struct MovingGhostObjectJob : IJobParallelFor
    {
        public NativeArray<MovingGhostObjectData> GhostData;
        public float DeltaTime;
        public float3 PlayerPosition;

        public void Execute(int index)
        {
            var ghost = GhostData[index];
            float3 direction = math.normalize(PlayerPosition - ghost.Position);
            ghost.Position += direction * ghost.Speed * DeltaTime;
            GhostData[index] = ghost;
        }
    }
}
