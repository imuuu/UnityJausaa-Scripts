using System.Collections.Generic;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.BossSystem
{
    /// <summary>
    /// Plays an AnimationClip using Animancer for selected holders.
    /// - Finds an AnimancerComponent by walking up from each holder (or uses override).
    /// - If a BossAnimationDirector is present on the same object as Animancer, calls
    ///   director.PlayAttackClip(clip, attackIndex) instead of direct Play.
    /// - No string state names; clip-only playback.
    /// - Global cooldown is clip.length + padding (or explicit override).
    /// </summary>
    public sealed class AnimationTriggerMechanic : MechanicTrigger
    {
        [FoldoutGroup("Animation"), BoxGroup("Animation/Target")]
        [SerializeField] private AnimancerComponent _animancerOverride;

        [FoldoutGroup("Animation"), BoxGroup("Animation/Target"), LabelText("Search depth up parents"), Min(0)]
        [SerializeField] private int _searchDepth = 8;

        [FoldoutGroup("Animation"), BoxGroup("Animation/Play"), SerializeField]
        private AnimationClip _clip;

        [FoldoutGroup("Animation"), BoxGroup("Animation/Play"), Min(0f), SerializeField]
        private float _fade = 0.1f;

        [FoldoutGroup("Animation"), BoxGroup("Animation/Play"), LabelText("Use layer playback"), SerializeField]
        private bool _playOnLayer = false;

        [FoldoutGroup("Animation"), BoxGroup("Animation/Play"), ShowIf(nameof(_playOnLayer)), Min(0), SerializeField]
        private int _layerIndex = 0;

        [FoldoutGroup("Animation"), BoxGroup("Animation/Play"), ShowIf(nameof(_playOnLayer)), Min(0f), SerializeField]
        private float _layerFade = 0.1f;

        // [FoldoutGroup("Animation"), BoxGroup("Animation/Play"), LabelText("Attack index (optional)"), SerializeField]
        // private int _attackIndex = -1;

        [FoldoutGroup("Animation"), BoxGroup("Animation/Cooldown"), LabelText("Extra padding"), Min(0f)]
        [SerializeField] private float _extraPadding = 0.0f;

        [FoldoutGroup("Animation"), BoxGroup("Animation/Cooldown"), LabelText("Override global CD (0 = auto)"), Min(0f)]
        [SerializeField] private float _globalCdOverride = 0f;

        private readonly Dictionary<Transform, AnimancerComponent> _cache = new Dictionary<Transform, AnimancerComponent>(32);

        protected override void ExecuteActivation(List<MechanicHolder> targets)
        {
            if (_clip == null) return;

            int n = targets.Count;
            for (int i = 0; i < n; i++)
            {
                var holder = targets[i];
                if (holder == null) continue;
                var ac = ResolveAnimancer(holder.transform);
                if (ac == null) continue;

                // Prefer BossAnimationDirector if available on the same object as Animancer
                var director = ac.GetComponent<AnimationDirector>();
                if (director != null)
                {
                    //director.PlayAttackClip(_clip, _attackIndex);
                    director.TriggerAttack(_clip, queueIfBusy: true);
                    continue;
                }

                // Fallback: direct Animancer playback
                if (_playOnLayer)
                {
                    var layer = ac.Layers[Mathf.Max(0, _layerIndex)];
                    var state = layer.Play(_clip);
                    layer.StartFade(1f, _layerFade);
                }
                else
                {
                    ac.Play(_clip, _fade);
                }
            }
        }

        protected override float GetActivationGlobalCooldownSeconds(List<MechanicHolder> targets)
        {
            if (_globalCdOverride > 0f) return _globalCdOverride;
            if (_clip != null) return Mathf.Max(0f, _clip.length + _extraPadding);
            return Mathf.Max(0f, _triggerInterval);
        }

        private AnimancerComponent ResolveAnimancer(Transform from)
        {
            if (_animancerOverride != null) return _animancerOverride;
            if (from == null) return null;
            if (_cache.TryGetValue(from, out var cached) && cached != null) return cached;

            Transform t = from;
            int steps = 0;
            while (t != null && steps++ <= _searchDepth)
            {
                var ac = t.GetComponent<AnimancerComponent>();
                if (ac != null)
                {
                    _cache[from] = ac;
                    return ac;
                }
                t = t.parent;
            }
            _cache[from] = null;
            return null;
        }
    }
}
