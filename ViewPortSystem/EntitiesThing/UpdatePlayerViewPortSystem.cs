using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class UpdatePlayerViewPortSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // For simplicity, assume the same entity has both PlayerViewPortData and LocalTransform
        // (i.e. your “player” entity). If your player and viewport are separate entities,
        // you'd need a different approach to find the player's transform.
        Entities
            .WithBurst()
            .ForEach((ref PlayerViewPortData viewport, in LocalTransform transform) =>
            {
                float3 center = transform.Position;

                // Example: define a 10x10 square around the player in XZ
                float halfSize = 5f;
                viewport.Vertex0 = center + new float3(-halfSize, 0, -halfSize);
                viewport.Vertex1 = center + new float3(-halfSize, 0, halfSize);
                viewport.Vertex2 = center + new float3(halfSize, 0, halfSize);
                viewport.Vertex3 = center + new float3(halfSize, 0, -halfSize);

                // If your camera angle is always the same (like Diablo),
                // you might not need any rotation logic.
            })
            .ScheduleParallel();
    }
}
