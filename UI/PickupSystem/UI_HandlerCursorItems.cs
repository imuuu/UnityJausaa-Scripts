using System;
using Nova;
using Sirenix.OdinInspector;
using UI;
using UnityEngine;

namespace Game.UI
{
    [DefaultExecutionOrder(-1)]
    public class UI_HandlerCursorItems : MonoBehaviour
    {
        // This is considering if we cant this to be singleton
        public static UI_HandlerCursorItems Instance { get; private set; }
        private ManagerMouseInput _managerMouseInput => ManagerMouseInput.Instance;

        [Title("Debug")]
        [SerializeField] private bool _debug = false;
        [SerializeField, ReadOnly] private UIBlock _cursorItem;
        [ShowInInspector, ReadOnly] private Coroutine _coroutineUpdatePickItemPosition;

        private const float TIME_BEFORE_DROP = 0.1f;
        // some reason when Instantiating the item, it some how spawns wrong place and flickers,
        // so we gonna spawn that item far away from the cursor and after this time, we gonna show it
        private Vector3 _farAwayPosition = new Vector3(0,0,-10000); 
        private DateTime _pickupTimestamp;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);

        }

        public bool TryToPickupItem(ICursorPickupable item)
        {
            if (_cursorItem != null)
                return false;

            if (!item.IsEnable())
                return false;

            UIBlock uiBlock = item.GetItem();

            if (uiBlock.TryGetComponent<IHasEmptyUI>(out IHasEmptyUI hasEmptyUI)
            && hasEmptyUI.IsEmpty())
            {
                return false;
            }

            this.transform.localPosition = _farAwayPosition;
            if (item.IsCloneOnPickup())
            {
                UIBlock clone = Instantiate(uiBlock, transform);
                _cursorItem = clone;
            }
            else
            {
                // Detach from grid
                // _cursorItem == detachedItem if detached!
                bool detached = false;
                bool isCloned = false;
                if (uiBlock.TryGetComponent<IDetachable>(out IDetachable detachable)
                && detachable.IsDetachable()
                && GetGridView(uiBlock.gameObject, out GridView gridView))
                {
                    if (hasEmptyUI != null && hasEmptyUI.IsPossibleToEmpty())
                    {
                        detached = true;
                        isCloned = true;

                        UIBlock clone = Instantiate(uiBlock, transform);
                        _cursorItem = clone;

                        hasEmptyUI.OnEmpty();
                    }
                    else if (DetachFromGrid(gridView, uiBlock.GetComponent<ItemView>()))
                    {
                        detached = true;
                    }
                }

                if (detached)
                {
                    detachable.OnDetach();
                    if (_debug) Debug.Log($"Detached from grid. Is Item cloned: {isCloned}");
                }
                else
                {
                    Debug.LogWarning($"Item named {uiBlock.gameObject.name} is not detached from grid, might cause issues!");
                    _cursorItem = uiBlock;
                }
            }

            _cursorItem.transform.position += new Vector3(0,0,-10000);

            if (item.AutoDisableInteractable()
            && _cursorItem.gameObject.TryGetComponent(out Nova.Interactable interactable))
            {
                interactable.enabled = false;
            }

            IPickupItemUI[] pickupItemUIs = _cursorItem.GetComponents<IPickupItemUI>();
            foreach (IPickupItemUI pickupItemUI in pickupItemUIs)
            {
                pickupItemUI.OnPickup(item);
            }

            _cursorItem.GetComponent<ICursorPickupable>().OnPickup();

            ActionScheduler.RunNextFrame(() =>
            {
                this.transform.localPosition = Vector3.zero;
            });

            _pickupTimestamp = DateTime.UtcNow;
            return true;
        }

        private bool DetachFromGrid(GridView gridView, ItemView itemView)
        {
            if (gridView.TryGetSourceIndex(itemView, out int index))
            {
                if (gridView.TryDetach(index, out ItemView detachedItem, this.transform))
                {
                    _cursorItem = detachedItem.GetComponent<UIBlock>();
                }

                return true;
            }

            return false;
        }

        public bool TryToDropItem(ICursorDropReceiver receiver)
        {
            if (_cursorItem == null)
            {
                return false;
            }

            if ((DateTime.UtcNow - _pickupTimestamp).TotalSeconds < TIME_BEFORE_DROP)
            {
                return false;
            }

            ICursorPickupable pickupable = _cursorItem.GetComponent<ICursorPickupable>();
            if (!receiver.CanDropItem(pickupable))
            {
                return false;
            }

            if (receiver is MonoBehaviour
            && (receiver as MonoBehaviour).TryGetComponent(out IHasEmptyUI hasEmptyUI)
            && hasEmptyUI.IsEmpty())
            {
                hasEmptyUI.OnRestore();
            }

            if (pickupable.AutoDisableInteractable()
            && _cursorItem.gameObject.TryGetComponent(out Nova.Interactable interactable))
            {
                interactable.enabled = true;
            }

            receiver.OnDropItem(pickupable);
            return true;
        }

        private bool TryProjectRay(Ray ray, out Vector3 worldPos)
        {
            Plane plane = new Plane(transform.forward, transform.position);
            if (plane.Raycast(ray, out float distance))
            {
                worldPos = ray.GetPoint(distance);
                return true;
            }

            worldPos = default;
            return false;
        }

        private void FixedUpdate() 
        {
            if(_cursorItem == null) return;

            this.transform.localPosition = Vector3.zero;
            if (_managerMouseInput.TryGetCurrentRay(out Ray ray)
               && TryProjectRay(ray, out Vector3 worldPos))
            {
                _cursorItem.transform.position = worldPos;
            }
        }

        private bool GetGridView(GameObject item, out GridView gridView)
        {
            Transform parent = item.transform.parent;
            if (parent == null)
            {
                gridView = null;
                return false;
            }

            gridView = parent.GetComponent<GridView>();
            return gridView != null;
        }
    }

}
