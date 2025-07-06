using UnityEngine;

public class TestRaycastDirectionChecks : MonoBehaviour
{
    [SerializeField] private Collider targetCollider;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Top-Bottom check:
            if (RaycastHelper.TryGetFirstFreeDirectionTopBottomChecks(targetCollider, out Vector3 freeDir, randomAfterBack: true, raycastDistance: 2f))
            {
                Debug.Log("Free direction (top/bottom): " + freeDir);
                Debug.DrawRay(targetCollider.bounds.center, freeDir * 2f, Color.green, 2f);
            }
            else
            {
                Debug.Log("No free direction found (top/bottom)!");
            }

            // Sides check:
            if (RaycastHelper.TryGetFirstFreeDirectionSidesChecks(targetCollider, out Vector3 freeSideDir, randomAfterBack: true, raycastDistance: 2f))
            {
                Debug.Log("Free direction (sides): " + freeSideDir);
                Debug.DrawRay(targetCollider.bounds.center, freeSideDir * 2f, Color.cyan, 2f);
            }
            else
            {
                Debug.Log("No free direction found (sides)!");
            }
        }
    }
}
