using System.Collections.Generic;
using Nova;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.BuffSystem
{
    [System.Serializable]
    public class RarityDefinition
    {
        public MODIFIER_RARITY Rarity;
        public float Threshold;
        public Color MainColor;
        public Color MainGradientColor;
        public Color HoverColor;
        public Color HoverGradientColor;

        [Button]
        [PropertySpace(5, 5)]
        public void TestToFirst()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("This method should only be called in play mode.");
                return;
            }

            // UI_ChooseBuffCard found = null;

            // // Loop over every loaded scene
            // for (int i = 0; i < SceneManager.sceneCount; i++)
            // {
            //     var scene = SceneManager.GetSceneAt(i);
            //     if (!scene.isLoaded)
            //         continue;

            //     // Search each root GameObject in that scene
            //     foreach (var root in scene.GetRootGameObjects())
            //     {
            //         found = root.GetComponentInChildren<UI_ChooseBuffCard>(true);
            //         if (found != null)
            //         {
            //             Debug.Log($"Found UI_ChooseBuffCard in scene “{scene.name}” on GameObject “{found.gameObject.name}”", found.gameObject);

            //             ItemView itemView = found.GetComponent<ItemView>();

            //             ChooseBuffCardVisual visual = itemView.Visuals as ChooseBuffCardVisual;

            //             visual.ChanceColor(this);
            //             return;
            //         }
            //     }
            // }

            //Debug.LogWarning("No UI_ChooseBuffCard instance found in any loaded scene.");

            List<UI_ChooseBuffCard> results = new List<UI_ChooseBuffCard>();
            const int totalBuffs = 3;
            int count = 0;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                // Search each root GameObject
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    // true = include inactive children
                    var rootComponents = root.GetComponentsInChildren<UI_ChooseBuffCard>(true);
                    results.AddRange(rootComponents);

                    if( results.Count == 0) continue;

                    count += results.Count;
                    if (count >= totalBuffs)
                    {
                        break;
                    }
                }

                if (count >= totalBuffs)
                {
                    break;
                }
            }

            if (results.Count > 0)
            {
                Debug.Log($"Found {results.Count} UI_ChooseBuffCard instance(s):");
                foreach (UI_ChooseBuffCard card in results)
                {
                    Debug.Log($"• {card.gameObject.name} (Scene: {card.gameObject.scene.name})", card.gameObject);

                    ItemView itemView = card.GetComponent<ItemView>();

                    ChooseBuffCardVisual visual = itemView.Visuals as ChooseBuffCardVisual;
                    visual.ChanceColor(this);
                }
            }
            else
            {
                Debug.LogWarning("No UI_ChooseBuffCard instances found in any loaded scene.");
            }
        }


    }
}