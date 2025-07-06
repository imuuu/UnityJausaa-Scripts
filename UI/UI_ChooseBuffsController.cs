using System;
using System.Collections.Generic;
using Game.BuffSystem;
using Game.StatSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.UI
{
    public class UI_ChooseBuffsController : MonoBehaviour
    {
        [SerializeField] private float _delayBetweenCards = 0.1f;
        [SerializeField] private float _delayBeforeActivate = 1f;
        [SerializeField] private List<UI_ChooseBuffCard> _chooseBuffCardVisuals;

        public void Awake()
        {
            Events.OnBuffCardsOpen.AddListener(OnBuffCardsOpen);
        }

        private bool OnBuffCardsOpen()
        {
            Activate();
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
        public void Activate()
        {
            Deactivate();
            ManagerBuffs.Instance.ClearRolledModifiers();
            ActionScheduler.RunAfterDelay(_delayBeforeActivate, () =>
            {
                ManagerBuffs.Instance.OnChooseCardsOpen();
                RecrusiveActivate(0, _delayBetweenCards <= 0 ? true : false);
            });
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

        // private void ActivateCard(int i)
        // {
        //     BuffDefinition buffDefinition = ManagerBuffs.Instance.GetRandomBuffOrSkill();
        //     _chooseBuffCardVisuals[i].ApplyBuffDefinition(buffDefinition);
        //     _chooseBuffCardVisuals[i].gameObject.SetActive(true);
        // }

        private void ActivateCard(int i)
        {
            BuffCard buffCard = ManagerBuffs.Instance.GetRandomBuffCard();
            _chooseBuffCardVisuals[i].ApplyBuffCard(buffCard);
            _chooseBuffCardVisuals[i].gameObject.SetActive(true);
        }


    }
}