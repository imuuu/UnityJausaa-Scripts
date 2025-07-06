using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SO_SceneValidatorConfig", menuName = "Configurations/Scene Validator Config")]
public class SceneValidatorConfig : ScriptableObject
{
    [Header("Prefabs must exist in standalone scenes")]
    [Tooltip("List of prefabs that must exist in standalone scenes.")]
    public GameObject[] RequiredPrefabs;
}

public static class SceneValidator
{
    private static SceneValidatorConfig _config;
    private const string PREFIX = "<color=#FF00FF>[SceneValidator]</color>";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        _config = Resources.Load<SceneValidatorConfig>("SO_SceneValidatorConfig");

        if (_config == null)
        {
            Debug.LogError($"{PREFIX} SceneValidatorConfig not found in Resources. Please create one and place it in a 'Resources' folder.");
            return;
        }

        // Subscribe to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == CONSTANTS.ROOT_SCENE_NAME)
        {
            Debug.Log($"{PREFIX} Scene '{scene.name}' is the root scene. Skipping prefab validation.");
            return;
        }

        Dictionary<string, GameObject> rootObjectNames = new ();
        
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene activeScene = SceneManager.GetSceneAt(i);
            foreach (GameObject rootObject in activeScene.GetRootGameObjects())
            {
                if(rootObjectNames.ContainsKey(rootObject.name))
                {
                    continue;
                }

                string rootObjectName = rootObject.name.Replace("(Clone)", "").Trim();
                if(rootObjectNames.ContainsKey(rootObjectName))
                {
                    continue;
                }
                rootObjectNames.Add(rootObjectName, rootObject);
            }
        }

        foreach (GameObject prefab in _config.RequiredPrefabs)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"{PREFIX} One of the required prefabs in the config is null. Skipping...");
                continue;
            }

            string prefabName = prefab.name;

            if (!rootObjectNames.ContainsKey(prefabName))
            {
                Debug.Log($"{PREFIX} Spawn missing prefab: {prefabName}");
                Object.Instantiate(prefab);
            }
            else if(!rootObjectNames[prefabName].activeSelf)
            {
                Debug.Log($"{PREFIX} Respawn inactive prefab: {prefabName}");

                GameObject.Destroy(rootObjectNames[prefabName]);
                Object.Instantiate(prefab);
            }
        }
    }
}
