using System.Collections.Generic;
using Game.BuffSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.UI
{
    public class UI_ChooseBuffsController : MonoBehaviour
    {
        [SerializeField] private List<BUFF_OPEN_TYPE> _allowedOpenTypes = new();
        [SerializeField] private float _delayBetweenCards = 0.1f;
        [SerializeField] private float _delayBeforeActivate = 1f;
        [SerializeField] private List<UI_ChooseBuffCard> _chooseBuffCardVisuals;

        private List<BuffCard> _buffCards;

        public void Awake()
        {
            Events.OnBuffCardsOpen.AddListener(OnBuffCardsOpen);
        }

        private bool OnBuffCardsOpen(BUFF_OPEN_TYPE openType)
        {
            if (!_allowedOpenTypes.Contains(openType)) return true;

            Activate(openType);
            return true;
        }

        private void Start()
        {
            foreach (UI_ChooseBuffCard chooseBuffCardVisual in _chooseBuffCardVisuals)
            {
                chooseBuffCardVisual.gameObject.SetActive(false);
            }
        }

        [Button("Activate")]
        public void Activate(BUFF_OPEN_TYPE openType)
        {
            Deactivate();

            _buffCards = ManagerBuffs.Instance.GetBuffCards(_chooseBuffCardVisuals.Count, openType);

            ActionScheduler.CancelActions(gameObject.GetInstanceID());

            ActionScheduler.RunAfterDelay(_delayBeforeActivate, () =>
            {
                ManagerBuffs.Instance.OnChooseCardsOpen();
                RecrusiveActivate(0, _delayBetweenCards <= 0 ? true : false);
            }, gameObject.GetInstanceID());
        }

        [Button("Deactivate")]
        public void Deactivate()
        {
            foreach (UI_ChooseBuffCard chooseBuffCardVisual in _chooseBuffCardVisuals)
            {
                chooseBuffCardVisual.gameObject.SetActive(false);
            }
        }

        private void RecrusiveActivate(int i, bool activateAll = false)
        {
            if (i < _chooseBuffCardVisuals.Count)
            {
                ActivateCard(i);

                if (activateAll)
                {
                    RecrusiveActivate(i + 1, true);
                }
                else
                {
                    ActionScheduler.RunAfterDelay(_delayBetweenCards, () =>
                    {
                        RecrusiveActivate(i + 1);
                    });
                }
            }
        }

        private void ActivateCard(int i)
        {
            _chooseBuffCardVisuals[i].ApplyBuffCard(_buffCards[i]);
            _chooseBuffCardVisuals[i].gameObject.SetActive(true);
        }


    }
}