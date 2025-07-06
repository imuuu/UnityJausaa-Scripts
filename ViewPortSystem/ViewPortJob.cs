using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
struct ViewPortJob : IJobParallelFor
{
    public float2 v0;
    public float2 v1;
    public float2 v2;
    public float2 v3;

    public float2 n0;
    public float2 n1;
    public float2 n2;
    public float2 n3;

    [ReadOnly]
    public NativeArray<float2> triggerPositions; // effective positions (with offset already applied, if needed)
    [ReadOnly]
    public NativeArray<float> triggerOffsets;

    // Output: computed inside state per trigger
    public NativeArray<bool> results;

    public void Execute(int index)
    {
        float2 point = triggerPositions[index];
        float offset = triggerOffsets[index];

        // Compute distances for each edge
        float d0 = math.dot(point - v0, n0);
        float d1 = math.dot(point - v1, n1);
        float d2 = math.dot(point - v2, n2);
        float d3 = math.dot(point - v3, n3);

        float minDistance = math.min(math.min(d0, d1), math.min(d2, d3));

        // Determine state (example: inside if minDistance >= 0)
        results[index] = (minDistance >= 0);
    }
}