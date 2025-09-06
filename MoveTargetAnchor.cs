using System.Collections;
using UnityEngine;

/// <summary>
/// Marks a transform as a move target anchor. 
/// Put this on the transform you want to land at (e.g., Main Camera -> ChestTransformPoint).
/// </summary>
public class MoveTargetAnchor : MonoBehaviour
{
    [SerializeField] private Transform _point; // Optional override; defaults to this transform
    public Transform Point => _point != null ? _point : transform;

    private void Reset()
    {
        _point = transform;
    }
}