using UnityEngine;
using Sirenix.OdinInspector;
using Game.MobStateSystem;
using System;

public class KeepOrientation : MonoBehaviour
{
    private bool _isEnabled = true;
    [FoldoutGroup("Orientation Settings")]
    [SerializeField, Tooltip("If true, the object will keep exactly the same world rotation it had at Awake.")]
    private bool _keepInitialWorldRotation = true;

    [FoldoutGroup("Orientation Settings"), HideIf("_keepInitialWorldRotation"),
     Tooltip("Desired Up direction in world space if NOT keeping initial rotation.")]
    private Vector3 _desiredWorldUp = Vector3.up;

    [FoldoutGroup("Orientation Settings"), HideIf("_keepInitialWorldRotation"),
     Tooltip("Optional Forward direction in world space. If zero, only the Up direction is enforced.")]
    private Vector3 _desiredWorldForward = Vector3.zero;

    [FoldoutGroup("Position Settings"), Tooltip("Source for the X and Z position. 'None' means no override.")]
    [SerializeField] private YPositionSource _yPositionSource = YPositionSource.None;

    [FoldoutGroup("Position Settings"), ShowIf("@this._yPositionSource == YPositionSource.ColliderBoundsCenter"),
     Tooltip("If set, the object will track the X and Z position of this collider's bounds center.")]
    [SerializeField] private Collider _colliderRef;

    [FoldoutGroup("Position Settings"), ShowIf("@this._yPositionSource == YPositionSource.RigidbodyCenterOfMass"),
     Tooltip("If set, the object will track the X and Z position of this Rigidbody's world center of mass.")]
    [SerializeField] private Rigidbody _rigidbodyRef;

    [FoldoutGroup("Position Settings"), Tooltip("Additional offset added to the fixed Y position.")]
    [SerializeField] private float _extraYOffset = 0f;

    // SCALE
    [FoldoutGroup("Scale Settings"), Tooltip("Enables scaling based on height difference from the ground.")]
    [SerializeField] private bool _enableScaling = true;

    [FoldoutGroup("Scale Settings"), Tooltip("Determines how quickly the scale decreases per unit height difference.")]
    [SerializeField] private float _scaleFalloff = 0.1f;

    [FoldoutGroup("Scale Settings"), Tooltip("Minimum scale multiplier applied to the object.")]
    [SerializeField] private float _minScaleMultiplier = 0.1f;

    [FoldoutGroup("Scale Settings"), Tooltip("Maximum scale multiplier (usually 1 for full scale).")]
    [SerializeField] private float _maxScaleMultiplier = 1f;

    [FoldoutGroup("Scale Settings"), Tooltip("Dead zone for height near the ground where the scale remains at initial scale.")]
    [SerializeField] private float _deadZone = 0.1f;

    public enum YPositionSource
    {
        None,
        ColliderBoundsCenter,
        RigidbodyCenterOfMass
    }

    private Quaternion _initialWorldRotation;
    private float _initialY;
    private Vector3 _initialScale;

    private MobStateMachine _mobStateMachine;

    private void Awake()
    {
        _initialWorldRotation = transform.rotation;

        _initialY = transform.position.y;
        _initialScale = transform.localScale;

    }

    private void Start()
    {
        _mobStateMachine = _rigidbodyRef.GetComponentInChildren<MobStateMachine>();

        if (!_mobStateMachine) return;

        _mobStateMachine.AddEnterListener(MOB_STATE.MOVING, OnMobStateMoving);
        _mobStateMachine.AddExitListener(MOB_STATE.MOVING, OnMobStateNotMoving);
    }

    private void OnMobStateMoving()
    {
        _isEnabled = false;
        transform.localScale = _initialScale;
        HandleOrientation();
    }

    private void OnMobStateNotMoving()
    {
        _isEnabled = true;
    }

    private void LateUpdate()
    {
        if(!_isEnabled) return;

        HandleOrientation();
        HandleScale();
    }

    private void HandleOrientation()
    {
        if (_keepInitialWorldRotation)
        {
            transform.rotation = _initialWorldRotation;
        }
        else
        {
            if (_desiredWorldForward.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(
                    _desiredWorldForward.normalized,
                    _desiredWorldUp.normalized
                );
            }
            else
            {
                transform.up = _desiredWorldUp.normalized;
            }
        }

        float groundYPosition = 0f + _extraYOffset;

        if (_yPositionSource == YPositionSource.ColliderBoundsCenter && _colliderRef != null)
        {
            Vector3 refPos = _colliderRef.bounds.center;
            Vector3 pos = transform.position;
            pos.x = refPos.x;
            pos.z = refPos.z;
            pos.y = _initialY + _extraYOffset; // Y is fixed at the initial value.
            transform.position = pos;
        }
        else if (_yPositionSource == YPositionSource.RigidbodyCenterOfMass && _rigidbodyRef != null)
        {
            Vector3 refPos = _rigidbodyRef.worldCenterOfMass;
            Vector3 pos = transform.position;
            pos.x = refPos.x;
            pos.z = refPos.z;
            pos.y = _initialY + _extraYOffset; // Y remains constant.
            transform.position = pos;
        }
        else
        {
            Vector3 pos = transform.position;
            pos.y = groundYPosition;
            transform.position = pos;
        }
    }

    private void HandleScale()
    {
        if(!_enableScaling) return;

        float heightDifference = 0f;
        if (_yPositionSource == YPositionSource.RigidbodyCenterOfMass && _rigidbodyRef != null)
        {
            heightDifference = _rigidbodyRef.worldCenterOfMass.y - (_initialY + _extraYOffset);
        }
        else if (_yPositionSource == YPositionSource.ColliderBoundsCenter && _colliderRef != null)
        {
            heightDifference = _colliderRef.bounds.center.y - (_initialY + _extraYOffset);
        }
        else
        {
            heightDifference = transform.position.y - (_initialY + _extraYOffset);
        }
        heightDifference = Mathf.Max(heightDifference, 0f);

        float scaleMultiplier = 1f;
        if (heightDifference <= _deadZone)
        {
            scaleMultiplier = 1f;
        }
        else
        {
            float effectiveHeight = heightDifference - _deadZone;
            scaleMultiplier = Mathf.Clamp(1f - effectiveHeight * _scaleFalloff, _minScaleMultiplier, _maxScaleMultiplier);
        }

        transform.localScale = _initialScale * scaleMultiplier;
    }

    public bool IsEnabled()
    {
        return _isEnabled;
    }

    public void SetEnable(bool enable)
    {
        _isEnabled = enable;
    }
}
