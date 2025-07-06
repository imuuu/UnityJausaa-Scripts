using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.MobStateSystem
{
    public class MobStateEventTrigger : MonoBehaviour 
    {
        [SerializeField] private bool _getMobStateMachine = true;
        [SerializeField,HideIf("_getMobStateMachine")] private MobStateMachine _mobStateMachine;

        [BoxGroup("Event Trigger", showLabel: false), PropertySpace(8,8)]
        [SerializeField] private MOB_STATE[] _stateToTrigger;

        [SerializeField, ToggleLeft, BoxGroup("Enter", showLabel: false)] private bool _enableEnterEvent = false;
        [SerializeField, ShowIf("_enableEnterEvent"), BoxGroup("Enter")] UnityEvent _onStateEnter;

        [SerializeField, ToggleLeft, BoxGroup("Exit", showLabel: false)] private bool _enableExitEvent = false;
        [SerializeField, ShowIf("_enableExitEvent"), BoxGroup("Exit")] UnityEvent _onStateExit;

        void Awake()
        {
            if(_getMobStateMachine) _mobStateMachine = GetComponent<MobStateMachine>();
        }

        public void Start()
        {
            if(_mobStateMachine == null)
            {
                Debug.LogError("MobStateMachine is not set.");
                return;
            }

            for(int i = 0; i < _stateToTrigger.Length; i++)
            {
                if(_enableEnterEvent) _mobStateMachine.AddEnterListener(_stateToTrigger[i], OnStateEnter);
                
                if(_enableExitEvent) _mobStateMachine.AddExitListener(_stateToTrigger[i], OnStateExit);
            }

        }

        private void OnStateExit()
        {
            _onStateExit?.Invoke();
        }

        private void OnStateEnter()
        {
            _onStateEnter?.Invoke();
        }
    }
}