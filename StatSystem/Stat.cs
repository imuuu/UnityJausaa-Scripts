using System.Collections.Generic;
using UnityEngine;
namespace Game.StatSystem
{
    [System.Serializable]
    public class Stat
    {
        [SerializeField] private List<STAT_TYPE> _effectByTags;
        [SerializeField] private float _baseValue;
        private float _theValue;

        private List<Modifier> _addedModifiers;

        private bool _dirty = true;
        private bool _initialized = false;
        public Stat()
        {
            _baseValue = 0f;
            _theValue = 0f;
            _addedModifiers = new List<Modifier>();
        }

        public Stat(float baseValue, List<STAT_TYPE> effectByTags = null)
        {
            _baseValue = baseValue;
            _theValue = baseValue;
            _addedModifiers = new List<Modifier>();
            _effectByTags = effectByTags ?? new List<STAT_TYPE>();
        }
        public Stat(float baseValue, STAT_TYPE tag)
        {
            _baseValue = baseValue;
            _theValue = baseValue;
            _addedModifiers = new List<Modifier>();
            _effectByTags = new List<STAT_TYPE> { tag };
        }

        // public void AddStatValues(Stat stat)
        // {
        //     if (stat == null) return;

        //     if (_effectByTags != stat._effectByTags)
        //     {
        //         Debug.LogError($"Cannot add stat values from {stat} to {this} because their tags do not match.");
        //         return;
        //     }
        //     _baseValue += stat._baseValue;

        //     for (int i = _addedModifiers.Count - 1; i >= 0; i--)
        //     {
        //         Modifier mainMod = _addedModifiers[i];
        //         float mainModValue = mainMod.GetValue();
        //         for (int l = stat._addedModifiers.Count - 1; l >= 0; l--)
        //         {
        //             Modifier newMod = stat._addedModifiers[l];
        //             if (newMod.IsSameKind(mainMod))
        //             {
        //                 //maybe add here
        //             }
        //         }
        //     }
        // }

        public void AddEffectByTag(STAT_TYPE tag)
        {
            if (_effectByTags == null)
            {
                _effectByTags = new List<STAT_TYPE>();
            }

            if (!_effectByTags.Contains(tag))
            {
                _effectByTags.Add(tag);
            }
        }

        public void AddModifier(Modifier mod)
        {
            if (_addedModifiers == null)
            {
                _addedModifiers = new List<Modifier>();
            }

            if (!IsValidModifier(mod))
            {
                Debug.LogError($"Invalid modifier: {mod.GetTYPE()} for stat: {this}");
                return;
            }
            _addedModifiers.Add(mod);
            _dirty = true;
        }

        public Modifier GetModifierByID(int id)
        {
            if (_addedModifiers == null || _addedModifiers.Count == 0) return null;

            foreach (Modifier mod in _addedModifiers)
            {
                if (mod.ID == id)
                {
                    return mod;
                }
            }
            return null;
        }

        public void AddOrReplaceModifier(Modifier mod)
        {
            if (_addedModifiers == null)
            {
                _addedModifiers = new List<Modifier>();
            }

            if (mod == null || !IsValidModifier(mod))
            {
                Debug.LogError($"Invalid modifier: {mod} for stat: {this}");
                return;
            }

            Modifier existingMod = GetModifierByID(mod.ID);
            if (existingMod != null)
            {
                _addedModifiers.Remove(existingMod);
            }
            _addedModifiers.Add(mod);
            _dirty = true;
        }

        public bool IsValidModifier(Modifier mod)
        {
            for (int i = 0; i < _effectByTags.Count; i++)
            {
                if (_effectByTags[i] == mod.GetTarget())
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasModifiers()
        {
            return _addedModifiers.Count > 0;
        }

        public float GetBaseValue()
        {
            return _baseValue;
        }

        public float GetValue()
        {
            if (_dirty || !_initialized)
            {
                _initialized = true;
                _theValue = Calculate(_effectByTags);
                _dirty = false;
            }

            return _theValue;
        }

        public void SetBaseValue(float value)
        {
            _baseValue = value;
            _theValue = value;
            _dirty = true;
        }

        public int GetValueInt()
        {
            return Mathf.FloorToInt(GetValue());
        }

        public List<STAT_TYPE> GetTags()
        {
            return _effectByTags;
        }

        public float Calculate(List<STAT_TYPE> skillTags)
        {
            if (_addedModifiers == null || _addedModifiers.Count == 0)
            {
                return _baseValue;
            }

            float flat = 0f;
            float increased = 0f;
            float more = 0f;

            foreach (Modifier mod in _addedModifiers)
            {
                if (_effectByTags.Count == 0 || _effectByTags.Exists(tag => skillTags.Contains(tag)))
                {
                    switch (mod.GetTYPE())
                    {
                        case MODIFIER_TYPE.FLAT:
                            flat += mod.GetValue();
                            break;
                        case MODIFIER_TYPE.INCREASE:
                            increased += mod.GetValue();
                            break;
                        case MODIFIER_TYPE.MORE:
                            more *= (1f + mod.GetValue());
                            break;
                    }
                }
            }

            // 1. Base + Flat modifiers.
            // 2. Apply increased percentages.
            // 3. Apply multiplicative "more" modifiers.

            //Debug.Log($" CALCULATION ==== Base: {_baseValue}, Flat: {flat}, Increased: {increased}, More: {more}");
            return (_baseValue + flat) * (increased == 0 ? 1 : (1f + increased / 100)) * (more == 0 ? 1 : (1f + more / 100));
        }

        public void ClearModifiers()
        {
            if (_addedModifiers != null) _addedModifiers.Clear();
            _dirty = true;
        }

        public Modifier CreateEmptyModifier(MODIFIER_TYPE type, float value = 0f)
        {
            Modifier mod = new Modifier(_effectByTags[0],type, value);
            
            return mod;
        }
    }
}