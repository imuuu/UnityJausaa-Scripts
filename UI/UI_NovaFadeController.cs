using UnityEngine;
using Game.UI;

public class UI_NovaFadeController : MonoBehaviour
{
    [SerializeField] private UI_FadeColor ui_Fade;

    private void OnEnable()
    {
        Events.OnPlayableSceneStaticWaitBeforeLoad.AddListener(OnPlayableSceneStaticWaitBeforeLoad);
        Events.OnActivatePreloadedScene.AddListener(OnActivatePreloadedScene);
    }

    private bool OnActivatePreloadedScene(SCENE_NAME param)
    {
        FadeOut();
        return true;
    }

    private bool OnPlayableSceneStaticWaitBeforeLoad(SCENE_NAME sceneName, float delay)
    {
        FadeIn();
        return true;
    }

    public void FadeIn()
    {
        ui_Fade.StartFade(UI_FadeColor.FadeType.FadeIn);
    }

    public void FadeOut()
    {
        ui_Fade.StartFade(UI_FadeColor.FadeType.FadeOut);
    }
}
