using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.StatSystem
{
    [System.Serializable]
    public class StatList
    {
        public List<Stat> _stats;

        public void Initialize(List<STAT_TYPE> tags)
        {
            if (_stats == null)
            {
                _stats = new List<Stat>();
            }

            foreach (STAT_TYPE tag in tags)
            {
                Stat stat = new Stat(0, tag);
                _stats.Add(stat);
            }
        }
        /// <summary>
        /// Returns the value of a stat by its tag. 
        /// If the stat does not exist, returns 0.
        /// </summary>
        public float GetValueOfStat(STAT_TYPE tag, float defaultValue = 0f)
        {
            foreach (Stat stat in _stats)
            {
                foreach (STAT_TYPE statTag in stat.GetTags())
                {
                    if (statTag == tag)
                    {
                        return stat.GetValue();
                    }
                }
            }
            return defaultValue;
        }

        public Stat AddStat(Stat stat)
        {
            foreach (Stat existingStat in _stats)
            {
                foreach (STAT_TYPE statTag in existingStat.GetTags())
                {
                    if (statTag == stat.GetTags()[0])
                    {
                        Debug.LogWarning("Stat already exists: " + stat.GetTags()[0]);
                        return existingStat;
                    }
                }
            }
            _stats.Add(stat);
            return stat;
        }

        public void SetStat(Stat stat)
        {
            if (_stats == null)
            {
                _stats = new List<Stat>();
            }

            for (int i = 0; i < _stats.Count; i++)
            {
                foreach (STAT_TYPE statTag in _stats[i].GetTags())
                {
                    if (statTag == stat.GetTags()[0])
                    {
                        _stats[i] = stat;
                        return;
                    }
                }
            }
            _stats.Add(stat);
        }

        public float GetBaseValue(STAT_TYPE tag)
        {
            foreach (Stat stat in _stats)
            {
                foreach (STAT_TYPE statTag in stat.GetTags())
                {
                    if (statTag == tag)
                    {
                        return stat.GetBaseValue();
                    }
                }
            }
            return 0f;
        }

        public Stat GetStat(STAT_TYPE tag)
        {
            foreach (Stat stat in _stats)
            {
                foreach (STAT_TYPE statTag in stat.GetTags())
                {
                    if (statTag == tag)
                    {
                        return stat;
                    }
                }
            }
            return null;
        }

        public bool TryGetStat(STAT_TYPE tag, out Stat stat)
        {
            foreach (Stat s in _stats)
            {
                foreach (STAT_TYPE statTag in s.GetTags())
                {
                    if (statTag == tag)
                    {
                        stat = s;
                        return true;
                    }
                }
            }
            stat = null;
            return false;
        }

        public void AddModifier(Modifier modifier)
        {
            foreach (Stat stat in _stats)
            {
                foreach (STAT_TYPE statTag in stat.GetTags())
                {
                    if (statTag == modifier.GetTarget())
                    {
                        stat.AddModifier(modifier);

                        //Debug.Log("|||||||||||| Current Stat: " + stat.GetValue() + " Base Stat: " + stat.GetBaseValue() + " Modifier: " + modifier.GetTYPE() + " Target: " + modifier.GetTarget());

                        // this makes possible to effect other stats with same tag
                        //return;
                    }
                }
            }
        }

        public void AddModifiers(List<Modifier> modifiers)
        {
            foreach (Modifier modifier in modifiers)
            {
                AddModifier(modifier);
            }
        }

        public void ClearModifiers()
        {
            foreach (Stat stat in _stats)
            {
                stat.ClearModifiers();
            }
        }

        public bool HasStat(STAT_TYPE type)
        {
            foreach (Stat stat in _stats)
            {
                foreach (STAT_TYPE statTag in stat.GetTags())
                {
                    if (statTag == type)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool HasStat(Stat stat)
        {
            foreach (var s in stat.GetTags())
            {
                if (HasStat(s))
                {
                    return true;
                }
            }

            return false;
        }
    }
}