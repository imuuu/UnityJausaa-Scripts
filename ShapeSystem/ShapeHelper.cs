using UnityEngine;

namespace Game.Shapes
{
    /// <summary>
    /// Provides utility methods for various shape-related operations.
    /// </summary>
    public static class ShapeHelper
    {
        // ----------------------------------------------------------------------
        // Generic method that just calls shape.GetClosestPoint(...)
        // ----------------------------------------------------------------------
        public static Vector3 GetClosestPointOnShape(IShape shape, Vector3 point)
        {
            return shape.GetClosestPoint(point);
        }

        // ----------------------------------------------------------------------
        // Polygons
        // ----------------------------------------------------------------------
        /// <summary>
        /// Returns the closest point on the perimeter (edges) of a polygon to a given point.
        /// Assumes the polygon is defined by an ordered array of vertices in 3D,
        /// though typically you'll treat them as 2D in XZ or XY plane.
        /// </summary>
        public static Vector3 GetClosestPointOnPolygon(Vector3[] polygonVertices, Vector3 point)
        {
            if (polygonVertices == null || polygonVertices.Length < 2)
            {
                Debug.LogError("Invalid polygon vertices. Must contain at least 2 vertices.");
                return point;
            }

            Vector3 closestPoint = Vector3.zero;
            float minDistanceSqr = float.MaxValue;

            int vertexCount = polygonVertices.Length;
            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 start = polygonVertices[i];
                Vector3 end = polygonVertices[(i + 1) % vertexCount]; // wrap around to form closed loop
                Vector3 candidate = GetClosestPointOnLineSegment(start, end, point);

                float sqrDist = (candidate - point).sqrMagnitude;
                if (sqrDist < minDistanceSqr)
                {
                    minDistanceSqr = sqrDist;
                    closestPoint = candidate;
                }
            }

            return closestPoint;
        }

        /// <summary>
        /// Finds the closest point on a line segment [segmentStart -> segmentEnd] to an arbitrary point.
        /// </summary>
        private static Vector3 GetClosestPointOnLineSegment(Vector3 segmentStart, Vector3 segmentEnd, Vector3 point)
        {
            Vector3 segment = segmentEnd - segmentStart;
            Vector3 toPoint = point - segmentStart;

            float segmentLengthSqr = segment.sqrMagnitude;
            if (segmentLengthSqr < Mathf.Epsilon)
            {
                // segmentStart and segmentEnd are effectively the same point
                return segmentStart;
            }

            // Project 'toPoint' onto 'segment' to find the parameter t along the segment
            float dot = Vector3.Dot(toPoint, segment);
            float t = dot / segmentLengthSqr;

            // Ensure t is within [0, 1] so it stays on the segment
            t = Mathf.Clamp01(t);

            return segmentStart + t * segment;
        }

        // ----------------------------------------------------------------------
        // Circle
        // ----------------------------------------------------------------------
        /// <summary>
        /// Returns the closest point on the perimeter of a circle to a given point.
        /// </summary>
        /// <param name="center">Center of the circle.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="point">The point from which we want the closest point on the circle.</param>
        /// <returns>Closest point on the circle’s perimeter.</returns>
        public static Vector3 GetClosestPointOnCircle(Vector3 center, float radius, Vector3 point)
        {
            Vector3 direction = point - center;

            // If the point is exactly at the center, handle it by returning any point on the circle
            if (direction.sqrMagnitude < Mathf.Epsilon)
            {
                return center + Vector3.right * radius; // e.g., pick "right" direction
            }

            // Get the point on the perimeter in the same direction, but at distance == radius
            return center + direction.normalized * radius;
        }

        // ----------------------------------------------------------------------
        // Squares - Interior vs Edges
        // ----------------------------------------------------------------------

        /// <summary>
        /// Returns the closest point on or within an axis-aligned square in the XZ plane, 
        /// effectively clamping the point inside the square boundary.
        /// </summary>
        public static Vector3 GetClosestPointOnSquareInterior(Vector3 center, float sideLength, Vector3 point)
        {
            // half-extent in each direction:
            float half = sideLength * 0.5f;

            // Translate 'point' into local space of the square
            Vector3 local = point - center;

            // Clamp in X and Z to remain within the square boundary
            float clampedX = Mathf.Clamp(local.x, -half, half);
            float clampedZ = Mathf.Clamp(local.z, -half, half);

            // Convert back to world space
            return center + new Vector3(clampedX, 0f, clampedZ);
        }

        /// <summary>
        /// Returns the closest point on the *perimeter* (edges/corners) of an axis-aligned square
        /// in the XZ plane, ignoring the interior.
        /// </summary>
        public static Vector3 GetClosestPointOnSquareEdges(Vector3 center, float sideLength, Vector3 point)
        {
            float half = sideLength * 0.5f;

            // Translate 'point' into the local space of the square
            Vector3 local = point - center;

            float ax = Mathf.Abs(local.x);
            float az = Mathf.Abs(local.z);

            // Compare which dimension is "dominant" in local space
            if (Mathf.Approximately(ax, az))
            {
                // The point is diagonally equidistant => corner
                local.x = Mathf.Sign(local.x) * half;
                local.z = Mathf.Sign(local.z) * half;
            }
            else if (ax > az)
            {
                // x dimension is farther out => clamp x to edge, keep z in [-half..half]
                local.x = Mathf.Sign(local.x) * half;
                local.z = Mathf.Clamp(local.z, -half, half);
            }
            else
            {
                // z dimension is farther out => clamp z to edge, keep x in [-half..half]
                local.z = Mathf.Sign(local.z) * half;
                local.x = Mathf.Clamp(local.x, -half, half);
            }

            // Convert back to world space



            return center + new Vector3(local.x, 0f, local.z);
        }


