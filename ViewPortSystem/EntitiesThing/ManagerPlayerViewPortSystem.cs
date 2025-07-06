using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
public struct PlayerViewPortData : IComponentData
{
    public float3 Vertex0;
    public float3 Vertex1;
    public float3 Vertex2;
    public float3 Vertex3;
}

// Data for a trigger object. 'offset' is the hysteresis value,
// and isInside/previousState track whether the object is inside the viewport.
public struct ViewPortTriggerData : IComponentData
{
    public float offset;
    public int isInside;
    public int previousState;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class ManagerPlayerViewPortSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Ensure we have the viewport singleton; otherwise, skip this update.
        if (!SystemAPI.HasSingleton<PlayerViewPortData>())
            return;

        // Retrieve the viewport data using SystemAPI.
        var viewportData = SystemAPI.GetSingleton<PlayerViewPortData>();

        // Convert vertices to 2D (using the XZ plane).
        float2 v0 = new float2(viewportData.Vertex0.x, viewportData.Vertex0.z);
        float2 v1 = new float2(viewportData.Vertex1.x, viewportData.Vertex1.z);
        float2 v2 = new float2(viewportData.Vertex2.x, viewportData.Vertex2.z);
        float2 v3 = new float2(viewportData.Vertex3.x, viewportData.Vertex3.z);

        // Compute inward normals for each edge (assumes vertices in clockwise order).
        float2 n0 = math.normalize(new float2((v1 - v0).y, -(v1 - v0).x));
        float2 n1 = math.normalize(new float2((v2 - v1).y, -(v2 - v1).x));
        float2 n2 = math.normalize(new float2((v3 - v2).y, -(v3 - v2).x));
        float2 n3 = math.normalize(new float2((v0 - v3).y, -(v0 - v3).x));

        // Process every trigger entity in parallel.
        Entities
            .WithBurst()
            .ForEach((ref ViewPortTriggerData trigger, in LocalTransform localTransform) =>
            {
                // Extract the position from the LocalTransform.
                float2 point = new float2(localTransform.Position.x, localTransform.Position.z);

                // Compute signed distances from the point to each edge.
                float d0 = math.dot(point - v0, n0);
                float d1 = math.dot(point - v1, n1);
                float d2 = math.dot(point - v2, n2);
                float d3 = math.dot(point - v3, n3);

                // Minimum distance indicates how close the point is to an edge.
                float minDistance = math.min(math.min(d0, d1), math.min(d2, d3));

                bool currentlyInside = trigger.isInside == 1;

                // Apply hysteresis: change state only when past the offset thresholds.
                if (minDistance >= trigger.offset)
                {
                    if (!currentlyInside)
                    {
                        trigger.previousState = trigger.isInside;
                        trigger.isInside = 1;
                    }
                }
                else if (minDistance <= -trigger.offset)
                {
                    if (currentlyInside)
                    {
                        trigger.previousState = trigger.isInside;
                        trigger.isInside = 0;
                    }
                }
                // If between -offset and offset, maintain the current state.
            }).ScheduleParallel();
    }
}
