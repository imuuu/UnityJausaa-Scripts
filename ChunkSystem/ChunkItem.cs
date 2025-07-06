using UnityEngine;
namespace Game.ChunkSystem
{
    [System.Serializable]
    public class ChunkItem : IWeightedLoot
    {
        public GameObject GameObject;
        public int Weight;

        #region Comparison
        public override bool Equals(object obj)
        {
            return Equals(obj as ChunkItem);
        }

        public bool Equals(ChunkItem other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return GameObject == other.GameObject;
        }

        public override int GetHashCode()
        {
            // Use 0 or some constant when GameObject is null
            return GameObject != null ? GameObject.GetHashCode() : 0;
        }

        public float GetWeight()
        {
            return Weight;
        }

        public static bool operator ==(ChunkItem left, ChunkItem right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(ChunkItem left, ChunkItem right)
        {
            return !(left == right);
        }
        #endregion
    }
}