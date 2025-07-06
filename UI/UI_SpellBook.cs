using System.Collections.Generic;
using Game.UI;
using Nova;
using Sirenix.OdinInspector;
using UnityEngine;
using Game.SkillSystem;

public class UI_SpellBook : MonoBehaviour
{
    [SerializeField] private GridView _gridView;

    private List<SkillDefinition> _skills = null;

    [Title("Row Styling")]
    [SerializeField] private float _padding = 10;
    [SerializeField] private RadialGradient _rowGradient;

    private void Start()
    {
        Debug.Log("UI_SpellBook Start");
        //ActionScheduler.RunAfterDelay(0.1f, Init);

        Init();
    }

    private void Init()
    {
        _skills = ManagerSkills.Instance.GetAllSkills();
        _gridView.AddDataBinder<SkillDefinition, SkillSlotVisual>(BindItem);

        _gridView.SetSliceProvider(ProvideSlice);

        // _gridView.AddGestureHandler<Gesture.OnHover, SkillSlotVisual>(HandleHover);
        // _gridView.AddGestureHandler<Gesture.OnUnhover, SkillSlotVisual>(HandleUnhover);
        // _gridView.AddGestureHandler<Gesture.OnPress, SkillSlotVisual>(HandlePress);
        // _gridView.AddGestureHandler<Gesture.OnRelease, SkillSlotVisual>(HandleRelease);

        _gridView.SetDataSource(_skills);

    }

    private void ProvideSlice(int sliceIndex, GridView gridView, ref GridSlice2D gridSlice)
    {
        gridSlice.Layout.AutoSize.Y = AutoSize.Shrink;
        gridSlice.AutoLayout.AutoSpace = true;
        gridSlice.Layout.Padding.Value = _padding;
        gridSlice.Gradient = _rowGradient;
    }

    private void BindItem(Data.OnBind<SkillDefinition> evt, SkillSlotVisual target, int index)
    {
        UI_SkillSlot skillSlot = target.View.gameObject.GetComponent<UI_SkillSlot>();
        skillSlot.SetSkillDefinition(evt.UserData);
        target.Bind(evt.UserData);
    }

#region GestureHandle
    private void HandleRelease(Gesture.OnRelease evt, SkillSlotVisual target, int index)
    {
        target.Release();
    }

    private void HandlePress(Gesture.OnPress evt, SkillSlotVisual target, int index)
    {
        target.Press();
    }

    private void HandleUnhover(Gesture.OnUnhover evt, SkillSlotVisual target, int index)
    {
        target.Unhover();
    }

    private void HandleHover(Gesture.OnHover evt, SkillSlotVisual target, int index)
    {
        target.Hover();
    }
#endregion
}
