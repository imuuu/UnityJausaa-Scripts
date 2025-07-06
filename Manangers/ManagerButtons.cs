using System.Collections.Generic;
using Game.UI;
using Sirenix.OdinInspector;
using UnityEngine;

//<summary>
// This class is responsible for managing the buttons in the game.
// Very simple implementation of a button manager.
//</summary>
public class ManagerButtons : MonoBehaviour
{
    [System.Serializable]
    public class KeyPlan
    {
        public KeyCode Key;
        public string Name;
    }
    public static ManagerButtons Instance { get; private set; }

    [Title("SPECIFIC KEYS")]
    [SerializeField] private KeyCode _pauseKey = KeyCode.Escape;
    [SerializeField] private KeyCode _esc = KeyCode.Escape;
    [SerializeField] private KeyCode _interactable = KeyCode.E;

    private Dictionary<KeyCode, KeyEvents> _allKeyEvents = new();

    [Title("Skill Keys")]
    [SerializeField] private List<KeyPlan> SkillPlans = new ();
    private Dictionary<KeyCode, string> _skillKeys = new()
    {
    };

    [Title("UI_Page Keys (cant add here => add code)")]
    [ShowInInspector,ReadOnly] // Order matters!
    private Dictionary<KeyCode, PAGE_TYPE> _uiPageKeys = new()
    {
        { KeyCode.P, PAGE_TYPE.SPELL_BOOK },
        //{ KeyCode.Escape, PAGE_TYPE.PAUSE_MENU}
    };

    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("There should only be one ManagerButtons in the scene.");
            Destroy(this);
        }

        InitializeSkillKeys();
        InitializeUIPageKeys();
    }
    
    public void ActivatePause(bool activate)
    {
        if(SceneLoader.GetCurrentScene() == SCENE_NAME.Lobby)
        {
            Debug.LogWarning("Cannot activate pause in Lobby scene.");
            return;
        }

        if (activate)
        {
            ManagerPause.AddPause(PAUSE_REASON.PAUSE_MENU);
        }
        else
        {
            ManagerPause.RemovePause(PAUSE_REASON.PAUSE_MENU);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(_pauseKey) && SceneLoader.GetCurrentScene() != SCENE_NAME.Lobby)
        {
            // Debug.Log("Pause key pressed current scene: " + SceneLoader.GetCurrentScene());
            // if (ManagerPause.IsPaused(PAUSE_REASON.PAUSE_MENU))
            // {
            //     ManagerPause.RemovePause(PAUSE_REASON.PAUSE_MENU);
            // }
            // else
            // {
            //     ManagerPause.AddPause(PAUSE_REASON.PAUSE_MENU);
            // }

            ActivatePause(!ManagerPause.IsPaused(PAUSE_REASON.PAUSE_MENU));
        }

        if (Input.GetKeyDown(_interactable))
        {
            Events.OnInteract.Invoke();
        }

        if (Input.GetKeyDown(_esc))
        {
            Events.OnEscButtonPress.Invoke();
        }

        foreach (KeyValuePair<KeyCode, KeyEvents> kvp in _allKeyEvents)
        {
            KeyCode key = kvp.Key;
            KeyEvents keyEvents = kvp.Value;

            if (Input.GetKeyDown(key))
            {
                keyEvents.onKeyDown.Invoke();
            }

            if (Input.GetKey(key))
            {
                keyEvents.onKeyHold.Invoke();
            }

            if (Input.GetKeyUp(key))
            {
                keyEvents.onKeyUp.Invoke();
            }
        }
    }

    private void InitializeSkillKeys()
    {
        int index = 0;
        foreach (KeyPlan plan in SkillPlans)
        {
            _skillKeys[plan.Key] = plan.Name;
            RegisterKey(plan.Key);
            int slot = index;
            AddKeyDownListener(plan.Key, () =>
            {
                return Events.OnSkillButtonDown.Invoke(slot);
            });

            AddKeyUpListener(plan.Key, () =>
            {
                
                return Events.OnSkillButtonUp.Invoke(slot);
            });

            AddKeyHoldListener(plan.Key, () =>
            {
                return Events.OnSkillButtonHold.Invoke(slot);
            });

            index++;
        }
    }

    private void InitializeUIPageKeys()
    {
        foreach (KeyValuePair<KeyCode, PAGE_TYPE> pair in _uiPageKeys)
        {
            RegisterKey(pair.Key);
            AddKeyDownListener(pair.Key, () =>
            {
                return Events.OnUIPageButtonPress.Invoke(pair.Value);
            });
        }
    }

    public void RegisterKey(KeyCode key)
    {
        if (!_allKeyEvents.ContainsKey(key))
        {
            _allKeyEvents[key] = new KeyEvents();
        }
    }

    public void AddKeyDownListener(KeyCode key, EventPointHandler handler)
    {
        RegisterKey(key);
        _allKeyEvents[key].onKeyDown.AddListener(handler);
    }

    public void AddKeyHoldListener(KeyCode key, EventPointHandler handler)
    {
        RegisterKey(key);
        _allKeyEvents[key].onKeyHold.AddListener(handler);
    }

    public void AddKeyUpListener(KeyCode key, EventPointHandler handler)
    {
        RegisterKey(key);
        _allKeyEvents[key].onKeyUp.AddListener(handler);
    }

    public string GetSkillKey(int index)
    {
        if (index < 0 || index >= _skillKeys.Count)
        {
            Debug.LogWarning($"Invalid index {index}.");
            return "-";
        }
        int i = 0;
        foreach (KeyValuePair<KeyCode, string> pair in _skillKeys)
        {
            if (i == index)
            {
                return pair.Value;
            }
            i++;
        }
        return "-";
    }


}