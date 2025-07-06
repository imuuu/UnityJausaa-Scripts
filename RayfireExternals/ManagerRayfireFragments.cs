using UnityEngine;
using RayFire;
using Pathfinding;

/// <summary>
/// this is currently testing and helping understand how to handle fragments
/// TODO add this as global due to its global handler
/// </summary> 
public class ManagerRayfireFragments : MonoBehaviour
{
    private void OnEnable()
    {
        RFDemolitionEvent.GlobalEvent += OnFragmented;
    }

    private void OnDisable()
    {
        RFDemolitionEvent.GlobalEvent -= OnFragmented;
    }

    private void OnFragmented(RayfireRigid rigid)
    {
        foreach (var fragment in rigid.fragments)
        {
            if(fragment.gameObject.GetComponent<Collider>() == null)
            {
                continue;
            }

            DynamicObstacle dynamicObstacle = fragment.gameObject.GetComponent<DynamicObstacle>();
            if(dynamicObstacle == null)
            {
                dynamicObstacle = fragment.gameObject.AddComponent<DynamicObstacle>();
            }
        }
    }

}
