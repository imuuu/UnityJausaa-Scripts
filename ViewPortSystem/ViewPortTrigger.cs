using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class ViewPortTrigger : MonoBehaviour, IViewPortTrigger
{
    [SerializeField] private float _offset;

    [SerializeField, ReadOnly] private bool _isInside;

    [SerializeField] private UnityEvent _onEnter;
    [SerializeField] private UnityEvent _onExit;

    private void OnEnable()
    {
        ActionScheduler.RunNextFrame(() =>
        {
            ManagerPlayerViewPort.Instance.RegisterViewPortTrigger(this);
        });
    }

    private void OnDisable()
    {
        ManagerPlayerViewPort.Instance.UnregisterViewPortTrigger(this);
    }

    public void OnEnter()
    {
        _onEnter?.Invoke();
    }

    public void OnExit()
    {
        _onExit?.Invoke();
    }

    public float GetOffset()
    {
        return _offset;
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public bool IsInside()
    {
        return _isInside;
    }

    public void SetInside(bool value)
    {
        _isInside = value;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(transform.position, transform.position + Vector3.forward * _offset);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.back * _offset);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * _offset);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * _offset);
    }
}