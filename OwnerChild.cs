using UnityEngine;
public class OwnerChild : MonoBehaviour, IOwner
{
    [SerializeField] private Owner _owner;
    public void SetOwner(OWNER_TYPE ownerType)
    {
        _owner.SetOwner(ownerType);
    }

    public OWNER_TYPE GetOwnerType()
    {
        return _owner.GetOwnerType();
    }

    public IOwner GetRootOwner()
    {
        return _owner;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public bool IsManipulated()
    {
        return _owner.IsManipulated();
    }

    public IOwner GetManipulatedOwner()
    {
        return _owner.GetManipulatedOwner();
    }

    public void SetManipulatedOwner(IOwner owner)
    {
        _owner.SetManipulatedOwner(owner);
    }
}