using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityToolbarExtender;

[InitializeOnLoad]
public static class CustomPlayToolbar
{
    private const string StartFromRootKey = "CustomPlayToolbar_StartFromRoot";
    private const string _lastSceneKey = "CustomPlayToolbar_lastScene";
    private const string AdditionalScenesKey = "CustomPlayToolbar_AdditionalScenes";

    private static bool _sceneSwitched = false; 

    static CustomPlayToolbar()
    {
        ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnToolbarGUI()
    {
        GUILayout.FlexibleSpace();

        GUILayout.Label("Additional Scenes:", GUILayout.Width(120));
        ADDITIONAL_SCENES selectedScene = (ADDITIONAL_SCENES)EditorPrefs.GetInt(AdditionalScenesKey, (int)ADDITIONAL_SCENES.NONE);
        ADDITIONAL_SCENES newSelectedScene = (ADDITIONAL_SCENES)EditorGUILayout.EnumPopup(selectedScene, GUILayout.Width(150));

        if (newSelectedScene != selectedScene)
        {
            EditorPrefs.SetInt(AdditionalScenesKey, (int)newSelectedScene);
        }

        bool startFromRoot = EditorPrefs.GetBool(StartFromRootKey, false);
        bool newValue = GUILayout.Toggle(startFromRoot, "Start From Root", "Button", GUILayout.Width(120));

        if (newValue != startFromRoot)
        {
            EditorPrefs.SetBool(StartFromRootKey, newValue);
            EditorPrefs.DeleteKey(_lastSceneKey);
        }

        
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        bool startFromRoot = EditorPrefs.GetBool(StartFromRootKey, false);
        string previousScenePath = EditorPrefs.GetString(_lastSceneKey);
        ADDITIONAL_SCENES selectedScene = (ADDITIONAL_SCENES)EditorPrefs.GetInt(AdditionalScenesKey, (int)ADDITIONAL_SCENES.NONE);

        if(selectedScene != ADDITIONAL_SCENES.NONE && state == PlayModeStateChange.EnteredPlayMode && !startFromRoot)
        {
            string scenePath = SceneManager.GetActiveScene().path;
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            SceneLoader.LoadMainThenAdditive(sceneName, selectedScene.ToString());
        }
        else if(!startFromRoot)
        {
            return;
        }
        else if (state == PlayModeStateChange.ExitingEditMode && startFromRoot && !_sceneSwitched)
        {
            _sceneSwitched = true;

            if (string.IsNullOrEmpty(previousScenePath))
            {
                EditorPrefs.SetString(_lastSceneKey, SceneManager.GetActiveScene().path);
            }

            if (SceneManager.GetActiveScene().path != CONSTANTS.ROOT_SCENE_PATH)
            {
                SceneLoader.ClearExtraScenes();
                SceneLoader.SetExtraScenes(new ADDITIONAL_SCENES[1]{selectedScene});

                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(CONSTANTS.ROOT_SCENE_PATH, OpenSceneMode.Single);

                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () => { EditorApplication.isPlaying = true; };
            }
        }
        else if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _sceneSwitched = false;
        }

        if (state == PlayModeStateChange.EnteredEditMode && !string.IsNullOrEmpty(previousScenePath))
        {
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            EditorPrefs.DeleteKey(_lastSceneKey);
        }

        //Debug.Log($"END ==========> Play State: {state} + startFromRoot: {startFromRoot} + sceneSwitched: {_sceneSwitched} + previousScenePath: {previousScenePath}");
    }


}
