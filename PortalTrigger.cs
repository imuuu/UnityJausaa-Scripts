using UnityEngine;
using Unity.Cinemachine;

public class PortalTrigger : MonoBehaviour
{
    // private bool _playerInside = false;
    // [SerializeField] private PlayerMovement _playerMovement;

    [Header("Jump Camera")]
    [SerializeField] private CinemachineCamera _jumpCam;

    // private void OnTriggerEnter(Collider other)
    // {
    //     if (other.CompareTag("Player"))
    //     {
    //         _playerInside = true;
    //         _playerMovement = other.GetComponentInParent<PlayerMovement>();


    //         // SceneLoader.PreloadSceneAsync("ToxicLevel", (scene) =>
    //         // {
    //         //     Debug.Log($"<color=#1db8fb>[PortalTrigger]</color> Scene {scene} preloaded.");
    //         //     SceneLoader.ActivatePreloadedScene(scene);
    //         // });
    //     }
    // }

    // private void OnTriggerExit(Collider other)
    // {
    //     if (other.CompareTag("Player"))
    //     {
    //         _playerInside = false;
    //     }
    // }

    // private void Update()
    // {
    //     if (_playerInside && _playerMovement != null && Input.GetKeyDown(KeyCode.E))
    //     {
    //         //_playerMovement.FaceTowardsTarget(this.transform.position);
    //         _playerMovement.transform.LookAt(this.transform.position);
    //         _playerMovement._isPerformingAction = true;

    //         if (_jumpCam != null)
    //         {
    //             _jumpCam.Priority = 20;
    //         }

    //         LoadToScene();
    //     }
    // }

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
    }
}
