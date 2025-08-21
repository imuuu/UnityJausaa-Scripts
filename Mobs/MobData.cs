using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Sirenix.Serialization;


#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;
#endif

namespace Game.Mobs
{
    [CreateAssetMenu(fileName = "MobData", menuName = "Game/Mobs/Mob Data")]
    public class MobData : SerializedScriptableObject
    {
        public MOB_TYPE MobType;
        [BoxGroup("Visual & Prefab", ShowLabel = false)]
        [LabelText("Mob Prefab")]
        public GameObject Prefab;

        [BoxGroup("Basic Stats")]
        public int HP = 100;
        [BoxGroup("Basic Stats")]
        public float HpMultiplierEachMinute = 1.1f;

        [BoxGroup("Combat Stats")]
        [MinValue(0)]
        public float Damage = 10;
        [BoxGroup("Combat Stats")]
        public float DamageMultiplierEachMinute = 1.03f;

        [BoxGroup("Movement Stats")]
        [MinValue(0f), SuffixLabel("units/s", Overlay = true)]
        public float MovementSpeed = 5f;

        [BoxGroup("Movement Stats")]
        [MinValue(0f), SuffixLabel("°/s", Overlay = true)]
        public float RotationSpeed = 5f;


        [BoxGroup("Drops")]
        [LabelText("Currency Drop Entries")]
        public List<CurrencyDropEntry> CurrencyDrops = new ();


        public float GetHealthWithMultiplier(float minutes)
        {
            return HP * Mathf.Pow(HpMultiplierEachMinute, minutes);
        }

        public float GetHealthWithRoundTime()
        {
            float minutes = 1f;
            if(ManagerGame.Instance != null) minutes = ManagerGame.Instance.GetCurrentRoundTimeMinutes();
            
            return GetHealthWithMultiplier(minutes);
        }
        private Color GetHPBarColor(float value)
        {
            return Color.Lerp(Color.red, Color.green, value / 1000f);
        }

#if UNITY_EDITOR
        [PropertySpace(10)]
        [Button("Add To Library"), GUIColor(0.2f, 0.8f, 0.2f)]
        [ShowIf("@MobLibrary.Instance != null && !MobLibrary.Instance.Contains(this)")]
        private void AddToLibrary()
        {
            var library = MobLibrary.Instance;
            if (library == null)
            {
                Debug.LogWarning("No MobLibrary instance found in the project.");
                return;
            }

            library.AddMob(this);
            EditorUtility.SetDirty(library);
            Debug.Log($"Added '{name}' to Mob Library");
        }
#endif
    }
}
