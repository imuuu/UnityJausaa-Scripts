using UnityEngine;

public interface IOwner
{
    public void SetOwner(OWNER_TYPE ownerType);
    public OWNER_TYPE GetOwnerType();

    public IOwner GetRootOwner();

    public GameObject GetGameObject();

    public bool IsManipulated();
    public IOwner GetManipulatedOwner();
    public void SetManipulatedOwner(IOwner owner);
}