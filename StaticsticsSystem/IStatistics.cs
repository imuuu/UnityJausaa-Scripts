
using UnityEngine;

public interface IStatistics : IWidth
{
    public float Height { get; }
    public Vector3 FeetPosition { get; }

    public Vector3 HeadPosition { get; }
}
