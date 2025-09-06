using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

public class DragAndThrowOnPlane : MonoBehaviour
{
    private Rigidbody pickedRigidbody;
    private Vector3 lastMouseWorldPos;
    private Vector3 mouseDelta;
    // This will store the desired y position for the object when dragged.
    private float dragY = 2f; // default value if autoHeight is disabled

    // Define a plane used for raycasting (its height can be adjusted as needed)
    private Plane dragPlane;

    [Header("Movement Settings")]
    // Base speed factor for dragging.
    public float moveSpeed = 10f;
    // Base multiplier for converting mouse movement into throw speed.
    public float throwMultiplier = 5f;

    [Header("Throw Angle Settings")]
    // Mouse speed (magnitude) corresponding to a horizontal (0°) throw.
    public float minMouseSpeed = 0f;
    // Mouse speed (magnitude) at which the throw will reach the maximum angle.
    public float maxMouseSpeed = 50f;
    // Maximum angle (in degrees) above horizontal for the throw.
    public float maxThrowAngle = 45f;

    [Header("Weight Influence Settings")]
    // Enable this option to have the object's mass affect drag and throw speeds.
    public bool useWeightInfluence = false;
    // Multiplier to adjust how object mass affects dragging speed.
    public float weightDragMultiplier = 1f;
    // Multiplier to adjust how object mass affects throwing speed.
    public float weightThrowMultiplier = 1f;

    [Header("Lift Settings")]
    // Base speed factor for smoothly lifting the object to the correct height.
    public float liftSpeed = 5f;
    // Enable this option to have the object's mass affect the lift speed.
    public bool useWeightInfluenceLift = true;
    // Multiplier to adjust how object mass affects lift speed.
    public float weightLiftMultiplier = 1f;
    // Maximum mass used for lift calculations (objects heavier than this lift at the same speed).
    public float maxLiftWeight = 50f;

    [Header("Auto Height Settings")]
    // If enabled, the object's proper vertical offset is determined automatically.
    public bool autoHeight = true;
    // The ground level (y position) where objects should "rest."
    public float groundLevel = 0f;

    private int _mouseButton = 1;
    
    [Title("Hand Settings")]
    [SerializeField] private Transform _handObject;
    private Transform _handObjectTransform;

    void Start()
    {
        // The plane's height here is less important when autoHeight is enabled.
        dragPlane = new Plane(Vector3.up, new Vector3(0, 0.5f, 0));

        if(_handObject == null) return;

        _handObjectTransform = Instantiate(_handObject, Vector3.zero, Quaternion.identity);
        if(_handObjectTransform.transform.GetComponent<FollowerController>() == null) _handObjectTransform.transform.AddComponent<FollowerController>();
        _handObjectTransform.gameObject.SetActive(false);
    }

    void Update()
    {
        // Use mouse button 2 (middle click) for picking up and dragging.
        if (Input.GetMouseButtonDown(_mouseButton))
        {
            Ray ray = Camera.main.ScreenPointToRay(GamepadCursor.CurrentScreenPosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.rigidbody != null)
                {
                    pickedRigidbody = hit.rigidbody;
                    float enter;
                    if (dragPlane.Raycast(ray, out enter))
                    {
                        lastMouseWorldPos = ray.GetPoint(enter);
                    }
                    pickedRigidbody.useGravity = false;

                    if(_handObjectTransform != null)
                    {
                        _handObjectTransform.gameObject.SetActive(true);
                        if (_handObjectTransform.TryGetComponent(out FollowerController followerController))
                        {
                            followerController._target = pickedRigidbody.transform;
                            followerController._distanceFromTarget = Vector3.Distance(hit.point, pickedRigidbody.transform.position);
                        }
                    }
                   

                    if (autoHeight)
                    {
                        EntityStatistics entityStats = pickedRigidbody.GetComponent<EntityStatistics>();
                        if (entityStats != null)
                        {
                            dragY = groundLevel;
                        }
                        else
                        {
                            Collider col = pickedRigidbody.GetComponent<Collider>();
                            if (col != null)
                            {
                                dragY = groundLevel + col.bounds.extents.y;
                            }
                            else
                            {
                                dragY = groundLevel;
                            }
                        }
                    }
                    else
                    {
                        dragY = 2f;
                    }
                    // Do not snap immediately; the lift will occur smoothly.
                }
            }
        }

