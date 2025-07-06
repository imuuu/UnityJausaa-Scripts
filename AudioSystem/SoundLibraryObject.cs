using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Game.SkillSystem;
using Game.AudioSystem;

[CreateAssetMenu(menuName = "Audio/Sound Library")]
public class SoundLibraryObject : ScriptableObject
{
    public static SoundLibraryObject Instance { get; private set; }

    #region Extra Classes
    [Serializable]
    public class SkillEntry
    {
        public SKILL_NAME Skill;
        public List<AudioSoundData> Clips = new List<AudioSoundData>();
    }
    [Serializable]
    public class MusicEntry
    {
        public MUSIC_TYPE Music;
        public List<AudioSoundData> Clips = new List<AudioSoundData>();
    }
    [Serializable]
    public class EffectEntry
    {
        public EFFECT_TYPE Effect;
        public List<AudioSoundData> Clips = new List<AudioSoundData>();
    }
    [Serializable]
    public class MobEntry
    {
        public MOB_TYPE Mob;
        public List<AudioSoundData> Clips = new List<AudioSoundData>();
    }
    #endregion

    [TabGroup("Sounds", "Skills")][SerializeField] private List<SkillEntry> _skillSounds = new();
    [TabGroup("Sounds", "Music")][SerializeField] private List<MusicEntry> _musicSounds = new();
    [TabGroup("Sounds", "Effects")][SerializeField] private List<EffectEntry> _effectSounds = new();
    [TabGroup("Sounds", "Mobs")][SerializeField] private List<MobEntry> _mobSounds = new();

    private Dictionary<SKILL_NAME, List<AudioSoundData>> _skillCache;
    private Dictionary<MUSIC_TYPE, List<AudioSoundData>> _musicCache;
    private Dictionary<EFFECT_TYPE, List<AudioSoundData>> _effectCache;
    private Dictionary<MOB_TYPE, List<AudioSoundData>> _mobCache;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning($"There are multiple SoundLibraryObjects in your project! Using {name}.");
        Instance = this;
        BuildCaches();
    }

    private void BuildCaches()
    {
        _skillCache = _skillSounds.ToDictionary(e => e.Skill, e => e.Clips);
        _musicCache = _musicSounds.ToDictionary(e => e.Music, e => e.Clips);
        _effectCache = _effectSounds.ToDictionary(e => e.Effect, e => e.Clips);
        _mobCache = _mobSounds.ToDictionary(e => e.Mob, e => e.Clips);
    }

    // These feed Odin’s ValueDropdown
    public IEnumerable<ValueDropdownItem<AudioSoundData>> GetClipsForSkill(SKILL_NAME s)
        => _skillCache.TryGetValue(s, out var list)
            ? list.Select(c => new ValueDropdownItem<AudioSoundData>(c.name, c))
            : Enumerable.Empty<ValueDropdownItem<AudioSoundData>>();

    public IEnumerable<ValueDropdownItem<AudioSoundData>> GetClipsForMusic(MUSIC_TYPE m)
        => _musicCache.TryGetValue(m, out var list)
            ? list.Select(c => new ValueDropdownItem<AudioSoundData>(c.name, c))
            : Enumerable.Empty<ValueDropdownItem<AudioSoundData>>();

    public IEnumerable<ValueDropdownItem<AudioSoundData>> GetClipsForEffect(EFFECT_TYPE e)
        => _effectCache.TryGetValue(e, out var list)
            ? list.Select(c => new ValueDropdownItem<AudioSoundData>(c.name, c))
            : Enumerable.Empty<ValueDropdownItem<AudioSoundData>>();

    public IEnumerable<ValueDropdownItem<AudioSoundData>> GetClipsForMob(MOB_TYPE m)
        => _mobCache.TryGetValue(m, out var list)
            ? list.Select(c => new ValueDropdownItem<AudioSoundData>(c.name, c))
            : Enumerable.Empty<ValueDropdownItem<AudioSoundData>>();

    /// <summary>
    /// Adds the given AudioSoundData to the appropriate category/sub-entry in the library.
    /// If an entry for its subcategory doesn't exist, creates one.
    /// </summary>
    public void AddSound(AudioSoundData data)
    {
        if (data == null) return;

        switch (data.Category)
        {
            case SoundCategory.Skills:
                AddToEntry(_skillSounds, data.SkillType, data);
                break;
            case SoundCategory.Music:
                AddToEntry(_musicSounds, data.MusicType, data);
                break;
            case SoundCategory.Effects:
                AddToEntry(_effectSounds, data.EffectType, data);
                break;
            case SoundCategory.Mobs:
                AddToEntry(_mobSounds, data.MobType, data);
                break;
        }
        BuildCaches();
    }

    /// <summary>
    /// Checks if the library already contains the given AudioSoundData.
    /// </summary>
    public bool Contains(AudioSoundData data)
    {
        if (data == null) return false;

        return data.Category switch
        {
            SoundCategory.Skills => _skillSounds.Any(e => e.Skill == data.SkillType && e.Clips.Contains(data)),
            SoundCategory.Music => _musicSounds.Any(e => e.Music == data.MusicType && e.Clips.Contains(data)),
            SoundCategory.Effects => _effectSounds.Any(e => e.Effect == data.EffectType && e.Clips.Contains(data)),
            SoundCategory.Mobs => _mobSounds.Any(e => e.Mob == data.MobType && e.Clips.Contains(data)),
            _ => false
        };
    }

    // Helper to add data to a generic entry list
    private void AddToEntry<TCategory, TEntry>(List<TEntry> entries, TCategory key, AudioSoundData data)
        where TEntry : class
    {
        // Using reflection to handle different entry types
        var entry = entries.Cast<object>().FirstOrDefault(e =>
        {
            var categoryProp = e.GetType().GetFields().FirstOrDefault(f => f.FieldType == typeof(TCategory));
            if (categoryProp == null) return false;
            return EqualityComparer<TCategory>.Default.Equals((TCategory)categoryProp.GetValue(e), key);
        });
        if (entry == null)
        {
            // Create a new entry of the same type
            entry = Activator.CreateInstance(typeof(TEntry)) as TEntry;
            var categoryProp = entry.GetType().GetFields().First(f => f.FieldType == typeof(TCategory));
            categoryProp.SetValue(entry, key);
            var clipsProp = entry.GetType().GetFields().First(f => f.FieldType == typeof(List<AudioSoundData>));
            clipsProp.SetValue(entry, new List<AudioSoundData> { data });
            entries.Add((TEntry)entry);
        }
        else
        {
            var clipsProp = entry.GetType().GetFields().First(f => f.FieldType == typeof(List<AudioSoundData>));
            var list = clipsProp.GetValue(entry) as List<AudioSoundData>;
            if (!list.Contains(data)) list.Add(data);
        }
    }
}
