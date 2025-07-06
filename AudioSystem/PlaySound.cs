using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.AudioSystem;
using Game.SkillSystem;
using Sirenix.OdinInspector;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum PlayMode
{
    OnEnable,
    Manual
}

public class PlaySound : MonoBehaviour
{
    private SoundLibraryObject _library => SoundLibraryObject.Instance;

    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // Category selection
    [EnumToggleButtons]
    [Title("Category")]
    [PropertySpace(0, 10)]
    [SerializeField]
    private SoundCategory _category;

    [ShowIf("@_category == SoundCategory.Skills")]
    [SerializeField]
    private SKILL_NAME _skillName;

    [ShowIf("@_category == SoundCategory.Music")]
    [SerializeField]
    private MUSIC_TYPE _musicType;

    [ShowIf("@_category == SoundCategory.Effects")]
    [SerializeField]
    private EFFECT_TYPE _effectType;

    [ShowIf("@_category == SoundCategory.Mobs")]
    [SerializeField]
    private MOB_TYPE _mobType;

    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    [Title("Random Multi Sounds")]
    [PropertySpace(10, 10)]
    [SerializeField][Tooltip("If enabled, the sound will be played from a random selection of clips.")]
    private bool _useRandomMultiple = false;

    // Single-clip selections (shown when not using random multi)
    [ShowIf("@_useRandomMultiple == false && _category == SoundCategory.Skills")]
    [ValueDropdown("@_library.GetClipsForSkill(_skillName)")]
    [SerializeField]
    private AudioSoundData _skillClip;

    [ShowIf("@_useRandomMultiple == false && _category == SoundCategory.Music")]
    [ValueDropdown("@_library.GetClipsForMusic(_musicType)")]
    [SerializeField]
    private AudioSoundData _musicClip;

    [ShowIf("@_useRandomMultiple == false && _category == SoundCategory.Effects")]
    [ValueDropdown("@_library.GetClipsForEffect(_effectType)")]
    [SerializeField]
    private AudioSoundData _effectClip;

    [ShowIf("@_useRandomMultiple == false && _category == SoundCategory.Mobs")]
    [ValueDropdown("@_library.GetClipsForMob(_mobType)")]
    [SerializeField]
    private AudioSoundData _mobClip;

    // Random-multi selections (shown when using random multi)
    [ShowIf("@_useRandomMultiple && _category == SoundCategory.Skills")]
    [ListDrawerSettings(DraggableItems = true, DefaultExpandedState = true)]
    [SerializeField]
    private List<AudioSoundData> _skillRandomClips = new List<AudioSoundData>();

    [ShowIf("@_useRandomMultiple && _category == SoundCategory.Music")]
    [ListDrawerSettings(DraggableItems = true, DefaultExpandedState = true)]
    [SerializeField]
    private List<AudioSoundData> _musicRandomClips = new List<AudioSoundData>();

    [ShowIf("@_useRandomMultiple && _category == SoundCategory.Effects")]
    [ListDrawerSettings(DraggableItems = true, DefaultExpandedState = true)]
    [SerializeField]
    private List<AudioSoundData> _effectRandomClips = new List<AudioSoundData>();

    [ShowIf("@_useRandomMultiple && _category == SoundCategory.Mobs")]
    [ListDrawerSettings(DraggableItems = true, DefaultExpandedState = true)]
    [SerializeField]
    private List<AudioSoundData> _mobRandomClips = new List<AudioSoundData>();

    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    [Title("Play Settings")]
    [PropertySpace(10, 10)]
    [EnumToggleButtons]
    [SerializeField]
    private PlayMode _playMode = PlayMode.OnEnable;

    [ShowIf("@_playMode == PlayMode.Manual")]
    [Button("Play Sound")]
    public void PlaySoundNow()
    {
        if (_enableDelay)
            ActionScheduler.RunAfterDelay(_delay, PlayClip);
        else
            PlayClip();
    }

    [SerializeField]
    private bool _enableDelay;

    [ShowIf("@_enableDelay")]
    [Indent]
    [SerializeField]
    private float _delay = 0.5f;

    private void OnEnable()
    {
        if (_playMode != PlayMode.OnEnable)
            return;

        if (_enableDelay)
            ActionScheduler.RunAfterDelay(_delay, PlayClip);
        else
            PlayClip();
    }

    // private IEnumerator DelayedPlay()
    // {
    //     yield return new WaitForSeconds(_delay);
    //     PlayClip();
    // }

    private void PlayClip()
    {
        AudioSoundData clipToPlay = null;

        if (!_useRandomMultiple)
        {
            clipToPlay = _category switch
            {
                SoundCategory.Skills => _skillClip,
                SoundCategory.Music => _musicClip,
                SoundCategory.Effects => _effectClip,
                SoundCategory.Mobs => _mobClip,
                _ => null
            };
        }
        else
        {
            var list = _category switch
            {
                SoundCategory.Skills => _skillRandomClips,
                SoundCategory.Music => _musicRandomClips,
                SoundCategory.Effects => _effectRandomClips,
                SoundCategory.Mobs => _mobRandomClips,
                _ => null
            };
            if (list != null && list.Count > 0)
                clipToPlay = list[Random.Range(0, list.Count)];
        }

        if (clipToPlay != null && ManagerSound.Instance != null)
        {
            new SoundBuilder(ManagerSound.Instance)
                .SetSoundData(clipToPlay)
                .SetPosition(transform.position)
                .Play();
        }
    }

    // Editor button to select the library asset
#if UNITY_EDITOR
    [Title("Move To Sound Library")]
    [PropertySpace(10, 0)]
    [Button("Ping Sound Library")]
    private void PingSoundLibrary()
    {
        if (_library != null)
        {
            //Selection.activeObject = _library;
            EditorGUIUtility.PingObject(_library);
        }
    }

    // Ping single clip when appropriate
    [ShowIf("@_useRandomMultiple == false && _category == SoundCategory.Skills && _skillClip != null")]
    [PropertySpace(0, 0)]
    [Button("Ping Skill Clip")]
    private void PingSkillClip()
    {
        //Selection.activeObject = _skillClip;
        EditorGUIUtility.PingObject(_skillClip);
    }

    [ShowIf("@_useRandomMultiple == false && _category == SoundCategory.Music && _musicClip != null")]
    [PropertySpace(0, 0)]
    [Button("Ping Music Clip")]
    private void PingMusicClip()
    {
        //Selection.activeObject = _musicClip;
        EditorGUIUtility.PingObject(_musicClip);
    }

    [ShowIf("@_useRandomMultiple == false && _category == SoundCategory.Effects && _effectClip != null")]
    [PropertySpace(0, 0)]
    [Button("Ping Effect Clip")]
    private void PingEffectClip()
    {
        //Selection.activeObject = _effectClip;
        EditorGUIUtility.PingObject(_effectClip);
    }

    [ShowIf("@_useRandomMultiple == false && _category == SoundCategory.Mobs && _mobClip != null")]
    [PropertySpace(0, 0)]
    [Button("Ping Mob Clip")]
    private void PingMobClip()
    {
        //Selection.activeObject = _mobClip;
        EditorGUIUtility.PingObject(_mobClip);
    }


#endif
}
