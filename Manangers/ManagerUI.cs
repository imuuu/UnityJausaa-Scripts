using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.UI
{
    [DefaultExecutionOrder(-1)]
    public class ManagerUI : MonoBehaviour
    {
        public static ManagerUI Instance { get; private set; }

        [Title("Registered Pages")]
        [ReadOnly, ShowInInspector]
        private Dictionary<PAGE_TYPE, IUserInterfacePage> _pages = new ();

        private void Awake() 
        {
            if(Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnEnable() 
        {
            Events.OnUIPageButtonPress.AddListener(OnUIPageButtonPress);
        }

        private void OnDisable() 
        {
            Events.OnUIPageButtonPress.RemoveListener(OnUIPageButtonPress);
        }

        public void RegisterPage(IUserInterfacePage page)
        {
            if(_pages.ContainsKey(page.GetPageType()))
            {
                Debug.LogWarning($"Page {page.GetPageType()} is already registered");
                return;
            }

            _pages.Add(page.GetPageType(), page);
        }

        public void UnregisterPage(IUserInterfacePage page)
        {
            if(!_pages.ContainsKey(page.GetPageType()))
            {
                Debug.LogWarning($"Page {page.GetPageType()} is not registered");
                return;
            }

            _pages.Remove(page.GetPageType());
        }

        private bool OnUIPageButtonPress(PAGE_TYPE pageType)
        {
            return TogglePage(pageType);
        }

        public bool OpenPage(PAGE_TYPE pageType)
        {
            if(_pages.ContainsKey(pageType))
            {
                _pages[pageType].SetActive(true);
                Events.OnUIPageOpen.Invoke(pageType);
                return true;
            }

            Debug.LogWarning($"Page {pageType} is not registered");
            return false;
        }

        public bool ClosePage(PAGE_TYPE pageType)
        {
            if(_pages.ContainsKey(pageType))
            {
                _pages[pageType].SetActive(false);

                if (pageType == PAGE_TYPE.PAUSE_MENU)
                {
                    ManagerPause.RemovePause(PAUSE_REASON.PAUSE_MENU);
                }

                Events.OnUIPageClose.Invoke(pageType);
                return true;
            }

            Debug.LogWarning($"Page {pageType} is not registered");
            return false;
        }

        public bool TogglePage(PAGE_TYPE pageType)
        {
            if(_pages.ContainsKey(pageType))
            {
                _pages[pageType].SetActive(!_pages[pageType].IsVisible());

                if (_pages[pageType].IsVisible())
                    Events.OnUIPageOpen.Invoke(pageType);
                else
                    Events.OnUIPageClose.Invoke(pageType);

                return true; 
            }

            Debug.LogWarning($"Page {pageType} is not registered");
            return false;
        }

#region Odin
        [Title("Test Functions")]
        [Button("Open Page")]
        public void TestOpenPage(PAGE_TYPE pageType)
        {
            OpenPage(pageType);
        }

        [Button("Close Page")]
        public void TestClosePage(PAGE_TYPE pageType)
        {
            ClosePage(pageType);
        }
    }
#endregion
}

