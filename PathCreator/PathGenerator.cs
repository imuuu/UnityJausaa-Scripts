using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.PathSystem
{
    /// <summary>
    /// Generates a path through a set of points by:
    /// 1) Ordering by nearest neighbor
    /// 2) Subdividing to equal distances (arc-length sampling)
    /// 3) Providing sampling helpers and debug drawing
    /// </summary>
    public class PathGenerator
    {
        public IReadOnlyList<Vector3> MainPoints => _mainPoints;
        public int Resolution { get; set; }
        public bool CloseLoop { get; set; }

        private List<Vector3> _mainPoints;
        private List<Vector3> _ordered;
        private List<Vector3> _full;
        private static readonly System.Random _rng = new System.Random();

        public PathGenerator(IEnumerable<Vector3> mainPoints, int resolution, bool closeLoop)
        {
            _mainPoints = new List<Vector3>(mainPoints);
            if (_mainPoints.Count < 2)
                throw new ArgumentException("Need at least 2 main points", nameof(mainPoints));

            Resolution = Math.Max(resolution, _mainPoints.Count);
            CloseLoop = closeLoop;
        }

        /// <summary>
        /// Run ordering + equal-distance subdivision. Call before sampling or drawing.
        /// </summary>
        public void Generate()
        {
            _ordered = OrderByNearest(_mainPoints);
            _full = SubdivideArcLength(_ordered, Resolution, CloseLoop);
        }

        /// <summary>
        /// All generated points, equally spaced along the path.
        /// </summary>
        public List<Vector3> GetAllPoints()
        {
            if (_full == null) throw new InvalidOperationException("Generate() must be called first.");
            return new List<Vector3>(_full);
        }

        /// <summary>
        /// One random point from the generated list.
        /// </summary>
        public Vector3 GetRandomPoint()
        {
            var all = GetAllPoints();
            return all[_rng.Next(all.Count)];
        }

        /// <summary>
        /// Grab every Nth point, starting at index 0.
        /// </summary>
        public List<Vector3> GetEveryNth(int n)
        {
            var all = GetAllPoints();
            var outList = new List<Vector3>();
            n = Math.Max(1, n);
            for (int i = 0; i < all.Count; i += n)
                outList.Add(all[i]);
            return outList;
        }

        /// <summary>
        /// Draws the path with Debug.DrawLine in the Scene view.
        /// </summary>
        /// <param name="duration">Duration to keep each segment visible (0 = one frame).</param>
        public void DrawDebugLines(float duration = 0f)
        {
            var all = GetAllPoints();
            for (int i = 0; i < all.Count - 1; i++)
                Debug.DrawLine(all[i], all[i + 1], Color.white, duration);

            if (CloseLoop)
                Debug.DrawLine(all[all.Count - 1], all[0], Color.white, duration);
        }

        // ----------------------------------------------------------------
        // Nearest-neighbor ordering (greedy, O(n^2))
        // ----------------------------------------------------------------
        private static List<Vector3> OrderByNearest(IList<Vector3> points)
        {
            var rem = new List<Vector3>(points);
            var res = new List<Vector3>();
            Vector3 current = rem[0];
            res.Add(current);
            rem.RemoveAt(0);

            while (rem.Count > 0)
            {
                float bestDist = float.MaxValue;
                int bestIdx = 0;
                for (int i = 0; i < rem.Count; i++)
                {
                    float d = Vector3.SqrMagnitude(rem[i] - current);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestIdx = i;
                    }
                }
                current = rem[bestIdx];
                res.Add(current);
                rem.RemoveAt(bestIdx);
            }
            return res;
        }

        // ----------------------------------------------------------------
        // Arc-length based subdivision for equal spacing
        // ----------------------------------------------------------------
        private static List<Vector3> SubdivideArcLength(
            IReadOnlyList<Vector3> points,
            int resolution,
            bool loop)
        {
            int count = points.Count;
            int segCount = loop ? count : count - 1;
            var segments = new List<(Vector3 start, Vector3 end, float length)>(segCount);
            float totalLength = 0f;

            // Build segments
            for (int i = 0; i < segCount; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[(i + 1) % count];
                float len = Vector3.Distance(a, b);
                segments.Add((a, b, len));
                totalLength += len;
            }

            // Determine step distance
            int sampleCount = resolution;
            float step = loop
                ? totalLength / sampleCount
                : totalLength / (sampleCount - 1);

            var result = new List<Vector3>(sampleCount);
            if (!loop)
                result.Add(segments[0].start);

            int currentSeg = 0;
            //float distIntoSeg = 0f;
            float accumLength = 0f;

            // Sample points
            for (int i = 0; i < sampleCount; i++)
            {
                float target = step * i;
                if (!loop && i == sampleCount - 1)
                {
                    // ensure last point hits the very end
                    result.Add(segments[segCount - 1].end);
                    break;
                }

                // Advance through segments
                while (currentSeg < segments.Count &&
                       accumLength + segments[currentSeg].length < target)
                {
                    accumLength += segments[currentSeg].length;
                    currentSeg++;
                }
                if (currentSeg >= segments.Count)
                    currentSeg = segments.Count - 1;

                var (start, end, length) = segments[currentSeg];
                float localT = (target - accumLength) / length;
                result.Add(Vector3.Lerp(start, end, localT));
            }

            return result;
        }
    }
}
