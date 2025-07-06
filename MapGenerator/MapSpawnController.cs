using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Game.MapGenerator
{
    public class MapSpawnController : MapSpawnItemBase
    {
        [FoldoutGroup("Spawn Items")] 
        [ListDrawerSettings(DefaultExpandedState = true)]
        [PropertySpace(15, 15)]
        [GUIColor(0.3f, 0.8f, 0.8f, 1f)]
        [Tooltip("List of child spawn items (can be controllers or object settings).")]
        public List<MapSpawnItemBase> spawnItems = new List<MapSpawnItemBase>();

        [FoldoutGroup("Spawn Settings")]
        [Tooltip("How many spawn items to enable (from the list above).")]
        [Min(1)]
        public int ItemsToEnable = 1;

        [FoldoutGroup("Spawn Settings")]
        [Tooltip("If true, the same spawn item will not be used more than once.")]
        public bool UniqueItems = true;

        /// <summary>
        /// Scans all child objects (recursively) for spawn items and populates the spawnItems list.
        /// </summary>
        [BoxGroup("Buttons")]
        [PropertySpace(10, 10)]
        [GUIColor("#6f8ff7")]
        [Button("Scan This Child Spawn Items", ButtonSizes.Large)]
        public void ScanOnlyThisChildSpawnItems()
        {
            ScanAllChildItems(false);
        }

        [BoxGroup("Buttons")]
        [PropertySpace(10, 10)]
        [GUIColor("#aa52f7")]
        [Button("Scan Child and Child's Spawn Items", ButtonSizes.Large)]
        public void ScanAllChildSpawnItems()
        {
            ScanAllChildItems(true);
        }

        public void ScanAllChildItems(bool recursive = true)
        {
            spawnItems.Clear();

            List<MapSpawnController> controllers = new List<MapSpawnController>();
            List<MapSpawnItemBase> standaloneItems = new List<MapSpawnItemBase>();

            ManagerMapDesignPlates.TraverseChildren(transform, ref controllers, ref standaloneItems);

            spawnItems.AddRange(standaloneItems);
            spawnItems.AddRange(controllers);

            Debug.Log($"Scanned {spawnItems.Count} child spawn items for {name}.");

            if (!recursive) return;

            foreach (var controller in controllers)
            {
                controller.ScanAllChildItems(recursive);
            }
        }



        /// <summary>
        /// Spawns (enables) items from the spawnItems list using weighted random selection.
        /// Non-selected items are automatically disabled.
        /// </summary>
        [BoxGroup("Buttons")]
        [PropertySpace(8, 8)]
        [GUIColor("#90d5e8")]
        [Button("Spawn Items", ButtonSizes.Medium)]
        public void SpawnItems()
        {
            if (spawnItems.Count == 0)
            {
                Debug.LogWarning("No spawn items available!");
                return;
            }

            // Build a working list of available items.
            List<MapSpawnItemBase> availableItems = new List<MapSpawnItemBase>();
            foreach (var item in spawnItems)
            {
                if (item.isEnabled)
                    availableItems.Add(item);
            }
            if (availableItems.Count == 0)
            {
                availableItems.AddRange(spawnItems);
            }

            List<MapSpawnItemBase> selectedItems = new List<MapSpawnItemBase>();

            // Select the required number of items.
            for (int i = 0; i < ItemsToEnable; i++)
            {
                if (availableItems.Count == 0)
                    break;

                MapSpawnItemBase selected = GetRandomWeightedItem(availableItems);
                if (selected != null)
                {
                    selectedItems.Add(selected);
                    selected.TriggerSpawn();
                    selected.gameObject.SetActive(true);
                    if (UniqueItems)
                    {
                        availableItems.Remove(selected);
                    }
                }
            }

            // Disable any spawn items that were not selected.
            foreach (var item in spawnItems)
            {
                if (!selectedItems.Contains(item))
                {
                    item.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Picks one item from the provided list using weighted random selection.
        /// </summary>
        private MapSpawnItemBase GetRandomWeightedItem(List<MapSpawnItemBase> items)
        {
            int totalWeight = 0;
            foreach (var item in items)
            {
                totalWeight += item.weight;
            }
            int roll = Random.Range(0, totalWeight);
            foreach (var item in items)
            {
                if (roll < item.weight)
                {
                    return item;
                }
                roll -= item.weight;
            }
            return null;
        }

        public override void TriggerSpawn()
        {
            // For a controller, spawn items from our list.
            SpawnItems();

            // Recursively trigger spawn on any child controllers or standalone object settings.
            foreach (var item in spawnItems)
            {
                if (item.gameObject.activeSelf)
                {
                    item.TriggerSpawn();
                }
            }
        }
    }
}
