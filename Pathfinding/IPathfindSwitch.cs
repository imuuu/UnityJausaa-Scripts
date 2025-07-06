using UnityEngine;
/// <summary>
/// Interface for any component that needs its pathfinding to be switched on/off.
/// </summary>
public interface IPathfindSwitch
{

    public Transform GetTransform();
    /// <summary>
    /// Width used for offsetting the left/right raycasts.
    /// </summary>
    public float GetWidth();
    /// <summary>
    /// Called by the manager to switch pathfinding mode on or off.
    /// </summary>
    /// <param name="isPathFinding">If true, enables pathfinding (and disables movement) and vice‐versa.</param>
    public void SwitchPathFindingOn(bool isPathFinding);

}
