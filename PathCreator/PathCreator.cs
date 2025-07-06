
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.PathSystem
{
    // Path Creator: A tool for creating and manipulating paths in Unity.
    // Supports linear and bezier paths, with options for closed loops and control modes.
    public enum ControlMode { Free, Aligned }
    public enum PathSpace { XY, XZ, XYZ }
    public enum DrawMode { Bezier, Linear }

    [ExecuteInEditMode]
    public class PathCreator : MonoBehaviour
    {
       

        [Header("Path Settings")]
        [SerializeField] private bool _useTransform = true;
        [SerializeField] private bool _applyRotation = true;
        [SerializeField] private bool _closed = false;
        [SerializeField] private PathSpace _space = PathSpace.XZ;
        [SerializeField] private ControlMode _controlMode = ControlMode.Aligned;

        [Header("Draw Settings")]
        [SerializeField] private DrawMode _drawMode = DrawMode.Bezier;
        [Range(1, 50)][SerializeField] private int _drawResolution = 10;
        [SerializeField] private float _gizmoSize = 0.03f;

        [Header("Edit Settings")]
        [BoxGroup("Edit Settings", showLabel: false)] public bool IsEditable = true;
        [BoxGroup("Edit Settings")] public bool ShowHandles = true;
        [BoxGroup("Edit Settings")] public bool ShowGizmos = true;

        [HideInInspector]
        public List<Vector3> points = new List<Vector3> {
        new Vector3(0,0,0),
        new Vector3(1,0,0)
    };

        public int NumPoints => points.Count;

        [SerializeField] private bool _directionSet = false;
        [SerializeField] private Vector3 _lockedDirection = Vector3.zero;

        public void SetLockedDirection(Vector3 dir)
        {
            _lockedDirection = dir;
            _directionSet = true;
        }
        
        [Button("Enable direction")]
        public void EnableLockedDirection()
        {
            _directionSet = true;
        }

        [Button("Clear Direction")]
        public void ClearLockedDirection()
        {
            _lockedDirection = Vector3.zero;
            _directionSet = false;
        }
        // public Vector3 GetPoint(int i)
        // {
        //     Vector3 p = points[i];
        //     if (_space == PathSpace.XZ) p = new Vector3(p.x, 0, p.y);
        //     else if (_space == PathSpace.XY) p = new Vector3(p.x, p.y, 0);

        //     if(_directionSet)
        //     {
        //         p = transform.position + _lockedDirection + p;
        //     } else if (_useTransform)
        //         p = _applyRotation ? transform.TransformPoint(p) : transform.position + p;
        //     return p;
        // }

        public Vector3 GetPoint(int i)
        {
            // 1) fetch your stored control‐point in path‑local coords
            Vector3 localP = points[i];

            // 2) map into the chosen plane
            if (_space == PathSpace.XZ)
                localP = new Vector3(localP.x, 0f, localP.y);
            else if (_space == PathSpace.XY)
                localP = new Vector3(localP.x, localP.y, 0f);

            // 3) pick the rotation to apply:
            //    - if _directionSet: rotate Z+ to _lockedDirection
            //    - else if using transform: use its rotation (or none)
            Quaternion rot;
            if (_directionSet)
            {
                // ensure non‑zero
                Vector3 dir = _lockedDirection.sqrMagnitude > 0f
                    ? _lockedDirection.normalized
                    : Vector3.forward;
                rot = Quaternion.LookRotation(dir, Vector3.up);
            }
            else if (_useTransform)
            {
                rot = _applyRotation
                    ? transform.rotation
                    : Quaternion.identity;
            }
            else
            {
                rot = Quaternion.identity;
            }

            // 4) apply rotation & translate into world space
            return rot * localP + transform.position;
        }

        // Basic add (for linear)
        public void AddPoint(Vector3 worldPos)
        {
            Vector3 local = WorldToLocal(worldPos);
            points.Add(local);
            EnforceMode(points.Count - 1);
        }

        // Adds a full bezier segment: two control points + new anchor
        public void AddSegment(Vector3 worldPos)
        {
            Vector3 local = WorldToLocal(worldPos);
            if (points.Count == 0)
            {
                points.Add(local);
                return;
            }
            Vector3 lastAnchor = points[points.Count - 1];
            Vector3 dir = local - lastAnchor;
            Vector3 cp1 = lastAnchor + dir * 0.25f;
            Vector3 cp2 = lastAnchor + dir * 0.75f;
            points.Add(cp1);
            points.Add(cp2);
            points.Add(local);
            EnforceMode(points.Count - 2);
            EnforceMode(points.Count - 1);
        }

        Vector3 WorldToLocal(Vector3 worldPos)
        {
            Vector3 local = _useTransform
                ? (_applyRotation ? transform.InverseTransformPoint(worldPos) : worldPos - transform.position)
                : worldPos;
            if (_space == PathSpace.XZ) local = new Vector3(local.x, local.z, 0);
            else if (_space == PathSpace.XY) local = new Vector3(local.x, local.y, 0);
            return local;
        }

        public void ClearPoints()
        {
            points.Clear();
        }

        public void EnforceMode(int index)
        {
            if (_controlMode != ControlMode.Aligned) return;
            int anchorIndex = index;
            int modeAnchor = anchorIndex / 3 * 3;
            int fixedIndex, enforcedIndex;
            if (anchorIndex <= modeAnchor)
            {
                fixedIndex = modeAnchor - (anchorIndex - modeAnchor);
                enforcedIndex = anchorIndex;
            }
            else
            {
                enforcedIndex = modeAnchor - (anchorIndex - modeAnchor);
                fixedIndex = anchorIndex;
            }
            if (fixedIndex < 0 || enforcedIndex < 0 || fixedIndex >= points.Count || enforcedIndex >= points.Count) return;
            Vector3 anchor = points[modeAnchor];
            Vector3 dir = (anchor - points[fixedIndex]).normalized;
            float dist = (points[enforcedIndex] - anchor).magnitude;
            points[enforcedIndex] = anchor + dir * dist;
        }

        // public void EnforceMode(int index)
        // {
        //     if (controlMode != ControlMode.Aligned) return;
        //     // only enforce on control handles, skip real anchor points
        //     if (index % 3 == 0) return;

        //     // find the anchor this handle belongs to
        //     int anchorIndex = (index / 3) * 3;
        //     Vector3 anchor = points[anchorIndex];

        //     // direction & distance from anchor to the moved handle
        //     Vector3 dir = (points[index] - anchor).normalized;
        //     float dist = Vector3.Distance(points[index], anchor);

        //     // sibling handle is offset 1<->2 from the same anchor
        //     int offset = index - anchorIndex;                // either 1 or 2
        //     int siblingOffset = (offset == 1) ? 2 : 1;
        //     int siblingIndex = anchorIndex + siblingOffset;
        //     if (siblingIndex < 0 || siblingIndex >= points.Count) return;

        //     // mirror it
        //     points[siblingIndex] = anchor - dir * dist;
        // }

        void OnDrawGizmos()
        {
            if(!ShowGizmos) return;

            if (points == null || points.Count < 2) return;

            if(ShowHandles)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    Vector3 wp = GetPoint(i);
                    float size = UnityEditor.HandleUtility.GetHandleSize(wp) * 0.2f;
                    if (i % 3 == 0)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawSphere(wp, size);
                    }
                    else if (i % 3 == 1)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawCube(wp, Vector3.one * size);
                    }
                    else
                    {

                        Gizmos.color = Color.blue;
                        Gizmos.DrawCube(wp, Vector3.one * size);
                    }
                }
            }

            if (_drawMode == DrawMode.Linear)
            {
                for (int i = 0; i < points.Count - 1; i++)
                    Gizmos.DrawLine(GetPoint(i), GetPoint(i + 1));
                if (_closed) Gizmos.DrawLine(GetPoint(points.Count - 1), GetPoint(0));
            }
            else
            {
                int segmentCount = (points.Count - 1) / 3;
                if (_closed) segmentCount++;
                for (int s = 0; s < segmentCount; s++)
                {
                    int i = (s * 3) % points.Count;
                    Vector3 p0 = GetPoint(i);
                    Vector3 p1 = GetPoint((i + 1) % points.Count);
                    Vector3 p2 = GetPoint((i + 2) % points.Count);
                    Vector3 p3 = GetPoint((i + 3) % points.Count);
                    Vector3 prev = p0;
                    for (int j = 1; j <= _drawResolution; j++)
                    {
                        float t = j / (float)_drawResolution;
                        Vector3 next = Bezier.GetPoint(p0, p1, p2, p3, t);
                        Gizmos.DrawLine(prev, next);
                        prev = next;
                    }
                }
            }
        }

        public bool IsClosed()
        {
            return _closed;
        }

        public float GetGizmoSize()
        {
            return _gizmoSize;
        }

        public DrawMode GetDrawMode()
        {
            return _drawMode;
        }

        public bool IsApplyRotation()
        {
            return _applyRotation;
        }
        public bool IsUseTransform()
        {
            return _useTransform;
        }

        public PathSpace GetPathSpace()
        {
            return _space;
        }
    }
    // public static class Bezier
    // {
    //     public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    //     {
    //         float u = 1 - t;
    //         return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
    //     }

    //     internal static Vector3 GetFirstDerivative(Vector3 vector31, Vector3 vector32, Vector3 vector33, Vector3 vector34, float u)
    //     {
    //         throw new NotImplementedException();
    //     }
    // }

    public static class Bezier
    {
        public static Vector3 GetPoint(
            Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            return u * u * u * p0
                 + 3f * u * u * t * p1
                 + 3f * u * t * t * p2
                 + t * t * t * p3;
        }

        public static Vector3 GetFirstDerivative(
            Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            return 3f * u * u * (p1 - p0)
                 + 6f * u * t * (p2 - p1)
                 + 3f * t * t * (p3 - p2);
        }
    }
}