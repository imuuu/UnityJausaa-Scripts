using UnityEngine;
using Unity.Entities;
using UnityEngine.Events;

// Authoring component for the viewport trigger.
public class ViewPortTriggerAuthoring : MonoBehaviour
{
    [Tooltip("How far inside/outside the polygon to trigger the event.")]
    public float offset = 0f;

    public UnityEvent onEnter;
    public UnityEvent onExit;

    public void OnEnter()
    {
        Debug.Log("222OnEnter");
        onEnter?.Invoke();
    }

    public void OnExit()
    {
        Debug.Log("22OnExit");
        onExit?.Invoke();
    }


    public class ViewPortTriggerBaker : Baker<ViewPortTriggerAuthoring>
    {
        public override void Bake(ViewPortTriggerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // 1) Add your DOTS-friendly component:
            AddComponent(entity, new ViewPortTriggerData
            {
                offset = authoring.offset,
                isInside = 0,
                previousState = 0
            });

            // 2) Attach the MonoBehaviour reference to the same entity
            //    so we can retrieve and call 'onEnter' / 'onExit' later.
            AddComponentObject(entity, authoring);
        }
    }
}
