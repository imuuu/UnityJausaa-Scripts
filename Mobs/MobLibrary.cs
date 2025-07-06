
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Game.Mobs
{
    [CreateAssetMenu(fileName = "MobLibrary", menuName = "Game/Mobs/Mob Library")]
    public class MobLibrary : SerializedScriptableObject
    {
        public static MobLibrary Instance { get; private set; }

        private void OnEnable()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning($"There are multiple MobLibrary instances! Using {name}.");
            Instance = this;
        }

        [Title("Mob Prefabs")]
        [ListDrawerSettings(ShowFoldout = true)]
        [SerializeField]
        private List<MobData> _mobs = new();

        public MobData GetMobData(GameObject prefab)
        {
            if (prefab == null)
                return null;

            foreach (MobData mob in _mobs)
            {
                if (mob.Prefab == prefab)
                    return mob;
            }

            Debug.LogWarning($"MobData not found for prefab: {prefab.name}");
            return null;
        }

        /// <summary>
        /// Returns true if this MobData is already in the library.
        /// </summary>
        public bool Contains(MobData data)
        {
            return data != null && _mobs.Contains(data);
        }

        /// <summary>
        /// Adds the given MobData to the library if not already present.
        /// </summary>
        public void AddMob(MobData data)
        {
            if (data == null || _mobs.Contains(data))
                return;

            _mobs.Add(data);
        }

        public MobData GetMobDataByType(MOB_TYPE mobType)
        {
            foreach (MobData mob in _mobs)
            {
                if (mob.MobType == mobType)
                    return mob;
            }

            Debug.LogWarning($"MobData not found for type: {mobType}");
            return null;
        }

        public List<MobData> GetMobDatas()
        {
            if (_mobs == null || _mobs.Count == 0)
            {
                Debug.LogWarning("MobLibrary is empty or not initialized.");
                return new List<MobData>();
            }
            return new List<MobData>(_mobs);
        }
    }
}
