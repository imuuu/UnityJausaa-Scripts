using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneInformer : MonoBehaviour
{
    [SerializeField] private SCENE_NAME _scene;
    private const string PREFIX = "<color=#1db8fb>[SceneInformer]</color>";

    private void Start()
    {
        Debug.Log($"{PREFIX} Current scene: {_scene}");

        Scene currentScene = gameObject.scene;
        if (currentScene.IsValid() && currentScene.isLoaded)
        {
            SceneManager.SetActiveScene(currentScene);
            Debug.Log($"{PREFIX} Set active scene: {currentScene.name}");
        }

        SceneLoader.SetCurrentScene(_scene);
    }
}
