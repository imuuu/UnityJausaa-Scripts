using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace Game.Mobs
{
    public class MobDataController : MonoBehaviour
    {
        [SerializeField] private MobData _mobData;

        private IHealth _health;
        private IMovement _movement;
        private IDamageDealer _damageDealer;

        private void Awake()
        {
            _health = GetComponent<IHealth>();
            _movement = GetComponent<IMovement>();
            _damageDealer = GetComponent<IDamageDealer>();
        }

        public MobData GetMobData() 
        {
            return _mobData;
        }

        public void OnEnable()
        {
            _health.SetHealth(_mobData.GetHealthWithRoundTime());
            _movement.SetSpeed(_mobData.MovementSpeed);
            _movement.SetRotationSpeed(_mobData.RotationSpeed);
            _damageDealer.SetDamage(_mobData.Damage);
        }

#if UNITY_EDITOR
        [PropertySpace(10)]
        [Button("Create Mob Data"), GUIColor(0.2f, 0.8f, 0.2f)]
        [ShowIf("@_mobData == null")]
        private void CreateMobData()
        {
            const string dir = "Assets/Game/ScriptableObjects/Mobs";
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Debug.LogWarning($"Directory '{dir}' not found. Creating it.");
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Game/ScriptableObjects/Mobs"));
                AssetDatabase.Refresh();
            }

            string assetName = $"MobData_{gameObject.name}.asset";
            string path = $"{dir}/{assetName}";

            // Create and save the asset
            var data = ScriptableObject.CreateInstance<MobData>();
            AssetDatabase.CreateAsset(data, path);
            // Assign the prefab reference
            data.Prefab = gameObject;
            // Mark new asset dirty so changes save
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Assign to controller
            _mobData = data;
            EditorUtility.SetDirty(this);

            // Add to MobLibrary
            var library = MobLibrary.Instance;
            if (library != null)
            {
                library.AddMob(data);
                EditorUtility.SetDirty(library);
                Debug.Log($"Created '{data.name}', set Prefab, and added to Mob Library.");
            }
            else
            {
                Debug.LogWarning("No MobLibrary instance found in project. MobData created but not added to library.");
            }
        }
#endif
    }
}