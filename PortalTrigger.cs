using UnityEngine;
using Unity.Cinemachine;

// TODO make this better to fit more use cases for other maps
public class PortalTrigger : MonoBehaviour
{
    // private bool _playerInside = false;
    // [SerializeField] private PlayerMovement _playerMovement;

    [Header("Jump Camera")]
    [SerializeField] private CinemachineCamera _jumpCam;

    public void Trigger()
    {
        Player player = Player.Instance;

        if (player == null)
        {
            Debug.LogError("<color=#1db8fb>[PortalTrigger]</color> Player instance is null. Cannot trigger portal.");
            return;
        }

        player.transform.LookAt(this.transform.position);
        player.GetComponent<PlayerMovement>()._isPerformingAction = true;
        PlayerAnimationController animationController = player.GetComponentInChildren<PlayerAnimationController>();

        animationController.StartJump();
        if (_jumpCam != null)
        {
            _jumpCam.Priority = 20;
        }

        LoadToScene();
    }

    private void LoadToScene()
    {
        Debug.Log($"<color=#1db8fb>[PortalTrigger]</color> Player exited the trigger zone.");
        SceneLoader.PreloadSceneAsync(SCENE_NAME.ToxicLevel, (scene) =>
        {
            Debug.Log($"<color=#1db8fb>[PortalTrigger]</color> Scene {scene} preloaded.");
            ActionScheduler.RunAfterDelay(CONSTANTS.SCENE_LOAD_LOBBY_WAIT_TIME, () =>
            {
                Events.OnPlayableSceneStaticWaitBeforeLoad.Invoke(SCENE_NAME.ToxicLevel, CONSTANTS.SCENE_LOAD_LOBBY_WAIT_TIME_AFTER_PRELOAD);
                ActionScheduler.RunAfterDelay(CONSTANTS.SCENE_LOAD_LOBBY_WAIT_TIME_AFTER_PRELOAD, () =>
                {
                    MoveToScene(scene);
                });
            });
        });
    }

    private void MoveToScene(string scene)
    {
        SceneLoader.ActivatePreloadedScene(scene);
        SceneLoader.UnloadSceneAsync(SCENE_NAME.Lobby.ToString());

        Player player = Player.Instance;

        PlayerAnimationController animationController = player.GetComponentInChildren<PlayerAnimationController>();
        animationController.StartLanding();
    }
}
