using Nova;
using Sirenix.OdinInspector;
using UnityEngine;
public class UI_MoveCurve : MonoBehaviour 
{   
    [SerializeField] private float _duration = 0.5f;
    [SerializeField] private UI_MoveCurveAnimation _moveCurveAnimation;
    private AnimationHandle _animationHandle;


    [Button]
    public void Play()
    {
        _animationHandle.Cancel();
        _animationHandle = _moveCurveAnimation.Run(_duration);
    }
}