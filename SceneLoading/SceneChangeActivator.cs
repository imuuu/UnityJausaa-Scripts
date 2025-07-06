using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

public class SceneChangeActivator : MonoBehaviour
{
    [SerializeField] private SCENE_NAME[] _scenes;
    [SerializeField] private ActivationSettings _activationSettings;

    [SerializeField] private bool _enableDelayActivation = false;
    [SerializeField][ShowIf("_enableDelayActivation")] private float _delayActivationTime = 0.1f;


    private enum ActivationSettings
    {
        ACTIVE_ONLY_THESE_SCENES,
        ACTIVE_ONLY_OTHER_SCENES,
    }

    private void Awake()
    {
        Events.OnPlayableSceneChange.AddListener(OnPlayableSceneChange);
    }

    private void OnDestroy()
    {
        Events.OnPlayableSceneChange.RemoveListener(OnPlayableSceneChange);
        if (ActionScheduler.Instance != null)
        {
            ActionScheduler.CancelActions(this.GetInstanceID());
        }
    }

    private void Start()
    {
        OnPlayableSceneChange(SceneLoader.GetCurrentScene());
    }
    
    private bool OnPlayableSceneChange(SCENE_NAME newScene)
    {
        bool isInList = _scenes.Contains(newScene);
        bool shouldBeActive;

        switch (_activationSettings)
        {
            case ActivationSettings.ACTIVE_ONLY_THESE_SCENES:
                shouldBeActive = isInList;
                break;
            case ActivationSettings.ACTIVE_ONLY_OTHER_SCENES:
                shouldBeActive = !isInList;
                break;
            default:
                shouldBeActive = true;
                break;
        }

        if (shouldBeActive && _enableDelayActivation && ActionScheduler.Instance != null)
        {
            ActionScheduler.RunAfterDelay(_delayActivationTime, () => gameObject.SetActive(shouldBeActive), this.GetInstanceID());
            return true;
        }
        gameObject.SetActive(shouldBeActive);

        return true;
    }
}
