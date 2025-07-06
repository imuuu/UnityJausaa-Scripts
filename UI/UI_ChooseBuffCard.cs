using Game.BuffSystem;
using Nova;
using Sirenix.OdinInspector;
using UnityEngine;
public class UI_ChooseBuffCard : MonoBehaviour
{
    [SerializeField, BoxGroup("Index", showLabel: false)] private int _index;
    private ChooseBuffCardVisual _visual;

    //private BuffDefinition _buffDefinition;

    private BuffCard _buffCard;

    private void Awake()
    {
        _visual = GetComponent<ItemView>().Visuals as ChooseBuffCardVisual;
    }

    private void Start()
    {
        _visual.View.UIBlock.AddGestureHandler<Gesture.OnClick>(OnClick);
    }

    private void OnClick(Gesture.OnClick evt)
    {
        if (ManagerPause.IsPaused(PAUSE_REASON.PAUSE_MENU)) return;

        ManagerBuffs.Instance.OnPreSelectBuff(_index);
        _buffCard.OnSelectBuff(_index);
    }

    // public void ApplyBuffDefinition(BuffDefinition buffDefinition)
    // {
    //     _buffDefinition = buffDefinition;

    //     _visual.Bind(_index, buffDefinition);

    //     gameObject.SetActive(true);
    // }

    public void ApplyBuffCard(BuffCard buffCard)
    {
        _buffCard = buffCard;
        _visual.Bind(_index, buffCard);

        gameObject.SetActive(true);
    }
}