using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using System;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [Title("Add scenes from script")]
    [ShowInInspector, ReadOnly]
    private static List<string> _sceneOrder = new()
    {
        "RootScene", // Root Scene
        "UI_Scene",  // UI Scene
        //"GameScene"  // Game Scene
        //"ToxicLevel", // Toxic Scene
        "Lobby"
    };

    private const string PREFIX = "<color=#1db8fb>[SceneLoader]</color>";
    private const string KEY_EXTRA_SCENES = "SceneLoader_ExtraScenes";
    private static bool hasInitialized = false;

    private static SceneLoader _instance;
    private Dictionary<string, AsyncOperation> _preloadOperations = new Dictionary<string, AsyncOperation>();

    private static SCENE_NAME _currentScene = SCENE_NAME.RootScene;

    private void Awake()
    {
        // ensure one persistent loader
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // If we've already done this once, don't do it again
        if (hasInitialized)
            return;

        hasInitialized = true;

        // Now perform the loading
        if (_sceneOrder.Count == 0)
        {
            Debug.LogError("Scene order is empty! Please specify scenes in the SceneLoader.");
            return;
        }

        Initialize();
    }

    public static void Initialize()
    {
        Debug.Log($"{PREFIX} Initializing SceneLoader...");
        hasInitialized = true;
        MergeExtraScenes(ref _sceneOrder);
        ClearExtraScenes();
        LoadMainThenAdditive(_sceneOrder[0], _sceneOrder.Skip(1).ToArray());
    }

    public static void SetCurrentScene(SCENE_NAME sceneName)
    {   
        _currentScene = sceneName;
        Events.OnPlayableSceneChangeEnter.Invoke(sceneName);
    }

    public static SCENE_NAME GetCurrentScene()
    {
        return _currentScene;
    }

    public static bool IsCurrentScenePlayable()
    {
        return (int)_currentScene >= CONSTANTS.PLAYABLE_SCENE_INDEX_THRESHOLD;
    }

    /// <summary>
    /// Loads the given scenes additively. 
    /// If a scene is already loaded, it will not be loaded again.
    /// </summary>
    /// <param name="sceneNames">Array of scene names to load additively.</param>
    public static void LoadScenesAdditively(params string[] sceneNames)
    {
        if (sceneNames == null || sceneNames.Length == 0)
        {
            Debug.LogError($"{PREFIX} No scenes specified to load additively!");
            return;
        }

        foreach (var sceneName in sceneNames)
        {
            if (!IsSceneLoaded(sceneName))
            {
                SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive).completed += (op) =>
                {
                    Debug.Log($"{PREFIX} {sceneName} <color=green>loaded</color> (Additive).");
                };
            }
            else
            {
                Debug.Log($"{PREFIX} {sceneName} is already loaded.");
            }
        }
    }

    /// <summary>
    /// Loads a single scene with LoadSceneMode.Single (unloads all other scenes) 
    /// and then optionally loads additional scenes additively.
    /// </summary>
    /// <param name="mainSceneName">The main scene to load (Single).</param>
    /// <param name="additionalScenes">Any additional scenes to load additively afterward.</param>
    public static void LoadMainThenAdditive(string mainSceneName, params string[] additionalScenes)
    {
        if (string.IsNullOrEmpty(mainSceneName))
        {
            Debug.LogError($"{PREFIX} The main scene name is empty or null.");
            return;
        }

        if (IsSceneLoaded(mainSceneName))
        {
            //Debug.Log($"{PREFIX} {mainSceneName} is already loaded. Setting it active...");

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(mainSceneName));

            // Load additional scenes additively (if any)
            if (additionalScenes != null && additionalScenes.Length > 0)
            {
                LoadScenesAdditively(additionalScenes);
            }
        }
        else
        {
            // Load main scene in Single mode (unloads all other scenes)
            SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Single).completed += (op) =>
            {
                Debug.Log($"{PREFIX} {mainSceneName} <color=green>loaded</color> (Single).");

                // Optionally set active scene
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(mainSceneName));

                // Load additional scenes additively (if any)
                if (additionalScenes != null && additionalScenes.Length > 0)
                {
                    LoadScenesAdditively(additionalScenes);
                }
            };
        }
    }

    /// <summary>
    /// Checks if the specified scene is already loaded.
    /// </summary>
    private static bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName && scene.isLoaded)
            {
                return true;
            }
        }
        return false;
    }

    private static void MergeExtraScenes(ref List<string> sceneOrder)
    {
        ADDITIONAL_SCENES[] extraScenes = GetExtraScenes();
        if (extraScenes.Length == 0)
        {
            return;
        }

        List<string> extraSceneNames = new();
        for (int i = 0; i < extraScenes.Length; i++)
        {
            if (extraScenes[i] == ADDITIONAL_SCENES.NONE)
            {
                continue;
            }

            string sceneName = extraScenes[i].ToString();
            extraSceneNames.Add(sceneName);
        }

        sceneOrder.AddRange(extraSceneNames);
    }

    /// <summary>
    /// Begin loading a scene additively, but hold at 90% (won't activate).
    /// When the load reaches 95%, invokes onReady(sceneName).
    /// </summary>
    public static void PreloadSceneAsync(SCENE_NAME sceneName, Action<string> onReady)
    {
        if (_instance == null)
        {
            Debug.LogError($"{PREFIX} SceneLoader not initialized (no instance).");
            return;
        }
        _instance.StartCoroutine(_instance.PreloadCoroutine(sceneName, onReady));
    }

    private IEnumerator PreloadCoroutine(SCENE_NAME sceneName, Action<string> onReady)
    {
        string sceneNameStr = sceneName.ToString();
        if (IsSceneLoaded(sceneNameStr))
        {
            Debug.Log($"{PREFIX} {sceneName} is already loaded.");
            onReady?.Invoke(sceneNameStr);
            yield break;
        }

        if (_preloadOperations.ContainsKey(sceneNameStr))
        {
            Debug.Log($"{PREFIX} {sceneName} is already preloading.");
            yield break;
        }

        Debug.Log($"{PREFIX} Starting preload of {sceneName}...");
        var op = SceneManager.LoadSceneAsync(sceneNameStr, LoadSceneMode.Additive);
        op.allowSceneActivation = false;
        _preloadOperations[sceneNameStr] = op;

        Events.OnPlayableScenePreloadStart.Invoke(sceneName);

        // wait until it’s fully loaded (progress goes ~0.9)
        while (op.progress < 0.90f)
        {
            yield return null;
        }


        Debug.Log($"{PREFIX} {sceneName} preloaded (ready to activate).");

        Events.OnPlayableScenePreloadReady.Invoke(sceneName);
        onReady?.Invoke(sceneNameStr);
    }

    // ——— New API: activate a previously preloaded scene ———
    /// <summary>
    /// Allows a previously preloaded scene to activate and be added to the hierarchy.
    /// </summary>
    public static void ActivatePreloadedScene(string sceneName)
    {
        if (_instance == null)
        {
            Debug.LogError($"{PREFIX} SceneLoader not initialized (no instance).");
            return;
        }
        if (!_instance._preloadOperations.TryGetValue(sceneName, out var op))
        {
            Debug.LogError($"{PREFIX} No preload operation found for {sceneName}!");
            return;
        }

        Debug.Log($"{PREFIX} Activating {sceneName} now...");
        op.allowSceneActivation = true;
        _instance._preloadOperations.Remove(sceneName);

        Events.OnActivatePreloadedScene.Invoke(GetSceneName(sceneName));
    }

    // ——— New: Unload a single scene asynchronously with optional callback ———
    public static void UnloadSceneAsync(string sceneName, Action<string> onComplete = null)
    {
        if (!IsSceneLoaded(sceneName))
        {
            Debug.LogWarning($"{PREFIX} Cannot unload {sceneName}: not loaded.");
            return;
        }

        var op = SceneManager.UnloadSceneAsync(sceneName);
        op.completed += _ =>
        {
            Debug.Log($"{PREFIX} {sceneName} unloaded.");
            onComplete?.Invoke(sceneName);
        };
    }

    // ——— New: Unload multiple scenes asynchronously ———
    public static void UnloadScenesAsync(params string[] sceneNames)
    {
        if (sceneNames == null || sceneNames.Length == 0)
        {
            Debug.LogWarning($"{PREFIX} No scenes specified to unload.");
            return;
        }

        foreach (var name in sceneNames)
            UnloadSceneAsync(name);
    }

    public static ADDITIONAL_SCENES[] GetExtraScenes()
    {
        return ES3.Load(KEY_EXTRA_SCENES, new ADDITIONAL_SCENES[0]);
    }

    public static void SetExtraScenes(ADDITIONAL_SCENES[] sceneNames)
    {
        ES3.Save(KEY_EXTRA_SCENES, sceneNames);
    }

    public static void ClearExtraScenes()
    {
        ES3.DeleteKey(KEY_EXTRA_SCENES);
    }

    public static string TrimSceneNameFromPath(string sceneName)
    {
        return sceneName.Replace(CONSTANTS.ROOT_SCENE_PATH, "").Trim();
    }

    public static SCENE_NAME GetSceneName(string sceneName)
    {
        if (Enum.TryParse(sceneName, out SCENE_NAME scene))
        {
            return scene;
        }

        return SCENE_NAME.Lobby;
       
    }
}
