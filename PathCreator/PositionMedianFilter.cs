using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Keeps a rolling window of the last N positions and computes
    /// the median path midpoint to infer movement direction.
    /// </summary>
    public class PositionMedianFilter
    {
        private readonly int _capacity;
        private readonly Queue<Vector3> _positions = new Queue<Vector3>();

        public PositionMedianFilter(int capacity)
        {
            if (capacity < 2)
                throw new ArgumentException("Capacity must be at least 2", nameof(capacity));
            _capacity = capacity;
        }

        /// <summary>
        /// Add the latest local position to the buffer.
        /// </summary>
        public void AddPosition(Vector3 localPos)
        {
            if (_positions.Count == _capacity)
                _positions.Dequeue();
            _positions.Enqueue(localPos);
        }

        /// <summary>
        /// Returns a normalized direction vector based on
        /// the median midpoint of recorded positions.
        /// If not enough data, returns Vector3.zero.
        /// </summary>
        public Vector3 GetMedianDirection()
        {
            if (_positions.Count < 2)
                return Vector3.zero;

            Vector3 median = GetMedianPoint();
            Vector3 start = _positions.First();
            var dir = median - start;
            return dir.sqrMagnitude > Mathf.Epsilon ? dir.normalized : Vector3.zero;
        }

        /// <summary>
        /// Returns the median point of the buffered positions.
        /// </summary>
        public Vector3 GetMedianPoint()
        {
            var list = _positions.ToList();

            float Median(IEnumerable<float> vals)
            {
                var sorted = vals.OrderBy(v => v).ToArray();
                int mid = sorted.Length / 2;
                return (sorted.Length % 2 == 0)
                    ? (sorted[mid - 1] + sorted[mid]) * 0.5f
                    : sorted[mid];
            }

            return new Vector3(
                Median(list.Select(p => p.x)),
                Median(list.Select(p => p.y)),
                Median(list.Select(p => p.z))
            );
        }

        /// <summary>
        /// Exposes the current buffered positions (local space).
        /// </summary>
        public IReadOnlyList<Vector3> Positions => _positions.ToList();
    }
}
