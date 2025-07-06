using Sirenix.OdinInspector;
using UI;
using UnityEngine;
using UnityEngine.Events;

namespace Game.UI
{
    public class UI_DetachableItem : MonoBehaviour, IDetachable, IHasEmptyUI
    {
        [SerializeField] private bool _isDetachable = true;

        [Tooltip("If true, the item will be detached permanently"
        + "If false, the item will trigger onEmpty and onRestore events")]
        [SerializeField] private bool _isDetachablePermanent = false;
        [SerializeField, ReadOnly] private bool _isEmpty = false;

        [Title("Events")]
        [ShowIf("@!this._isDetachablePermanent")]
        [SerializeField] private UnityEvent _onEmptyEvent;
        [ShowIf("@!this._isDetachablePermanent")]
        [SerializeField] private UnityEvent _onRestoreEvent;

        public bool IsDetachable()
        {
            return _isDetachable;
        }

        public bool IsEmpty()
        {
            return _isEmpty;
        }

        public bool IsPossibleToEmpty()
        {
            return !_isDetachablePermanent;
        }

        public void OnDetach()
        {

        }

        public void OnEmpty()
        {
            if(_isDetachablePermanent) return;

            _onEmptyEvent?.Invoke();
            _isEmpty = true;
        }

        public void OnRestore()
        {
            if(_isDetachablePermanent) return;

            _onRestoreEvent?.Invoke();
            _isEmpty = false;
        }
    }
}