using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;


    /// <summary>
    /// Custom Odin drawer for LootTable<T> that displays its contents, weights, and probabilities.
    /// Place this script in an "Editor" folder.
    /// </summary>
    public class LootTableDrawer<T> : OdinValueDrawer<LootTable<T>> where T : UnityEngine.Object, IWeightedLoot
    {
        // Reflect into the private 'items' field
        private static readonly FieldInfo ItemsField = typeof(LootTable<T>)
            .GetField("items", BindingFlags.NonPublic | BindingFlags.Instance);

        protected override void DrawPropertyLayout(GUIContent label)
        {
            // Fetch the LootTable instance
            var table = this.ValueEntry.SmartValue;

            if (table == null)
            {
                SirenixEditorGUI.MessageBox("LootTable is null!", MessageType.Warning);
                return;
            }

            // Get the internal list and total weight
            var items = (List<T>)ItemsField.GetValue(table);
            float totalWeight = table.GetTotalWeight();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            // Iterate and draw each slot
            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];
                float weight = item?.GetWeight() ?? 0f;
                float chance = totalWeight > 0f ? weight / totalWeight : 0f;

                EditorGUILayout.BeginHorizontal();

                // Object field for the loot item (allows drag-and-drop)
                var selected = EditorGUILayout.ObjectField(item, typeof(T), false) as T;
                if (selected != item)
                {
                    items[i] = selected;
                }

                // Display weight and computed chance
                EditorGUILayout.LabelField($"Weight: {weight}", GUILayout.MaxWidth(100));
                EditorGUILayout.LabelField($"Chance: {chance:P1}", GUILayout.MaxWidth(100));

                // Remove button
                if (GUILayout.Button("Remove", GUILayout.MaxWidth(60)))
                {
                    items.RemoveAt(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            // Button to add an empty slot
            if (GUILayout.Button("Add Item"))
            {
                items.Add(null);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Total Weight: {totalWeight}", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
        }
    }

