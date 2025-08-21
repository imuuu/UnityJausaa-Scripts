using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SO_SceneValidatorConfig", menuName = "Configurations/Scene Validator Config")]
public class SceneValidatorConfig : ScriptableObject
{
    [Header("Object names must be unique")]
    [Tooltip("List of object names that can be only one instance in the scenes.")]
    public string[] ObjectNamesToBeOne;
    [Header("Prefabs must exist in standalone scenes")]
    [Tooltip("List of prefabs that must exist in standalone scenes.")]
    public GameObject[] RequiredPrefabs;
}

public static class SceneValidator
{
    private static SceneValidatorConfig _config;
    private const string PREFIX = "<color=#FF00FF>[SceneValidator]</color>";

    private static Dictionary<string, GameObject> _objectNamesToBeOne = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        _config = Resources.Load<SceneValidatorConfig>("SO_SceneValidatorConfig");

        _objectNamesToBeOne = new Dictionary<string, GameObject>();

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

        Dictionary<string, GameObject> rootObjectNames = new();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene activeScene = SceneManager.GetSceneAt(i);
            foreach (GameObject rootObject in activeScene.GetRootGameObjects())
            {
                if (rootObjectNames.ContainsKey(rootObject.name))
                {
                    continue;
                }

                string rootObjectName = rootObject.name.Replace("(Clone)", "").Trim();

                if (!_objectNamesToBeOne.ContainsKey(rootObjectName))
                {
                    for (int j = 0; j < _config.ObjectNamesToBeOne.Length; j++)
                    {
                        if (_config.ObjectNamesToBeOne[j] == rootObjectName)
                        {
                            Debug.Log($"{PREFIX} Found object name that must be unique: {rootObjectName}");
                            _objectNamesToBeOne.Add(rootObjectName, rootObject);
                            break;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"{PREFIX} Duplicate root object name found: {rootObjectName}. This may cause issues.");
                    GameObject.Destroy(rootObject);
                    continue;
                }

                if (rootObjectNames.ContainsKey(rootObjectName))
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
            else if (!rootObjectNames[prefabName].activeSelf)
            {
                Debug.Log($"{PREFIX} Respawn inactive prefab: {prefabName}");

                GameObject.Destroy(rootObjectNames[prefabName]);
                Object.Instantiate(prefab);
            }
        }
        
        if(_config.ObjectNamesToBeOne != null && _config.ObjectNamesToBeOne.Length > 0)
        {
            foreach (string objectName in _config.ObjectNamesToBeOne)
            {
                
            }
        }
    }
}
