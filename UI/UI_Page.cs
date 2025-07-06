using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.UI
{
    public class UI_page : MonoBehaviour, IUserInterfacePage
    {
        [Title("Page Type")]
        [SerializeField] private PAGE_TYPE _pageType;
        [Title("Settings")][Tooltip("If true, the page will be closed on start(next frame due to NOVA)")]
        [SerializeField] private bool _closeOnStart = false;
        [SerializeField] private bool _closeOnEscapeButton = false;

        [ToggleLeft][SerializeField] private bool _enablePauseOption = false;
        [ShowIf("_enablePauseOption")]
        [SerializeField] private PAUSE_REASON _pauseReason = PAUSE_REASON.NONE;

        [Tooltip("If true, the page will close when the scene changes. This is useful for pages that are not persistent across scenes.")]
        [ToggleLeft][SerializeField] private bool _enableCloseOnSceneChange = false;

        [ToggleLeft][SerializeField] private bool _enableEvents = false;
        [Title("Events")]
        [SerializeField, ShowIf("_enableEvents")] private UnityEvent _onPageOpened;
        [SerializeField, ShowIf("_enableEvents")] private UnityEvent _onPageClosed;

        private void Start()
        {
            RegisterToManager();
            ActionScheduler.RunNextFrame(() => SetActive(!_closeOnStart));

            Events.OnPlayableSceneChange.AddListener(OnPlayableSceneChange);
        }

        private void OnDestroy()
        {
            Events.OnPlayableSceneChange.RemoveListener(OnPlayableSceneChange);
        }

        private void OnEnable()
        {
            if(_closeOnEscapeButton) Events.OnUIPageClose.AddListener(OnEscapeButtonPress);
        }

        private void OnDisable()
        {
            if(_closeOnEscapeButton) Events.OnUIPageClose.RemoveListener(OnEscapeButtonPress);
        }

        private bool OnEscapeButtonPress(PAGE_TYPE pageName)
        {
            if (pageName != _pageType) return true;

            ManagerUI.Instance.ClosePage(_pageType);

            return true;
        }

        private bool OnPlayableSceneChange(SCENE_NAME sceneName)
        {
            if (_enableCloseOnSceneChange)
            {
                ManagerUI.Instance.ClosePage(_pageType);
            }

            return true;
        }

        public PAGE_TYPE GetPageType()
        {
            return _pageType;
        }

        public bool IsVisible()
        {
            return transform.gameObject.activeSelf;
        }

        public void RegisterToManager()
        {
            ManagerUI.Instance.RegisterPage(this);
        }

        public void UnregisterFromManager()
        {
            ManagerUI.Instance.UnregisterPage(this);
        }

        public void SetActive(bool active)
        {
            transform.gameObject.SetActive(active);

            if (_enablePauseOption)
            {
                if (_pauseReason == PAUSE_REASON.PAUSE_MENU)
                {
                    ManagerButtons.Instance.ActivatePause(active);
                }
                else
                {
                    if (active)
                        ManagerPause.AddPause(_pauseReason);
                    else
                        ManagerPause.RemovePause(_pauseReason);
                }
            }

            if (_enableEvents)
            {
                if (active)
                    _onPageOpened?.Invoke();
                else
                    _onPageClosed?.Invoke();
            }
        }
    }
}