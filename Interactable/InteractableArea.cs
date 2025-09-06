using UnityEngine;
using Sirenix.OdinInspector;
namespace Game.Interactable
{
    [RequireComponent(typeof(Collider))]
    public class InteractableArea : SerializedMonoBehaviour, IInteractable
    {
        public enum INTERACT_TYPE
        {
            BUTTON_PRESS,
            ON_ENTER,
        }

        [SerializeField,BoxGroup("Interaction"), PropertySpace(10,10)] private INTERACT_TYPE _interactType = INTERACT_TYPE.BUTTON_PRESS;
        [SerializeField,BoxGroup("Interaction"), PropertySpace(10,10)] bool  _interactOnce = false;

        [InfoBox("Can be null, if set it will show a UI plate when the player is in range")]
        [SerializeField, BoxGroup("UI Interactable Plate")] private GameObject _UI_InteractablePlate;
        [SerializeField, ToggleLeft, BoxGroup("UI Interactable Plate")] private bool _enableCustomLocationUIPlate = false;
        [ShowIf(nameof(_enableCustomLocationUIPlate))]
        [SerializeField, BoxGroup("UI Interactable Plate")] private Vector3 _UI_InteractablePlatePosition = Vector3.zero;
        private bool _playerInRange;

        private GameObject _interactablePlateInstance;
        private bool _isLocked = false;

        protected virtual void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
            _isLocked = false;
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
            ShowInteractablePlate();
            _playerInRange = true;

            if (_interactType == INTERACT_TYPE.ON_ENTER)
            {
                Interact();
            }
        }

        protected virtual void OnExit()
        {
            HideInteractablePlate();
            _playerInRange = false;
        }

        private bool HandleKeyDown()
        {
            if (_playerInRange && _interactType == INTERACT_TYPE.BUTTON_PRESS)
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
            if (_interactOnce && _isLocked) return false;

            if(_interactOnce) _isLocked = true;

            ToggleInteractablePlate();
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

        public GameObject GetInteractablePlate()
        {
            if (_interactablePlateInstance == null && _UI_InteractablePlate != null)
            {
                Vector3 platePosition = _enableCustomLocationUIPlate ? _UI_InteractablePlatePosition : transform.position;
                _interactablePlateInstance = Instantiate(_UI_InteractablePlate, platePosition, Quaternion.identity);
                _interactablePlateInstance.transform.SetParent(transform);
            }
            return _interactablePlateInstance;
        }

        public void ShowInteractablePlate()
        {
            GameObject plate = GetInteractablePlate();

            if (plate == null) return;

            plate.SetActive(true);
        }

        public void HideInteractablePlate()
        {
            if (_interactablePlateInstance != null)
            {
                _interactablePlateInstance.SetActive(false);
            }
        }

        public void ToggleInteractablePlate()
        {
            if (_interactablePlateInstance != null)
            {
                _interactablePlateInstance.SetActive(!_interactablePlateInstance.activeSelf);
            }
        }
    }
}