        /// <summary>
        /// Returns the closest point on or within an axis-aligned square that has
        /// a specified world-space center and rotation.  The square is sideLength wide in local space,
        /// centered at (0,0,0) with no rotation, then placed in world with [center + rotation].
        /// </summary>
        public static Vector3 GetClosestPointOnRotatedSquareInterior(
            Vector3 center,
            float sideLength,
            Quaternion rotation,
            Vector3 worldPoint)
        {
            // 1) Transform point into local space
            Vector3 localPoint = Quaternion.Inverse(rotation) * (worldPoint - center);

            // 2) Do the axis-aligned clamp in local space (square from -half..+half)
            float half = sideLength * 0.5f;
            float clampedX = Mathf.Clamp(localPoint.x, -half, half);
            float clampedZ = Mathf.Clamp(localPoint.z, -half, half);

            // 3) Transform back to world space
            Vector3 localClosest = new Vector3(clampedX, 0f, clampedZ);
            Vector3 worldClosest = center + rotation * localClosest;
            return worldClosest;
        }

        /// <summary>
        /// Returns the closest point on the *perimeter* (edges) of a rotated, axis-aligned square.
        /// </summary>
        public static Vector3 GetClosestPointOnRotatedSquareEdges(
     Vector3 center,
     float sideLength,
     Quaternion rotation,
     Vector3 worldPoint)
        {
            // 1) Transform the point into the shape’s local space
            Vector3 localPoint = Quaternion.Inverse(rotation) * (worldPoint - center);

            // 2) Since the shape has zero thickness in local Y (the XZ plane),
            //    we discard any Y offset. This forces the final point to lie on the plane.
            localPoint.y = 0f;

            float half = sideLength * 0.5f;
            float ax = Mathf.Abs(localPoint.x);
            float az = Mathf.Abs(localPoint.z);

            // 3) Clamp to edges or corners in XZ
            if (Mathf.Approximately(ax, az))
            {
                // Diagonal => corner
                localPoint.x = Mathf.Sign(localPoint.x) * half;
                localPoint.z = Mathf.Sign(localPoint.z) * half;
            }
            else if (ax > az)
            {
                // x is dominant => clamp x to ±half, z within [-half..half]
                localPoint.x = Mathf.Sign(localPoint.x) * half;
                localPoint.z = Mathf.Clamp(localPoint.z, -half, half);
            }
            else
            {
                // z is dominant => clamp z to ±half, x within [-half..half]
                localPoint.z = Mathf.Sign(localPoint.z) * half;
                localPoint.x = Mathf.Clamp(localPoint.x, -half, half);
            }

            // 4) Transform back to world space
            return center + (rotation * localPoint);
        }



        public static Vector3 GetClosestPointOnRotatedCircle(
    Vector3 center, float radius, Quaternion rotation, Vector3 worldPoint)
        {
            // 1) World → local
            Vector3 localPoint = Quaternion.Inverse(rotation) * (worldPoint - center);

            // 2) Snap to plane
            localPoint.y = 0f;

            // 3) If localPoint is exactly at origin, pick any direction on the circle
            float sqrMag = localPoint.sqrMagnitude;
            if (sqrMag < Mathf.Epsilon)
            {
                // e.g. local right
                localPoint = new Vector3(radius, 0f, 0f);
            }
            else
            {
                float mag = Mathf.Sqrt(sqrMag);
                localPoint = (localPoint / mag) * radius;
            }

            // 4) local → world
            return center + rotation * localPoint;
        }

        public static Vector3 GetClosestPointOnRotatedPolygon(
    Vector3 center,
    Quaternion rotation,
    Vector3[] localVerts,
    Vector3 worldPoint)
        {
            // 1) World → local
            Vector3 localPoint = Quaternion.Inverse(rotation) * (worldPoint - center);

            // 2) Snap to plane
            localPoint.y = 0f;

            // 3) Polygon edge check
            Vector3 localClosest = GetClosestPointOnPolygonEdges(localVerts, localPoint);

            // 4) local → world
            return center + rotation * localClosest;
        }

        public static Vector3 GetClosestPointOnRotatedTriangle(
     Vector3 center,
     Quaternion rotation,
     Vector3[] localTriangleVerts,
     Vector3 worldPoint)
        {
            // 1) World → local
            Vector3 localPoint = Quaternion.Inverse(rotation) * (worldPoint - center);

            // 2) Snap to plane
            localPoint.y = 0f;

            // 3) Now find closest point on the triangle’s edges in local space
            Vector3 localClosest = GetClosestPointOnPolygonEdges(localTriangleVerts, localPoint);

            // 4) Local → world
            return center + rotation * localClosest;
        }

        private static Vector3 GetClosestPointOnPolygonEdges(Vector3[] verts, Vector3 point)
        {
            if (verts == null || verts.Length < 2) return point;

            Vector3 best = Vector3.zero;
            float minDistSqr = float.MaxValue;

            for (int i = 0; i < verts.Length; i++)
            {
                int j = (i + 1) % verts.Length;
                Vector3 candidate = GetClosestPointOnLineSegment(verts[i], verts[j], point);
                float distSqr = (candidate - point).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    best = candidate;
                }
            }
            return best;
        }


    }

}
