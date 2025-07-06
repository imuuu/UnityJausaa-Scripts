using UnityEngine;
using Unity.Cinemachine;

public class SetCameraTarget : MonoBehaviour
{
    [SerializeField] private string targetObjectName = "c_traj"; // Name of the child under Player
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] GameObject playerRoot;


    void Start()
    {
        playerRoot = GameObject.FindGameObjectWithTag("Player");

        if (playerRoot != null)
        {            
            Transform targetTransform = FindChildRecursive(playerRoot.transform, targetObjectName);
            
            if (targetTransform != null)
            {
                if (virtualCamera != null)
                {
                    virtualCamera.Target.CustomLookAtTarget = targetTransform;
                    virtualCamera.Follow = targetTransform;
                    virtualCamera.LookAt = targetTransform; // Optional
                }
                else
                {
                    Debug.LogWarning("Virtual Camera not assigned.");
                }
            }
            else
            {
                Debug.LogWarning($"Child object '{targetObjectName}' not found under Player.");
            }
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'Player' found in the scene.");
        }
    }
    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindChildRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
