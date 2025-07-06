using Unity.Entities;
using UnityEngine;

// This system checks each trigger's state (inside/outside) and invokes UnityEvents 
// on the corresponding MonoBehaviour when a state change occurs.
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ManagerPlayerViewPortSystem))] // Ensure we run after the system that sets 'isInside'
public partial class ViewPortTriggerEventSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Because we need to call managed code (UnityEvents),
        // we'll disable Burst and run on the main thread.
        Entities
            .WithoutBurst()
            .ForEach((Entity entity, ref ViewPortTriggerData trigger) =>
            {
                // 1) Check if the state changed since last frame
                if (trigger.isInside != trigger.previousState)
                {
                    Debug.Log($"Entity {entity.Index} changed state: {trigger.isInside}");
                    // 2) Retrieve the MonoBehaviour reference that was added via 'AddComponentObject' in the Baker
                    var mb = EntityManager.GetComponentObject<ViewPortTriggerAuthoring>(entity);
                    if (mb != null)
                    {
                        // 3) If we just entered the viewport, call onEnter
                        if (trigger.isInside == 1)
                        {
                            mb.OnEnter();
                        }
                        // 4) If we just exited the viewport, call onExit
                        else
                        {
                            mb.OnExit();
                        }
                    }

                    // 5) Update previousState so we don't repeatedly fire the same event
                    trigger.previousState = trigger.isInside;
                }
            })
            .Run(); // .Run() so it executes immediately on the main thread
    }
}
