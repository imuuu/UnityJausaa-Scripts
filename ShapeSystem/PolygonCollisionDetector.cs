using UnityEngine;

namespace Mask.Game.Sectors
{
    /// <summary>
    /// Helper utility class to detect collisions between two polygons2D
    /// </summary>
    public static class PolygonCollisionDetector
    {
        //====================================================================================================
        // 2D Polygon Collision Detection with Vector3
        //====================================================================================================

        //<summary>
        // Check if two polygons intersect in XZ plane
        //</summary>
        public static bool PolygonsIntersect(Vector3[] polyA, Vector3[] polyB)
        {
            if (EdgesIntersect(polyA, polyB))
                return true;

            if (IsPointInPolygon(polyA[0], polyB) || IsPointInPolygon(polyB[0], polyA))
                return true;

            return false;
        }
        
        //<summary>
        // Checks if any edges between two polygons (in XZ plane) intersect
        //</summary>
        private static bool EdgesIntersect(Vector3[] polyA, Vector3[] polyB)
        {
            for (int i = 0; i < polyA.Length; i++)
            {
                float a1x = polyA[i].x;
                float a1z = polyA[i].z;
                float a2x = polyA[(i + 1) % polyA.Length].x;
                float a2z = polyA[(i + 1) % polyA.Length].z;

                for (int j = 0; j < polyB.Length; j++)
                {
                    float b1x = polyB[j].x;
                    float b1z = polyB[j].z;
                    float b2x = polyB[(j + 1) % polyB.Length].x;
                    float b2z = polyB[(j + 1) % polyB.Length].z;

                    if (SegmentsIntersect(a1x, a1z, a2x, a2z, b1x, b1z, b2x, b2z))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        //<summary>
        // Determines if a point (in XZ plane) is inside a polygon
        //</summary>
        public static bool IsPointInPolygon(Vector3 point, Vector3[] polygon)
        {
            bool inside = false;

            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if (((polygon[i].z > point.z) != (polygon[j].z > point.z)) &&
                    (point.x < (polygon[j].x - polygon[i].x) * (point.z - polygon[i].z) / (polygon[j].z - polygon[i].z + Mathf.Epsilon) + polygon[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        //<summary>
        // Checks if two line segments intersect in the XZ plane
        //</summary>
        private static bool SegmentsIntersect(float p1x, float p1z, float p2x, float p2z,
                                              float q1x, float q1z, float q2x, float q2z)
        {
            int o1 = Orientation(p1x, p1z, p2x, p2z, q1x, q1z);
            int o2 = Orientation(p1x, p1z, p2x, p2z, q2x, q2z);
            int o3 = Orientation(q1x, q1z, q2x, q2z, p1x, p1z);
            int o4 = Orientation(q1x, q1z, q2x, q2z, p2x, p2z);

            if (o1 != o2 && o3 != o4)
                return true;

            if (o1 == 0 && OnSegment(p1x, p1z, q1x, q1z, p2x, p2z)) return true;
            if (o2 == 0 && OnSegment(p1x, p1z, q2x, q2z, p2x, p2z)) return true;
            if (o3 == 0 && OnSegment(q1x, q1z, p1x, p1z, q2x, q2z)) return true;
            if (o4 == 0 && OnSegment(q1x, q1z, p2x, p2z, q2x, q2z)) return true;

            return false;
        }

        //<summary>
        // Determines the orientation of three points in the XZ plane
        //</summary>
        private static int Orientation(float px, float pz, float qx, float qz, float rx, float rz)
        {
            float val = (qz - pz) * (rx - qx) - (qx - px) * (rz - qz);

            if (Mathf.Abs(val) < Mathf.Epsilon)
                return 0; 

            return (val > 0) ? 1 : 2; // Clockwise or Counterclockwise
        }

        //<summary>
        // Checks if point (qx, qz) lies on segment (px, pz)-(rx, rz)
        //</summary>
        private static bool OnSegment(float px, float pz, float qx, float qz, float rx, float rz)
        {
            if (qx <= Mathf.Max(px, rx) && qx >= Mathf.Min(px, rx) &&
                qz <= Mathf.Max(pz, rz) && qz >= Mathf.Min(pz, rz))
                return true;
            return false;
        }
    
        //====================================================================================================
        // 2D Polygon Collision Detection with Vector2
        //====================================================================================================

        public static bool PolygonsIntersect(Vector2[] polyA, Vector2[] polyB)
        {
            if (EdgesIntersect(polyA, polyB))
                return true;

            if (IsPointInPolygon(polyA[0], polyB) || IsPointInPolygon(polyB[0], polyA))
                return true;

            return false;
        }

        private static bool EdgesIntersect(Vector2[] polyA, Vector2[] polyB)
        {
            for (int i = 0; i < polyA.Length; i++)
            {
                Vector2 a1 = polyA[i];
                Vector2 a2 = polyA[(i + 1) % polyA.Length];

                for (int j = 0; j < polyB.Length; j++)
                {
                    Vector2 b1 = polyB[j];
                    Vector2 b2 = polyB[(j + 1) % polyB.Length];

                    if (SegmentsIntersect(a1, a2, b1, b2))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
        {
            int n = polygon.Length;
            bool inside = false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Vector2 pi = polygon[i];
                Vector2 pj = polygon[j];

                if (((pi.y > point.y) != (pj.y > point.y)) &&
                    (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y + Mathf.Epsilon) + pi.x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            int o1 = Orientation(p1, p2, q1);
            int o2 = Orientation(p1, p2, q2);
            int o3 = Orientation(q1, q2, p1);
            int o4 = Orientation(q1, q2, p2);

            if (o1 != o2 && o3 != o4)
                return true;

            if (o1 == 0 && OnSegment(p1, q1, p2)) return true;
            if (o2 == 0 && OnSegment(p1, q2, p2)) return true;
            if (o3 == 0 && OnSegment(q1, p1, q2)) return true;
            if (o4 == 0 && OnSegment(q1, p2, q2)) return true;

            return false;
        }

        private static int Orientation(Vector2 p, Vector2 q, Vector2 r)
        {
            float val = (q.y - p.y) * (r.x - q.x) -
                        (q.x - p.x) * (r.y - q.y);

            if (Mathf.Abs(val) < Mathf.Epsilon)
                return 0;  // Colinear

            return (val > 0) ? 1 : 2;
        }

        private static bool OnSegment(Vector2 p, Vector2 q, Vector2 r)
        {
            if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
                q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y))
                return true;
            return false;
        }
    }
}

