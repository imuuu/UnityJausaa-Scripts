using System;
using System.Collections.Generic;
using Game.StatSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.BuffSystem
{

    [CreateAssetMenu(fileName = "ModifierStringTemplates", menuName = "ModifierStringTemplates", order = 0)]
    public class ModifierStringTemplates : ScriptableObject
    {
        [Serializable]
        public class Template
        {
            public STAT_TYPE target;
            public MODIFIER_TYPE modifierType;
            [InfoBox("Use {0} to insert the modifier's value. For example: \"+{0} base damage\"")]
            public string templateString;
        }

        public List<Template> templates = new ();

        public string GetTemplate(Modifier modifier)
        {
            foreach (Template template in templates)
            {
                if (template.target == modifier.GetTarget() && template.modifierType == modifier.GetTYPE())
                {
                    return string.Format(template.templateString, modifier.GetValue());
                }
            }

            Debug.LogWarning($"No TEXT template found for target: {modifier.GetTarget()} and type: {modifier.GetTYPE()}");
            return string.Empty;
        }
    }
}