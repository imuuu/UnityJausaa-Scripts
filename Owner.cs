using UnityEngine;
public class Owner : MonoBehaviour, IOwner
{
    [SerializeField] private OWNER_TYPE _ownerType;
    private IOwner _manipulatedOwner;

    public void SetOwner(OWNER_TYPE ownerType)
    {
        _ownerType = ownerType;
    }

    public OWNER_TYPE GetOwnerType()
    {
        return _ownerType;
    }

    public IOwner GetRootOwner()
    {
        return this;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public bool IsManipulated()
    {
        return _manipulatedOwner != null;
    }

    public IOwner GetManipulatedOwner()
    {
        return _manipulatedOwner;
    }

    public void SetManipulatedOwner(IOwner owner)
    {
        _manipulatedOwner = owner;
    }
}