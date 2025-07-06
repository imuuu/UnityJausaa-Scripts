using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Shapes;
using DG.Tweening;
using Game.MobStateSystem;
using Game;

public class StandController : MonoBehaviour, IFeet, IEnabled
{
    [SerializeField, BoxGroup("Booleans")] private bool _enable = false;
    [Tooltip("Automatically stand up when fallen and not moving")]
    [SerializeField, ShowIf("_enable"), BoxGroup("Booleans")] private bool _automateStand = false;

    [SerializeField, BoxGroup("References"), Required] private Transform _rootParent;
    [SerializeField, BoxGroup("References"), Required] private MobStateMachine _mobStateMachine;
    [SerializeField, BoxGroup("References"), Required] private Rigidbody _rigidbody;

    [ShowIf("_enable")]
    [BoxGroup("Feet Shape")]
    [TypeFilter(nameof(GetFilteredTypeList))]
    [OdinSerialize, SerializeReference, InlineProperty]
    private ShapeBase _feetShapeType;

    [ShowIf("_enable")]
    [BoxGroup("Feet Shape"), HorizontalGroup("Feet Shape/Offset")]
    [SerializeField, LabelText("Position Offset")]
    private Vector3 _offset;

    
    [BoxGroup("Stand Animation")]
    [SerializeField, ShowIf("_enable"), HorizontalGroup("Stand Animation/Settings", Width = 0.5f)]
    private Ease _easeTypeStand;
    [BoxGroup("Stand Animation")]
    [SerializeField, ShowIf("_enable"), HorizontalGroup("Stand Animation/Settings", Width = 0.5f), LabelText("Duration (sec)")]
    private float _durationStand = 0.75f;

    [BoxGroup("UpsideDown Animation")]
    [SerializeField, ShowIf("_enable"), HorizontalGroup("UpsideDown Animation/Settings", Width = 0.5f)]
    private Ease _easeTypeUpsideDown;
    [BoxGroup("UpsideDown Animation")]
    [SerializeField, ShowIf("_enable"), HorizontalGroup("UpsideDown Animation/Settings", Width = 0.5f), LabelText("Duration (sec)")]
    private float _durationUpsideDown = 0.75f;
     [BoxGroup("UpsideDown Animation")]
    [SerializeField, ShowIf("_enable"), LabelText("Angle")]
    private float _angleUpsideDown = 40f;


    [PropertySpace(10, 10)]
    [SerializeField, ShowIf("_enable"), BoxGroup("Ground Check")] private LayerMask _groundLayers;
    [SerializeField, ShowIf("_enable"), BoxGroup("Ground Check")] private float _groundCheckDistance = 0.1f;
    [SerializeField, ShowIf("_enable"), BoxGroup("Ground Check")] private bool _debugGroundCheck;

    [TitleGroup("Debug")]
    [SerializeField, ShowIf("_enable"), ToggleLeft]
    private bool _debugShowClosestPoint;

    private Vector3 _closestPoint;
    private Vector3 _testPoint;
    private bool _rbGravityState;
    private bool _rbKinematicState;
    private bool _isRbEnabled = true;
    private ShapeBase _testUpsideDownShape;
    private Tween _tweenUpsideDown;
    private Tween _tweenStand;

    private void OnEnable()
    {
        if(!_automateStand || !_enable) return;

        ActionScheduler.RunNextFrame(() =>
        {
            _mobStateMachine.AddEnterListener(MOB_STATE.FALLEN_NOT_MOVING, OnFallenNotMoving);
            _mobStateMachine.AddEnterListener(MOB_STATE.UP_SIDE_DOWN, RotateFromUpsideDown);
        });
        //_mobStateMachine.AddEnterListener(MOB_STATE.FALLEN_NOT_MOVING, OnFallenNotMoving);
    }
    private void OnDisable() 
    {
        if (!_automateStand || !_enable) return;

        _mobStateMachine.RemoveEnterListener(MOB_STATE.FALLEN_NOT_MOVING, OnFallenNotMoving);
        _mobStateMachine.RemoveEnterListener(MOB_STATE.UP_SIDE_DOWN, RotateFromUpsideDown);
    }

    private void OnFallenNotMoving()
    {
        RotateStand();
    }

    private void ToggleRigidbody()
    {
        if(_isRbEnabled)
        {
            _rbGravityState = _rigidbody.useGravity;
            _rbKinematicState = _rigidbody.isKinematic;

            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = Vector3.zero;
        }
        else
        {
            _rigidbody.useGravity = _rbGravityState;
            _rigidbody.isKinematic = _rbKinematicState;
        }

        _isRbEnabled = !_isRbEnabled;
    }

    public bool IsGrounded()
    {
        return CheckGround(GetFeetPosition());
    }

