using System.Collections.Generic;
using UnityEngine;
using Nova;
using Sirenix.OdinInspector;
using Unity.Entities.UniversalDelegates;

/// <summary>
/// Spawns a dynamic number of UIBlock2D slots as children and allows
/// toggling the first N slots on or off, filling from left to right.
/// </summary>
public class UI_Multibar : MonoBehaviour
{
    [SerializeField] private ListView _listView;
    [SerializeField] private UIBlock2D _slotPrefab;
    [SerializeField] private List<UIBlock2D> _slots = new();

    private static List<UI_Multibar> _allSameTypeListViews = new ();

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (_listView != null && !_allSameTypeListViews.Contains(this))
        {
            _allSameTypeListViews.Add(this);
        }
    }

    private void OnDisable()
    {
        if (_allSameTypeListViews.Contains(this))
        {
            _allSameTypeListViews.Remove(this);
        }
    }

    private void Initialize()
    {
        _listView.AddDataBinder<UIBlock2D, MultiBarSlotVisual>(BindItem);
        _listView.SetDataSource(_slots);
        _listView.Refresh();
    }

    private void BindItem(Data.OnBind<UIBlock2D> evt, MultiBarSlotVisual target, int index)
    {

    }

    [Button("Set Toggles")]
    public void SetToggles(int amount, bool instant = false)
    {
        int i = 0;
        foreach (var child in _listView.gameObject.transform)
        {
            Transform childTransform = (Transform)child;
            UI_ToggleColor toggle = childTransform.GetComponent<UI_ToggleColor>();
            if (toggle == null)
            {
                Debug.LogWarning($"No UI_ToggleColor component found on item at index. Skipping.");
                continue;
            }

            if(instant)
                toggle.ToggleColorInsta(i++ < amount);
            else
                toggle.ToggleColor(i++ < amount);
        }
        // for (int i = 0; i < data.Count; i++)
        // {
        //     UI_ToggleColor toggle = data[i].GetComponent<UI_ToggleColor>();
        //     if (toggle == null)
        //     {
        //         Debug.LogWarning($"No UI_ToggleColor component found on item at index {i}. Skipping.");
        //         continue;
        //     }

        //     //toggle.ToggleColor(i < amount);
        //     toggle.ToggleColorInsta(i < amount);
        // }
    }

    /// <summary>
    /// Sets the total number of slots by adding or removing as needed.
    /// </summary>
    /// <param name="amount">Desired slot count.</param>
    public void SetSlotAmount(int amount, int rank)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Amount cannot be negative.");
            return;
        }

        int current = _slots.Count;
        if (amount > current)
        {
            for (int i = 0; i < amount - current; i++)
                AddSlot(i < rank);
        }
        else if (amount < current)
        {
            for (int i = 0; i < current - amount; i++)
                RemoveSlot();
        }
    }

    [Button("Add Slot")]
    public void AddSlot(bool enableColor = false)
    {
        _slots.Add(_slotPrefab.gameObject.GetComponent<UIBlock2D>());

        _listView.Refresh();

        GameObject newSlot = _slots[_slots.Count - 1].gameObject;
        UI_ToggleColor toggle = newSlot.GetComponent<UI_ToggleColor>();
        toggle.ToggleColorInsta(enableColor);

        //Debug.Log($"Added new slot. Total slots: {_slots.Count - 1} color enabled: {enableColor}");
        RefreshAllLayouts();
    }

    public void RefreshSlots()
    {
        _listView.Refresh();
    }

    [Button("Remove Slot")]
    public void RemoveSlot()
    {
        ItemView itemView;
        int index = _slots.Count - 1;
        bool detaiched = _listView.TryDetach(index, out itemView);

        if (!detaiched)
        {
            Debug.LogWarning($"Failed to detach item at index {index}. Trying to remove middle item");
            //trying to remove middle item
            index = _slots.Count / 2;

            if( index < 0 || index >= _slots.Count)
            {
                Debug.LogWarning("No slots to remove.");
                return;
            }

            detaiched = _listView.TryDetach(index, out itemView);

            if (!detaiched)
            {
                Debug.LogWarning($"Failed to detach item at index {index}. ItemView: {itemView}");
                return;
            }
        }

        _slots.RemoveAt(index);
       
        Destroy(itemView.gameObject);

        _listView.Refresh();
        RefreshAllLayouts();
    }

    // this is due to that Nova doesnt like percent based layouts it seems
    public void RefreshAllLayouts()
    {
        // foreach (var listView in _allSameTypeListViews)
        // {
        //     listView.RefreshLayout();
        // }
    }

    public void RefreshLayout()
    {
        _listView.UIBlock.AutoLayout.Axis = Axis.Z;
        _listView.UIBlock.AutoLayout.Offset = 0f;
        ActionScheduler.RunAfterDelay(0.1f, () =>
        {
            if(_listView == null || _listView.UIBlock == null)
            {
                return;
            }
            _listView.UIBlock.AutoLayout.Axis = Axis.X;
        });
    }
}
