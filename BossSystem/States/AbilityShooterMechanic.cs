using System.Collections.Generic;
using Game.Extensions;
using Game.SkillSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.BossSystem
{
    /// <summary>
    /// Uses MechanicTrigger selection/gates to choose holders, then fires a SkillDefinition
    /// via ISkillExecuteHandler. Global CD can be overridden or defaults to _triggerInterval.
    /// </summary>
    public sealed class AbilityShooterMechanic : MechanicTrigger
    {
        [BoxGroup("Ability Shooter"), SerializeField] private Component _skillExecuteHandler;
        [BoxGroup("Ability Shooter"), SerializeField] private SkillDefinition _skillDefinition;
        [BoxGroup("Ability Shooter"), LabelText("Override global CD (sec, 0=interval)"), Min(0f)][SerializeField] private float _globalCdOverride;

        protected override void ExecuteActivation(List<MechanicHolder> targets)
        {
            var exec = _skillExecuteHandler as ISkillExecuteHandler;
            if (exec == null || _skillDefinition == null || targets == null || targets.Count == 0) return;
            exec.ExecuteSkill(_skillDefinition, targets);
        }

        protected override float GetActivationGlobalCooldownSeconds(List<MechanicHolder> targets)
        {
            return _globalCdOverride > 0f ? _globalCdOverride : _triggerInterval;
        }

        [Button]
        private void FindSkillExecuteHandler()
        {
            Transform root = transform;
            for (int i = 0; i < 10; i++)
            {
                if (root == null) break;

                if (root.GetComponent<IOwner>() == null)
                {
                    root = root.parent;
                    continue;
                }

                break;
            }

            Debug.Log($"Finding skill execute handler in: {root.name}");
            List<ISkillExecuteHandler> skillExecuteHandler = new();
            root.TraverseChildren<ISkillExecuteHandler>(
                skillExecuteHandler,
                includeInactive: true,
                includeSelf: true,
                breakOnFirstMatch: true);

            if (skillExecuteHandler.Count > 0)
                _skillExecuteHandler = skillExecuteHandler[0] as Component;
            else
                Debug.LogWarning("No skill execute handler found.");

        }
    }
}
