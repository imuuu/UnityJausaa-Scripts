using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System;
namespace Game.MobStateSystem
{
    public class MobStateMachine : MonoBehaviour, IEnabled
    {   
        [InfoBox("Needs IFeet(StandController) component to be enabled to work properly.")]
        [SerializeField] private bool _enable = false;
        [SerializeField, ReadOnly, PropertySpace(5,5)] private MOB_STATE _currentState;
        [SerializeField, ReadOnly, PropertySpace(5, 5)] private MOB_STATE _forceState = MOB_STATE.NONE;
        private MOB_STATE _previousState;
        [Title("References")]
        [SerializeField, Required] private Transform _rootParent;
        [SerializeField, Required] private Rigidbody _rigidbody;
        private IFeet _feet;

        [Space(5)]
        [SerializeField, BoxGroup("Thresholds")] private float _stateCheckDelay = 0.1f;
        [SerializeField, BoxGroup("Thresholds")] private float _airAxelYThreshold = 0.8f;
        [SerializeField, BoxGroup("Thresholds")] private float _movementThreshold = 0.2f;
        [SerializeField, BoxGroup("Thresholds")] private float _fallenNotMovingStillThreshold = 2f;

        [Space(5)]
        [Tooltip("If true, the mob will be set to y = 0 when standing.")]
        [SerializeField,BoxGroup("Options")] private bool _isForceY_onStand;

        private Dictionary<MOB_STATE, MobStateCondition> _states = new();
        private float _totalTilt = 0;
        private float _timer;

        private Vector3 _lastPosition;

        private bool _isToggleConstraints = true;
        private bool[] _freezePosition = new bool[3];
        private bool[] _freezeRotation = new bool[3];

        [SerializeField, ToggleLeft] private bool _debug = false;

        private void Awake()
        {
            InitializeStates();

            if(_feet == null) _feet = _rootParent.GetComponent<IFeet>();
            if(_feet == null) _feet = _rootParent.GetComponentInChildren<IFeet>();

            if(_feet == null) 
            {
                Debug.LogError("No IFeet component found in the root parent or its children.");
                return;
            }

            if(_feet is IEnabled feetEnabled && _enable)
            {
                if(!feetEnabled.IsEnabled() )
                {
                    Debug.LogError("IFeet component found but is Disable. Please Activate IFeet component(Can be StandController).");
                    _enable = false;
                    return;
                }
            }
        }

        private void InitializeStates()
        {
            RegisterState(new NoneState());
            RegisterState(new StandingState(this));
            RegisterState(new MovingState(this));
            RegisterState(new UpSideDownState(this));
            RegisterState(new OnAirState(this));
            RegisterState(new FallenState(this));
            RegisterState(new FallenAndNotMovingState(this));
            RegisterState(new MobGrabbingState(this));
            RegisterState(new ReadyToFlyState(this));
        }

        private void OnDisable() 
        {
            _currentState = MOB_STATE.NONE;
        }

        private void RegisterState(MobStateCondition state)
        {
            _states[state.GetState()] = state;
        }

        private void Update()
        {
            if(!_enable) return;

            _states[_currentState].UpdateState();

            if(_forceState != MOB_STATE.NONE) return;

            if (_currentState != MOB_STATE.ON_AIR)
            {
                _timer += Time.deltaTime;
                if (_timer < _stateCheckDelay) return;

                _timer = 0;
            }
           

            _totalTilt = transform.GetTilt();
            CheckStateTransitions();
            _lastPosition = transform.position;
        }

        public MOB_STATE GetState()
        {
            return _currentState;
        }

        [ShowIf("_debug")]
        [PropertySpace(10,0)]
        [InfoBox("This will force the state to the given state and never change until another state is forced.")]
        [Button("Force State"), BoxGroup("Buttons")]
        public void SetForceState(MOB_STATE state)
        {
            _forceState = state;
            HandleEvents(_states[state]);
        }

        [ShowIf("_debug")]
        [InfoBox("This will jump to the state and will change to another state if the condition is met")]
        [Button("Jump to State"), BoxGroup("Buttons")]
        public void JumpToState(MOB_STATE state)
        {
            _forceState = state;
            HandleEvents(_states[state]);
        }

        public float GetTilt()
        {
            return _totalTilt;
        }

        private void CheckStateTransitions()
        {
            bool someStateMet = false;
            foreach (MobStateCondition state in _states.Values)
            {
                if (state.IsConditionMet())
                {
                    someStateMet = true;
                    if (_currentState != state.GetState())
                    {
                        HandleEvents(state);
                    }
                    break;
                }
            }

            if (!someStateMet)
            {
                _currentState = MOB_STATE.NONE;
            }
        }

