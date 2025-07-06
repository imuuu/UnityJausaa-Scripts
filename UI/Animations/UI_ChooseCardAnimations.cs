using Nova;
using Sirenix.OdinInspector;
using UnityEngine;
namespace Game.UI
{
    public class UI_ChooseCardAnimations : MonoBehaviour
    {
        [SerializeField] private bool _startAtEnable = false;
        [SerializeField, BoxGroup("Move Animation")] private float _moveDuration = 0.5f;
        [SerializeField, BoxGroup("Move Animation")] private UI_MoveCurveAnimation _moveStartAnimation;
        [SerializeField, BoxGroup("Move Animation")] private UI_MoveCurveAnimation _moveEndAnimation;

        private AnimationHandle _animationHandle;
        private Vector3 _startPosition;

        private void Start()
        {
            _startPosition = _moveStartAnimation.Target.Position.Value;
            //PlayAnimation();
        }

        private void OnEnable()
        {
            if (_startAtEnable)
            {
                PlayAnimation();
            }
        }

        [Button]
        public void PlayAnimation()
        {
            _moveStartAnimation.Target.Position = _startPosition;
            _animationHandle.Cancel();
            _animationHandle = _moveStartAnimation.Run(_moveDuration)
            .Chain(_moveEndAnimation,_moveDuration);
        }
    }
}
