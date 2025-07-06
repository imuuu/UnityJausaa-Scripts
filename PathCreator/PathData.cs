using System.Collections.Generic;
using UnityEngine;
namespace Game.PathSystem
{
    // WIP: Might happen to be used in the future.
    //[CreateAssetMenu(menuName = "Path/Create PathData", fileName = "New PathData")]
    public class PathData : ScriptableObject
    {
        public List<Vector3> worldPoints = new List<Vector3>();
        public bool closed = false;

        // Sample point along the saved path (world space)
        public Vector3 GetPointAtTime(float t)
        {
            if (worldPoints.Count < 4) return Vector3.zero;
            if (closed)
            {
                t = t % 1f;
                if (t < 0) t += 1f;
            }
            int segmentCount = (worldPoints.Count - 1) / 3;
            if (closed) segmentCount++;
            float segT = t * segmentCount;
            int segIndex = Mathf.Min(Mathf.FloorToInt(segT), segmentCount - 1);
            float u = segT - segIndex;
            int i = segIndex * 3;
            return Bezier.GetPoint(
                worldPoints[i],
                worldPoints[i + 1],
                worldPoints[i + 2],
                worldPoints[i + 3],
                u
            );
        }

        public float GetPathLength()
        {
            float length = 0f;
            Vector3 prev = GetPointAtTime(0f);
            int steps = 20;
            for (int i = 1; i <= steps; i++)
            {
                Vector3 next = GetPointAtTime(i / (float)steps);
                length += Vector3.Distance(prev, next);
                prev = next;
            }
            return length;
        }
    }
}