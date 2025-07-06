using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.PhysicInteractions
{
    public abstract class ForceInteraction : MonoBehaviour 
    {
        [BoxGroup("General")]
        [SerializeField]
        private LayerMask _layerMask;
        public void SetMask(LayerMask mask)
        {
            _layerMask = mask;
        }

        public LayerMask GetMask()
        {
            return _layerMask;
        }
    }
}