        if (pickedRigidbody != null)
        {
            if (Input.GetMouseButton(_mouseButton))
            {
                Ray ray = Camera.main.ScreenPointToRay(GamepadCursor.CurrentScreenPosition);
                float enter;
                Vector3 currentMouseWorldPos = lastMouseWorldPos;
                if (dragPlane.Raycast(ray, out enter))
                {
                    currentMouseWorldPos = ray.GetPoint(enter);
                }
                // Compute mouse movement (used later for throwing).
                mouseDelta = (currentMouseWorldPos - lastMouseWorldPos) / Time.deltaTime;
                lastMouseWorldPos = currentMouseWorldPos;

                Vector3 currentPos = pickedRigidbody.position;

                // --- Horizontal Movement ---
                // For horizontal motion, use the mouse's x and z, but keep the object's current y.
                Vector3 horizontalTarget = new Vector3(currentMouseWorldPos.x, currentPos.y, currentMouseWorldPos.z);
                float effectiveMoveSpeed = moveSpeed;
                if (useWeightInfluence)
                {
                    effectiveMoveSpeed = moveSpeed * (weightDragMultiplier / pickedRigidbody.mass);
                }
                Vector3 horizontalVelocity = (horizontalTarget - currentPos) * effectiveMoveSpeed;

                // --- Vertical Lift ---
                float effectiveLiftSpeed = liftSpeed;
                if (useWeightInfluenceLift)
                {
                    // Use the object's mass but cap it to maxLiftWeight so it doesn't slow down indefinitely.
                    float massForLift = Mathf.Min(pickedRigidbody.mass, maxLiftWeight);
                    effectiveLiftSpeed = liftSpeed * (weightLiftMultiplier / massForLift);
                }
                // Calculate the vertical difference from desired height.
                float verticalDiff = dragY - currentPos.y;
                float verticalVelocity = verticalDiff * effectiveLiftSpeed;

                // Combine horizontal and vertical velocities.
                Vector3 desiredVelocity = horizontalVelocity + new Vector3(0, verticalVelocity, 0);
                pickedRigidbody.linearVelocity = desiredVelocity;
            }

            if (Input.GetMouseButtonUp(_mouseButton))
            {
                if(_handObjectTransform != null) _handObjectTransform.gameObject.SetActive(false);

                float effectiveThrowMultiplier = throwMultiplier;
                if (useWeightInfluence)
                {
                    effectiveThrowMultiplier = throwMultiplier * (weightThrowMultiplier / pickedRigidbody.mass);
                }

                // Compute the base horizontal throw velocity.
                Vector3 baseThrowVelocity = mouseDelta * effectiveThrowMultiplier;
                float totalSpeed = baseThrowVelocity.magnitude;
                if (totalSpeed > 0.001f)
                {
                    // Use mouse movement magnitude to determine the throw angle.
                    float mouseSpeed = mouseDelta.magnitude;
                    float t = Mathf.InverseLerp(minMouseSpeed, maxMouseSpeed, mouseSpeed);
                    float angleDeg = Mathf.Lerp(0, maxThrowAngle, t);
                    float angleRad = angleDeg * Mathf.Deg2Rad;

                    Vector3 horizontalDir = baseThrowVelocity.normalized;
                    Vector3 finalVelocity = horizontalDir * (totalSpeed * Mathf.Cos(angleRad))
                                            + Vector3.up * (totalSpeed * Mathf.Sin(angleRad));

                    pickedRigidbody.linearVelocity = finalVelocity;
                }
                else
                {
                    pickedRigidbody.linearVelocity = Vector3.zero;
                }

                // Re-enable gravity and release the object.
                pickedRigidbody.useGravity = true;
                pickedRigidbody = null;
            }
        }
    }
}
