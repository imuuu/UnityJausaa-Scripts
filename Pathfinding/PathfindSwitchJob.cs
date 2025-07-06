using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
/// <summary>
/// A Burst–compiled job that checks whether each switch’s position is within the player’s distance threshold.
/// </summary>
[BurstCompile]
struct PathfindSwitchJob : IJobParallelFor
{
    public float3 playerPos;
    public float switchDistanceSq; // squared threshold

    [ReadOnly]
    public NativeArray<float3> switchPositions;
    public NativeArray<bool> results;

    public void Execute(int index)
    {
        float3 pos = switchPositions[index];
        // Use distancesq for efficiency.
        float distSq = math.distancesq(playerPos, pos);
        results[index] = distSq <= switchDistanceSq;
    }
}
