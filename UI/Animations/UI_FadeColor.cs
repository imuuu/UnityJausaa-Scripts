using UnityEngine;
using Nova;
using UI.Animations;
using Sirenix.OdinInspector;

namespace Game.UI
{
    public class UI_FadeColor : MonoBehaviour
    {
        public enum FadeType { FadeIn, FadeOut, Toggle }
        [SerializeField] private UIBlock _uiBlock;
        [SerializeField, Min(0)] private float _fadeDuration = 0.5f;

        [SerializeField, BoxGroup("OnStart")] private bool _fadeOnStart = false;
        [SerializeField, BoxGroup("OnStart"), ShowIf("_fadeOnStart")] private FadeType _fadeTypeOnStart = FadeType.FadeOut;
        [SerializeField, BoxGroup("OnEnable")] private bool _fadeOnEnable = false;
        [SerializeField, BoxGroup("OnEnable"), ShowIf("_fadeOnEnable")] private FadeType _fadeTypeOnEnable = FadeType.FadeOut;

        private Color _originalColor;

        private BodyFadeoutAnimationHidden _bodyFadeoutAnimation;
        private AnimationHandle _bodyFadeoutHandle;
        private BodyFadeinAnimationHidden _bodyFadeinAnimation;
        private AnimationHandle _bodyFadeinHandle;

        private GradientFadeoutAnimationHidden _gradientFadeoutAnimation;
        private AnimationHandle _gradientFadeoutHandle;
        private GradientFadeinAnimationHidden _gradientFadeinAnimation;
        private AnimationHandle _gradientFadeinHandle;

        private bool _init = false;

        private void Awake()
        {
            if (_uiBlock == null)
            {
                _uiBlock = GetComponent<UIBlock>();
            }
            _originalColor = _uiBlock.Color;
        }

        private void Start()
        {
            Init();
            if (_fadeOnStart)
            {
                StartFade(_fadeTypeOnStart);
            }
        }

        private void OnEnable()
        {
            Init();
            if (_fadeOnEnable)
            {
                StartFade(_fadeTypeOnEnable);
            }
        }

        private void Init()
        {
            if (_init) return;

            _init = true;
            _bodyFadeoutAnimation = new BodyFadeoutAnimationHidden();
            _bodyFadeoutAnimation.Target = _uiBlock;

            _bodyFadeinAnimation = new BodyFadeinAnimationHidden();
            _bodyFadeinAnimation.Target = _uiBlock;
            _bodyFadeinAnimation.TargetColor = _originalColor;

            if (_uiBlock is UIBlock2D block2D)
            {
                _gradientFadeoutAnimation = new GradientFadeoutAnimationHidden();
                _gradientFadeoutAnimation.Target = block2D;

                _gradientFadeinAnimation = new GradientFadeinAnimationHidden();
                _gradientFadeinAnimation.Target = block2D;
                _gradientFadeinAnimation.TargetGradient = block2D.Gradient.Color;
            }
        }

        /// <summary>
        /// Triggers the fade based on the provided FadeType.
        /// </summary>
        /// <param name="fadeType">Choose FadeIn, FadeOut, or Toggle.</param>
        [BoxGroup("TEST Buttons"), Button("Start Fade")]
        public void StartFade(FadeType fadeType)
        {
            switch (fadeType)
            {
                case FadeType.FadeIn:
                    // Cancel any ongoing fade-out and start fade-in.

                    _bodyFadeoutHandle.Cancel();
                    _bodyFadeinHandle.Cancel();
                    _bodyFadeinHandle = _bodyFadeinAnimation.Run(_fadeDuration);

                    _gradientFadeoutHandle.Cancel();
                    _gradientFadeinHandle.Cancel();
                    _gradientFadeinHandle = _gradientFadeinAnimation.Run(_fadeDuration);
                    break;
                case FadeType.FadeOut:
                    // Cancel any ongoing fade-in and start fade-out.
                    _bodyFadeinHandle.Cancel();
                    _bodyFadeoutHandle.Cancel();
                    _bodyFadeoutHandle = _bodyFadeoutAnimation.Run(_fadeDuration);

                    _gradientFadeinHandle.Cancel();
                    _gradientFadeoutHandle.Cancel();
                    _gradientFadeoutHandle = _gradientFadeoutAnimation.Run(_fadeDuration);
                    break;
                case FadeType.Toggle:
                    // Toggle based on current alpha (threshold 0.5).
                    float currentAlpha = _uiBlock.Color.a;
                    if (currentAlpha < 0.5f)
                    {
                        StartFade(FadeType.FadeIn);
                    }
                    else
                    {
                        StartFade(FadeType.FadeOut);
                    }
                    break;
            }
        }        
    }
}
