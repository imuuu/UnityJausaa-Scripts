using System.Collections.Generic;
using Game;
using UnityEngine;


//TODO so that uses job system like PlayerViewport
[DefaultExecutionOrder(-100)]
public class ManagerMobMovement : MonoBehaviour 
{
    public static ManagerMobMovement Instance { get; private set; }
    private LinkedList<IMovement> _mobMovements = new ();
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("There should only be one ManagerMobMovement in the scene.");
            Destroy(this);
        }
    }

    private void FixedUpdate() 
    {
        if(ManagerPause.IsPaused()) return;

        foreach (IMovement mobMovement in _mobMovements)
        {
            if(mobMovement is IEnabled && !(mobMovement as IEnabled).IsEnabled()) continue;

            if(!mobMovement.IsMovementEnabled()) continue;

            mobMovement.UpdateMovement(Time.fixedDeltaTime);
        }

#if UNITY_EDITOR
        DebugUpdate();
#endif
    }

    public void RegisterMob(IMovement mobMovement)
    {
        if (!_mobMovements.Contains(mobMovement))
        {
            _mobMovements.AddLast(mobMovement);

#if UNITY_EDITOR
            if (mobMovement is MonoBehaviour mobMonoBehaviour)
            {
                _mobTransforms.Add(mobMonoBehaviour.transform);
            }
#endif
        }
    }

    public void UnregisterMob(IMovement mobMovement)
    {
        if (_mobMovements.Contains(mobMovement))
        {
            _mobMovements.Remove(mobMovement);

#if UNITY_EDITOR
            if (mobMovement is MonoBehaviour mobMonoBehaviour)
            {
                _mobTransforms.Remove(mobMonoBehaviour.transform);
            }
#endif

        }
    }


#if UNITY_EDITOR
    
    private List<Transform> _mobTransforms = new ();

    private void DebugUpdate()
    {
        _mobTransforms.Clear();
        foreach (IMovement mobMovement in _mobMovements)
        {
            _mobTransforms.Add((mobMovement as MonoBehaviour).transform);
        }
    }
#endif


}