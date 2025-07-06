using UnityEngine;
using UnityEngine.Audio;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Game.SkillSystem;

namespace Game.AudioSystem
{
    // public enum SoundCategory
    // {
    //     Skills,
    //     Music,
    //     Effects,
    //     Mobs
    // }

    [CreateAssetMenu(menuName = "Audio/Sound Data")]
    public class AudioSoundData : ScriptableObject
    {
        [Title("Audio Clip Settings")]
        [PropertyOrder(0)]
        public AudioClip Clip;

        [PropertyOrder(1)]
        public AudioMixerGroup MixerGroup;

        [PropertyOrder(2), Tooltip("Loop playback of this clip.")]
        public bool Loop;

        [PropertyOrder(3), Tooltip("Play automatically on spawn.")]
        public bool PlayOnAwake;

        [PropertyOrder(4), Tooltip("Subject to frequent sound pooling rules.")]
        public bool FrequentSound;

        [PropertyOrder(5), Range(0f, 1f), Tooltip("Volume multiplier for this clip.")]
        public float Volume = 1f;

        [PropertyOrder(6), Range(-3f, 3f), Tooltip("Pitch multiplier for this clip (1 = normal pitch).")]
        public float Pitch = 1f;

        [PropertyOrder(7), Range(-1f, 1f), Tooltip("Stereo pan: -1 = left, 1 = right.")]
        public float StereoPan = 0f;

        //––––––––––––––––––––––––––––––––––––––––––––––––––––
        [Title("Library Classification")]
        [PropertyOrder(10)]
        [EnumToggleButtons]
        public SoundCategory Category;

        [ShowIf("@Category == SoundCategory.Skills")]
        [LabelText("Skill Type")]
        [PropertyOrder(11)]
        public SKILL_NAME SkillType;

        [ShowIf("@Category == SoundCategory.Music")]
        [LabelText("Music Type")]
        [PropertyOrder(12)]
        public MUSIC_TYPE MusicType;

        [ShowIf("@Category == SoundCategory.Effects")]
        [LabelText("Effect Type")]
        [PropertyOrder(13)]
        public EFFECT_TYPE EffectType;

        [ShowIf("@Category == SoundCategory.Mobs")]
        [LabelText("Mob Type")]
        [PropertyOrder(14)]
        public MOB_TYPE MobType;

#if UNITY_EDITOR
        [PropertySpace(10, 0)]
        [PropertyOrder(15)]
        [Button("Add To Library"), GUIColor(0.2f, 0.8f, 0.2f)]
        [ShowIf("@!SoundLibraryObject.Instance.Contains(this)")]
        private void AddToLibrary()
        {
            SoundLibraryObject.Instance.AddSound(this);
            EditorUtility.SetDirty(SoundLibraryObject.Instance);
            Debug.Log($"Added '{name}' to Sound Library under {Category} -> {GetSubcategory()}");
        }

        [ShowInInspector, ReadOnly, PropertyOrder(20)]
        private bool IsInLibrary => SoundLibraryObject.Instance.Contains(this);

        private string GetSubcategory()
        {
            return Category switch
            {
                SoundCategory.Skills => SkillType.ToString(),
                SoundCategory.Music => MusicType.ToString(),
                SoundCategory.Effects => EffectType.ToString(),
                SoundCategory.Mobs => MobType.ToString(),
                _ => string.Empty
            };
        }
#endif
    }
}