        private void HandleEvents(MobStateCondition nextState)
        {
            if (_states.TryGetValue(_currentState, out MobStateCondition previousStateCondition))
            {
                previousStateCondition.ExitState();
                previousStateCondition.OnExitState?.Invoke();
            }

            _currentState = nextState.GetState();
            nextState.EnterState();

            if (nextState.OnEnterState != null)
            {
                nextState.OnEnterState.Invoke();
            }

            if (nextState.GetState() != MOB_STATE.STAND && nextState.GetState() != MOB_STATE.MOVING && nextState.GetState() != MOB_STATE.NONE)
            {
                if(_rootParent.TryGetComponent<FallDamageOnCollision>(out FallDamageOnCollision fallDamage))
                {
                    fallDamage.enabled = true;
                }
               
            }
            else
            {
                if(_rootParent.TryGetComponent<FallDamageOnCollision>(out FallDamageOnCollision fallDamage))
                {
                    fallDamage.enabled = false;
                }
            }
        }

        public bool IsGrounded()
        {
            return _feet.IsGrounded();
        }

        public bool IsMoving(float threshold)
        {
            //Debug.Log("Distance: " + Vector3.Distance(_lastPosition, transform.position) + " Threshold: " + threshold);
            return Vector3.Distance(_lastPosition, transform.position) > threshold;
        }

        public bool IsFalling()
        {
            return !IsGrounded() && _rigidbody.linearVelocity.y > 0.1f && !IsAir();
        }

        public bool IsAir()
        {
            return _feet.GetFeetPosition().y > _airAxelYThreshold;
        }

        private void OnDrawGizmosSelected() 
        {
            if(_feet == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawCube(_feet.GetFeetPosition(), Vector3.one * 0.1f);
        }

        public float GetMovementThreshold()
        {
            return _movementThreshold;
        }

        public float GetFallenNotMovingStillThreshold()
        {
            return _fallenNotMovingStillThreshold;
        }

        public void AddEnterListener(MOB_STATE state, Action action)
        {
            if (_states.ContainsKey(state))
            {
                _states[state].OnEnterState += action;
            }
        }

        public void RemoveEnterListener(MOB_STATE state, Action action)
        {
            if (_states.ContainsKey(state))
            {
                _states[state].OnEnterState -= action;
            }
        }

        public void AddExitListener(MOB_STATE state, Action action)
        {
            if (_states.ContainsKey(state))
            {
                _states[state].OnExitState += action;
            }
        }

        public void RemoveExitListener(MOB_STATE state, Action action)
        {
            if (_states.ContainsKey(state))
            {
                _states[state].OnExitState -= action;
            }
        }

        public bool IsEnabled()
        {
            return _enable;
        }

        public void SetEnable(bool enable)
        {
            _enable = enable;
        }
        
        public Transform GetRootParent()
        {
            return _rootParent;
        }

        public void ToggleRigidbodyConstraints(bool enable)
        {
            if (_isToggleConstraints == enable) return;

            if (_debug) Debug.Log("Toggle rb Constraints: " + enable);

            if (!enable)
            {
                _freezePosition[0] = (_rigidbody.constraints & RigidbodyConstraints.FreezePositionX) != 0;
                _freezePosition[1] = (_rigidbody.constraints & RigidbodyConstraints.FreezePositionY) != 0;
                _freezePosition[2] = (_rigidbody.constraints & RigidbodyConstraints.FreezePositionZ) != 0;

                _freezeRotation[0] = (_rigidbody.constraints & RigidbodyConstraints.FreezeRotationX) != 0;
                _freezeRotation[1] = (_rigidbody.constraints & RigidbodyConstraints.FreezeRotationY) != 0;
                _freezeRotation[2] = (_rigidbody.constraints & RigidbodyConstraints.FreezeRotationZ) != 0;

                _rigidbody.constraints = RigidbodyConstraints.None;
            }
            else
            {
                if (_freezePosition[0]) _rigidbody.constraints |= RigidbodyConstraints.FreezePositionX;
                if (_freezePosition[1]) _rigidbody.constraints |= RigidbodyConstraints.FreezePositionY;
                if (_freezePosition[2]) _rigidbody.constraints |= RigidbodyConstraints.FreezePositionZ;

                if (_freezeRotation[0]) _rigidbody.constraints |= RigidbodyConstraints.FreezeRotationX;
                if (_freezeRotation[1]) _rigidbody.constraints |= RigidbodyConstraints.FreezeRotationY;
                if (_freezeRotation[2]) _rigidbody.constraints |= RigidbodyConstraints.FreezeRotationZ;
            }

            _isToggleConstraints = enable;
        }

    }

   
}