using System.Collections.Generic;
using Game.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
public class OwnerChild : MonoBehaviour, IOwner
{
    [SerializeField]
    private bool _findOwnerFromParents = false;
    [SerializeField,HideIf(nameof(_findOwnerFromParents))] private Owner _owner;

    private void Awake()
    {
        if (_findOwnerFromParents)
        {
            List<Owner> owners = new(1);
            this.transform.TraverseParents<Owner>(owners,
            maxDepth: 10,
            includeSelf: false,
            includeInactive: false,
            breakOnFirstMatch: true);

            if(owners.Count >0 ) _owner = owners[0];
        }
    }

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