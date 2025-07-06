using UnityEngine;
using Sirenix.OdinInspector;
namespace Game.Interactable
{
    [RequireComponent(typeof(Collider))]
    public class InteractableArea : SerializedMonoBehaviour, IInteractable
    {
        private bool _playerInRange;

        protected virtual void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        protected virtual void OnEnable()
        {
            Events.OnInteract.AddListener(HandleKeyDown);
        }

        protected virtual void OnDisable()
        {
            Events.OnInteract.RemoveListener(HandleKeyDown);
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OnEnter();
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OnExit();
            }
        }

        protected virtual void OnEnter()
        {
            _playerInRange = true;
        }

        protected virtual void OnExit()
        {
            _playerInRange = false;
        }

        private bool HandleKeyDown()
        {
            if (_playerInRange)
            {
#if UNITY_EDITOR
                Debug.Log($"<color=#1db8fb>[InteractableArea]</color> Interacting {gameObject.name}");
#endif
                return Interact();
            }

            return true;
        }

        /// <summary>
        ///  Override this in your child to define “what happens” when the key is pressed.
        /// </summary>
        public virtual bool Interact()
        {
            return false;
        }

        // Optional: draw gizmo so you can see your trigger in-editor
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Collider col = GetComponent<Collider>();
            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider b) Gizmos.DrawWireCube(b.center, b.size);
            if (col is SphereCollider s) Gizmos.DrawWireSphere(s.center, s.radius);
        }
    }
}