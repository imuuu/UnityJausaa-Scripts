using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// ------------------------------------------------------------
// Boss Phase System (MonoBehaviours + Plain C# classes)
// - No ScriptableObjects needed.
// - Add BossPhaseController to your Boss prefab.
// - Add BossMechanic-derived components for individual abilities.
// - Configure Phases array in the inspector: which mechanics are
//   enabled/disabled per phase and what transitions trigger the next phase.
// - All runtime code avoids per-frame GC allocations (no LINQ, lists reused).
// ------------------------------------------------------------

namespace Game.BossSystem
{
    /// <summary>
    /// Example: shoots a projectile toward the player at fixed intervals if CanAct.
    /// Replace SpawnProjectile with your pooling/ability system.
    /// </summary>
    public sealed class HydraHeadRemoveMechanic : MechanicTrigger
    {
        [SerializeField] private AnimationClip _removeHeadAnimation;
        [SerializeField] private Transform _headsParent;

        [Title("Spawnable")]
        [SerializeField] private GameObject _hydraSnakeMob;

        private int _totalChilds = -1;

        private void OnEnable()
        {
            _totalChilds = -1;
        }

        protected override void ExecuteActivation(List<MechanicHolder> targets)
        {

        }

        public override void OnMechanicInit()
        {
            base.OnMechanicInit();
        }
        public override void OnPhaseEnter()
        {
            base.OnPhaseEnter();
        }

        public override bool ActivateNow(bool ignoreGlobalCooldown = false, bool ignorePerHolderCooldown = false)
        {
            base.ActivateNow(ignoreGlobalCooldown, ignorePerHolderCooldown);

            Debug.Log("|||||||||||||||||||||||||> HydraHeadRemoveMechanic ActivateNow REMOVE HEAD");
            PlayAnimation();
            return true;
        }

        private GameObject GetNextChild(Transform transform)
        {
            if (transform.childCount == 0) return null;

            return transform.GetChild(_totalChilds-- - 1).gameObject;
        }

        private void PlayAnimation()
        {
            if (_totalChilds == -1)
            {
                _totalChilds = _headsParent.childCount;
                Debug.Log("|||||||||||||||||||||||||> HydraHeadRemoveMechanic PlayAnimation totalChilds: " + _totalChilds);
            }

            GameObject lastChild = GetNextChild(_headsParent);

            if (lastChild == null) return;

            AnimationDirector _animationDirector = lastChild.GetComponent<AnimationDirector>();

            _animationDirector.TriggerAttack(_removeHeadAnimation, queueIfBusy: true, () =>
            {
                if (lastChild != null)
                {
                    lastChild.SetActive(false);
                    SpawnHydraSnake();
                    Destroy(lastChild, 1f);
                }
            });
        }

        private GameObject GetLastChild(Transform transform)
        {
            if (transform.childCount == 0) return null;
            return transform.GetChild(transform.childCount - 1).gameObject;
        }

        private void SpawnHydraSnake()
        {
            GameObject hydraSnake = Instantiate(_hydraSnakeMob);
            Vector3 randomPosition = UnityEngine.Random.insideUnitSphere * 45f;
            Vector3 playerPosition = Player.Instance.transform.position;

            Vector3 spawnPosition = _headsParent.position + randomPosition;
            spawnPosition.y = 45f;

            hydraSnake.transform.localPosition = spawnPosition;

            Vector3 targetPosition = _headsParent.position + UnityEngine.Random.insideUnitSphere * 10f;

            SnakeIntroSequence introSequence = hydraSnake.GetComponent<SnakeIntroSequence>();

            introSequence.StartIntroAt(targetPosition);

        }
    }
}