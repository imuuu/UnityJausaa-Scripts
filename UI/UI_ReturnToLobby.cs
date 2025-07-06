using Game.UI;
using UnityEngine;

public class UI_ReturnToLobby : MonoBehaviour
{
    public void ReturnToLobby()
    {
        LoadToScene();
    }

    private void LoadToScene()
    {
        SceneLoader.PreloadSceneAsync(SCENE_NAME.Lobby, (scene) =>
        {
            ActionScheduler.RunAfterDelay(1, () =>
            {
                MoveToScene(scene);
            });
        });
    }

    private void MoveToScene(string scene)
    {
        ManagerUI.Instance.ClosePage(PAGE_TYPE.PAUSE_MENU);
        SceneLoader.ActivatePreloadedScene(scene);
        SceneLoader.UnloadSceneAsync(SCENE_NAME.ToxicLevel.ToString());
    }
}