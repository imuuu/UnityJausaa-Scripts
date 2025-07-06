using System.Collections.Generic;
using Nova;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneCameraHandler : MonoBehaviour
{
    [InfoBox("Higher priority camera will be active")]
    [SerializeField] private int _cameraPriority = 0; 

    private readonly static List<SceneCameraHandler> _sceneCameras = new ();
    private bool _isActive = false;
    private void Awake()
    {
        Camera existingMainCamera = Camera.main;

        if (existingMainCamera != null && existingMainCamera.gameObject != this.gameObject)
        {
            //Scene currentScene = SceneManager.GetActiveScene();
            //Debug.Log($"[{currentScene.name}] Disabling {gameObject.name} because the root scene has a Main Camera.");
            gameObject.SetActive(false); 
        }
        // else


        _sceneCameras.Add(this);

        SceneCameraHandler highest = GetMostHighestPriorityCamera();
        
        foreach (var cam in _sceneCameras)
            cam.DeactivateCamera();

        highest.ActivateCamera();
    }

    public int GetCameraPriority()
    {
        return _cameraPriority;
    }

    public void ActivateCamera()
    {
        if (_isActive) return;

        _isActive = true;
        gameObject.SetActive(true);

        InitScreenSpaces();

        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log($"{gameObject.name} is active in scene: {currentScene.name}");
    }

    public void DeactivateCamera()
    {
        if (!_isActive) return;

        _isActive = false;
        gameObject.SetActive(false);
    }

    public SceneCameraHandler GetMostHighestPriorityCamera()
    {
        SceneCameraHandler highestPriorityCamera = null;
        int highestPriority = int.MinValue;

        foreach (SceneCameraHandler camera in _sceneCameras)
        {
            if (camera.GetCameraPriority() > highestPriority)
            {
                highestPriority = camera.GetCameraPriority();
                highestPriorityCamera = camera;
            }
        }

        return highestPriorityCamera;
    }

    private void InitScreenSpaces()
    {
        ScreenSpace[] screenSpaces = FindObjectsByType<ScreenSpace>(FindObjectsSortMode.None);
        foreach (ScreenSpace screenSpace in screenSpaces)
        {
            screenSpace.TargetCamera = GetComponent<Camera>();
        }
    }

}
