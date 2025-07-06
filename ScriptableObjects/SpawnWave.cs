using System.Collections.Generic;

using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Holds data for a single wave of spawns.
/// </summary>
[CreateAssetMenu(fileName = "NewSpawnWave", menuName = "Game/Waves/SpawnWave")]
public class SpawnWave : SerializedScriptableObject
{
    [Header("Wave Settings")]
    [Tooltip("A name for your wave (for easy reference).")]
    public string WaveName = "Default Wave";

    [Tooltip("How long this wave should keep spawning mobs (in seconds).")]
    public float WaveDuration = 10f;

    [Tooltip("How many enemies can be active from this wave at once.")]
    public int TotalSpawns = 20;

    [Tooltip("How often (in seconds) this wave spawns new enemies.")]
    public AnimationCurve SpawnFrequency = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("Prefabs and Weights")]
    [Tooltip("A list of possible prefabs to spawn, each with an associated weight.")]
    public List<WeightedPrefab> WavePrefabs = new ();

    

    [System.Serializable]
    public class WeightedPrefab
    {
        public GameObject Prefab;
        [Tooltip("Higher weight = higher chance to spawn this prefab.")]
        public float Weight = 1f;

        public bool UseSpawnPattern = false;
        [ShowIf(nameof(UseSpawnPattern)), Indent]
        public SpawnPattern SpawnPattern;
    }

    [OdinSerialize][NonSerialized]public SpawnPattern _spawnPattern;


    [PropertyOrder(20)]
    [Button("Update Wave Duration (Asset Name)")]
    private void AddTimeToWaveSO()
    {
#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(this);

        // Remove existing "(number)" pattern from filename
        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        fileName = System.Text.RegularExpressions.Regex.Replace(fileName, @"\(\d+(\.\d+)?\)", "").Trim(); // Removes (123), (12.5), etc.

        string newName = fileName + "(" + WaveDuration + ")";
        AssetDatabase.RenameAsset(path, newName);
        AssetDatabase.SaveAssets();
#endif
    }


    /// <summary>
    /// Returns a prefab from wavePrefabs using weighted random selection.
    /// </summary>
    public WeightedPrefab GetRandomPrefab()
    {
        if (WavePrefabs == null || WavePrefabs.Count == 0) return null;

        // Sum all weights
        float totalWeight = 0f;
        foreach (var entry in WavePrefabs)
        {
            totalWeight += entry.Weight;
        }

        // Pick a random value
        float randomValue = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        // Find which prefab is chosen
        foreach (var entry in WavePrefabs)
        {
            cumulative += entry.Weight;
            if (randomValue <= cumulative)
            {
                return entry;
            }
        }

        // Fallback (should not happen if everything is set up right)
        return WavePrefabs[WavePrefabs.Count - 1];
    }

    public List<WeightedPrefab> GetPrefabs()
    {
        return WavePrefabs;
    }
   
}
