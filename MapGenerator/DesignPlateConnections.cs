using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.MapGenerator
{
    [Serializable]
    public class DesignPlateConnections
    {
        [SerializeField]
        private List<CONNECTION_TYPE> _topConnections = new ();    // e.g., { CLOSE, OPEN, CLOSE }
        [SerializeField]
        private List<CONNECTION_TYPE> _rightConnections = new ();  // e.g., { CLOSE, CLOSE, CLOSE }
        [SerializeField]
        private List<CONNECTION_TYPE> _bottomConnections = new ();
        [SerializeField]
        private List<CONNECTION_TYPE> _leftConnections = new ();

        /// <summary>
        /// Returns the list of connection types for the given side.
        /// </summary>
        public List<CONNECTION_TYPE> GetConnections(Side side)
        {
            switch (side)
            {
                case Side.Top: return _topConnections;
                case Side.Right: return _rightConnections;
                case Side.Bottom: return _bottomConnections;
                case Side.Left: return _leftConnections;
                default: return null;
            }
        }

        /// <summary>
        /// Checks if the connection list on the given side matches the connection list on the opposite side of the other plate.
        /// </summary>
        /// <param name="other">The neighbor plate’s connection data.</param>
        /// <param name="side">The side on THIS plate to check (its opposite will be checked on the neighbor).</param>
        /// <returns>True if the connection lists match, false otherwise.</returns>
        public bool IsCompatibleWith(DesignPlateConnections other, Side side)
        {
            List<CONNECTION_TYPE> myList = GetConnections(side);
            List<CONNECTION_TYPE> neighborList = other.GetConnections(OppositeSide(side));

            if (myList.Count != neighborList.Count)
            {
                //Debug.LogWarning($"[{side}] Count mismatch: {myList.Count} vs {neighborList.Count}");
                return false;
            }

            for (int i = 0; i < myList.Count; i++)
            {
                if (myList[i] != neighborList[i])
                {
                    //Debug.LogError($"Mismatch on {side} at index {i}: Candidate = {myList[i]}, Neighbor = {neighborList[i]}");
                    return false;
                }
            }
            return true;
        }


        /// <summary>
        /// Returns the opposite side.
        /// </summary>
        private Side OppositeSide(Side side)
        {
            switch (side)
            {
                case Side.Top: return Side.Bottom;
                case Side.Right: return Side.Left;
                case Side.Bottom: return Side.Top;
                case Side.Left: return Side.Right;
                default: return side;
            }
        }

        /// <summary>
        /// Rotates all connection lists clockwise by 90° for each rotation step.
        /// </summary>
        /// <param name="rotations">Number of 90° clockwise rotations to apply (0-3).</param>
        public void RotateConnectionsClockwise(int rotations)
        {
            rotations = rotations % 4;
            for (int i = 0; i < rotations; i++)
            {
                RotateOnce();
            }
        }

        /// <summary>
        /// Performs a single 90° clockwise rotation of the connection lists.
        /// </summary>
        private void RotateOnce()
        {
            // When rotating, reassign the lists:
            // Top -> Right, Right -> Bottom, Bottom -> Left, Left -> Top.
            List<CONNECTION_TYPE> oldTop = new (_topConnections);
            List<CONNECTION_TYPE> oldRight = new (_rightConnections);
            List<CONNECTION_TYPE> oldBottom = new (_bottomConnections);
            List<CONNECTION_TYPE> oldLeft = new (_leftConnections);

            _topConnections = oldLeft;
            _rightConnections = oldTop;
            _bottomConnections = oldRight;
            _leftConnections = oldBottom;
        }
    }

    public enum Side { Top, Right, Bottom, Left }
}