    private bool CheckGround(Vector3 origin)
    {
        return Mathf.Abs(origin.y) < _groundCheckDistance;
        //return Physics.Raycast(origin, -GetTransform().up, _groundCheckDistance, _groundLayers);
    }

    public Vector3 GetFeetPosition()
    {
        _feetShapeType.SetLikeParentTransform(GetTransform(), _offset);
        return _feetShapeType.Center;
    }

    public Transform GetTransform()
    {
        return _rootParent;
    }
    private void OnDrawGizmosSelected()
    {
        if (_feetShapeType == null || !_enable)
            return;

        if(_debugGroundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(GetFeetPosition(), -GetTransform().up * _groundCheckDistance);
        }
        if(_debugShowClosestPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_testPoint, 0.1f);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_closestPoint, 0.05f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_testPoint, _closestPoint);
        }

        _feetShapeType.SetLikeParentTransform(GetTransform(), _offset);

        Vector3[] vertices = _feetShapeType.GetVertices();

        if (vertices != null && vertices.Length > 1)
        {
            ShapeBuilder.DrawShapeGizmos(vertices, Color.white);
        }

        if(_testUpsideDownShape != null)
        {
            ShapeBuilder.DrawShapeGizmos(_testUpsideDownShape.GetVertices(), Color.cyan);
        }
    }

    [PropertySpace(5, 5)]
    [BoxGroup("Buttons"), ShowIf("_enable"),Button("SHOW CLOSEST POINT")]
    private void GetClosestPointOnShape()
    {
        _debugShowClosestPoint = true;
        _feetShapeType.SetLikeParentTransform(GetTransform(), _offset);
        _testPoint = _feetShapeType.Center + Vector3.down * 10f;
        _closestPoint = _feetShapeType.GetClosestPoint(_testPoint);
    }

    [PropertySpace(5, 5)]
    [BoxGroup("Buttons"), ShowIf("_enable"), Button("FORCE STAND")]
    public void RotateStand()
    {
        if(!_enable) return;

        if(_feetShapeType == null)
        {
            Debug.LogError("Feet Shape Type is null");
            return;
        }

        _feetShapeType.SetLikeParentTransform(GetTransform(), _offset);

        _testPoint = _feetShapeType.Center + Vector3.down * 10f;
        _closestPoint = _feetShapeType.GetClosestPoint(_testPoint);

        Quaternion finalRotation = Quaternion.Euler(0f, GetTransform().localEulerAngles.y, 0f);
        _tweenStand = GetTransform().DORotateAroundPivot(_closestPoint, finalRotation, _durationStand);
        _tweenStand.SetEase(_easeTypeStand);
        _tweenStand.OnComplete(() =>
        {
            GetTransform().position = new Vector3(GetTransform().position.x, 0, GetTransform().position.z);
        });
        _tweenStand.Play();
    }

    [Button("ROTATE FROM UPSIDE DOWN"), ShowIf("_enable")]
    public void RotateFromUpsideDown()
    {
        if(!_enable) return;

        if(_feetShapeType == null)
        {
            Debug.LogError("Feet Shape Type is null");
            return;
        }

        if(_tweenUpsideDown != null && _tweenUpsideDown.IsActive())
        {
            _tweenUpsideDown.Kill();
        }

        _testUpsideDownShape = (ShapeBase) SerializationUtility.CreateCopy(this._feetShapeType);

        Collider collider = _rootParent.GetComponentInChildren<Collider>();
        _testUpsideDownShape.Center = _testUpsideDownShape.Center = GetColliderTop(collider);

        Vector3 freeDirection = Vector3.zero;
        ActionScheduler.CancelActions(this.gameObject.GetInstanceID());
        if (!RaycastHelper.TryGetFirstFreeDirectionSidesChecks(collider, out freeDirection, randomAfterBack: true, raycastDistance: 2f))
        {
            //Debug.LogError("No free direction found (sides)!");

            ActionScheduler.RunAfterDelay(2f, () => 
            {
                if(_mobStateMachine.GetState() == MOB_STATE.UP_SIDE_DOWN)
                    RotateFromUpsideDown();

            }, identifier: this.gameObject.GetInstanceID());
            return;
        }

        bool isAlignedWithX = AxisAlignmentHelper.IsAlignedWithXAxis(GetTransform(),freeDirection, 0.1f);

        //Debug.Log($"Free direction: {freeDirection} | Is aligned with X: {isAlignedWithX}");
        Debug.DrawRay(collider.bounds.center, freeDirection * 2f, Color.cyan, 2f);

        Vector3 testPoint = _testUpsideDownShape.Center + freeDirection * 10f;
        Vector3 closestPoint = _testUpsideDownShape.GetClosestPoint(testPoint);

        MarkHelper.DrawSphereTimed(closestPoint, 0.1f, 5f, Color.green);

        // Debug.Log("GetSide: "+AxisAlignmentHelper.GetAxisSides(GetTransform(), freeDirection));
        // Debug.Log("=>GetSides booleans: "+AxisAlignmentHelper.GetAxisPositives(GetTransform(), freeDirection));
        // Debug.Log("Rotation: "+GetTransform().rotation);

        Quaternion finalRotation = Quaternion.identity;
        float offset = _angleUpsideDown;
        if(GetTransform().rotation.x > 0.5f)
        {
            if (isAlignedWithX)
            {
                float rotationValue = 180f - offset;
                if (freeDirection.x > 0)
                {
                    rotationValue = 180 + offset;
                }
                finalRotation = Quaternion.Euler(GetTransform().localEulerAngles.x, GetTransform().localEulerAngles.y, rotationValue);
            }
            else
            {
                float rotationValue = -offset;
                if (freeDirection.z < 0)
                {
                    rotationValue = offset;
                }
                finalRotation = Quaternion.Euler(rotationValue, GetTransform().localEulerAngles.y, GetTransform().localEulerAngles.z);
            }
        }
        else
        {
            if (isAlignedWithX)
            {
                float rotationValue = 180f + offset;
                if (freeDirection.x > 0)
                {
                    rotationValue = 180 - offset;
                }
                finalRotation = Quaternion.Euler(GetTransform().localEulerAngles.x, GetTransform().localEulerAngles.y, rotationValue);
            }
            else
            {
                float rotationValue = offset;
                if (freeDirection.z < 0)
                {
                    rotationValue = - offset;
                }
                finalRotation = Quaternion.Euler(rotationValue, GetTransform().localEulerAngles.y, GetTransform().localEulerAngles.z);
            }
        }


        _tweenUpsideDown = GetTransform().DORotateAroundPivot(closestPoint, finalRotation, _durationUpsideDown);

        _tweenUpsideDown.SetEase(_easeTypeUpsideDown);
        _tweenUpsideDown.Play();
    }

    /// <summary>
    /// Returns the world-space top-center of any collider,
    /// accounting for its type, rotation, and scale.
    /// </summary>
    private Vector3 GetColliderTop(Collider col)
    {
        if (col == null)
            return Vector3.zero;

        if (col is BoxCollider box)
        {
            Vector3 worldCenter = box.transform.TransformPoint(box.center);
            float halfHeight = (box.size.y * 0.5f) * Mathf.Abs(box.transform.lossyScale.y);
            return worldCenter + box.transform.up * halfHeight;
        }
        else if (col is SphereCollider sphere)
        {
            Vector3 worldCenter = sphere.transform.TransformPoint(sphere.center);
            float radiusScale = Mathf.Max(
                Mathf.Abs(sphere.transform.lossyScale.x),
                Mathf.Abs(sphere.transform.lossyScale.y),
                Mathf.Abs(sphere.transform.lossyScale.z)
            );
            float scaledRadius = sphere.radius * radiusScale;
            return worldCenter + sphere.transform.up * scaledRadius;
        }

        else if (col is CapsuleCollider capsule)
        {
            Vector3 worldCenter = capsule.transform.TransformPoint(capsule.center);
            float scaleFactor = 1f;
            Vector3 up = Vector3.up;
            switch (capsule.direction)
            {
                case 0:
                    scaleFactor = Mathf.Abs(capsule.transform.lossyScale.x);
                    up = capsule.transform.right;
                    break;
                case 1:
                    scaleFactor = Mathf.Abs(capsule.transform.lossyScale.y);
                    up = capsule.transform.up;
                    break;
                case 2:
                    scaleFactor = Mathf.Abs(capsule.transform.lossyScale.z);
                    up = capsule.transform.forward;
                    break;
            }
            float halfHeight = capsule.height * 0.5f * scaleFactor;
            return worldCenter + up * halfHeight;
        }
        else
        {
            Debug.LogWarning($"STAND CONTROLLER Unsupported collider type: {col.GetType()}");
            // For any other collider types (like MeshCollider), fallback to using collider.bounds.
            // Note: bounds are axis-aligned, so if the collider is rotated, this may not match the visual top.
            return new Vector3(col.bounds.center.x, col.bounds.max.y, col.bounds.center.z);
        }
    }


    #region Odin
    public IEnumerable<Type> GetFilteredTypeList()
    {
        return typeof(ShapeBase).Assembly.GetTypes()
            .Where(t => !t.IsAbstract)
            .Where(t => typeof(ShapeBase).IsAssignableFrom(t));
    }

    public bool IsEnabled()
    {
        return _enable;
    }

    public void SetEnable(bool enable)
    {
        _enable = enable;
    }

    #endregion
}
