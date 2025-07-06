using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Game.PoolSystem
{
    public class PoolMonoOptions : SerializedMonoBehaviour
    {   
        [Title("Add Options")]
        [SerializeReference]
        [ListDrawerSettings(DraggableItems = true, DefaultExpandedState = true)]
        public List<OptionAddition> OptionAdditions = new List<OptionAddition>();

        public IEnumerable<Type> GetFilteredTypeList()
        {
            return typeof(OptionAddition).Assembly.GetTypes()
                .Where(t => !t.IsAbstract)
                .Where(t => typeof(OptionAddition).IsAssignableFrom(t));
        }

        public List<OptionAddition> GetOptionAdditions()
        {
            return OptionAdditions;
        }
    }
}
