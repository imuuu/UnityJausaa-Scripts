// using UnityEngine;
// using Sirenix.OdinInspector.Editor;
// using UnityEditor;
// using Sirenix.Utilities.Editor;
// using System.Linq;
// using Sirenix.Utilities;

// public class MobEditorWindow : OdinMenuEditorWindow
// {
//     [MenuItem("Tools/Odin/Mob Editor")]
//     private static void OpenWindow()
//     {
//         var window = GetWindow<MobEditorWindow>("Mob Editor");
//         window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 600);
//     }

//     protected override OdinMenuTree BuildMenuTree()
//     {
//         var tree = new OdinMenuTree();
//         tree.Config.DrawSearchToolbar = true;
//         tree.DefaultMenuStyle.IconSize = 28;

//         tree.AddAllAssetsAtPath("Mobs", "Assets/Game/Mobs", typeof(MobData), true, true)
//             .ForEach(AddDragHandles);

//         return tree;
//     }

//     private void AddDragHandles(OdinMenuItem menuItem)
//     {
//         if (menuItem.Value is MobData)
//         {
//             menuItem.OnDrawItem += x => DragAndDropUtilities.DragZone(menuItem.Rect, menuItem.Value, false, false);
//         }
//     }

//     protected override void OnBeginDrawEditors()
//     {
//         var selected = this.MenuTree.Selection.FirstOrDefault();
//         var toolbarHeight = this.MenuTree.Config.SearchToolbarHeight;

//         SirenixEditorGUI.BeginHorizontalToolbar(toolbarHeight);
//         {
//             if (selected != null)
//                 GUILayout.Label(selected.Name);

//             if (SirenixEditorGUI.ToolbarButton(new GUIContent("Create Mob")))
//             {
//                 string path = EditorUtility.SaveFilePanelInProject(
//                     "Create Mob Data", "NewMobData.asset", "asset", "Choose folder to save new MobData");
//                 if (!string.IsNullOrEmpty(path))
//                 {
//                     var asset = CreateInstance<MobData>();
//                     AssetDatabase.CreateAsset(asset, path);
//                     AssetDatabase.SaveAssets();
//                     AssetDatabase.Refresh();
//                     this.TrySelectMenuItemWithObject(asset);
//                 }
//             }
//         }
//         SirenixEditorGUI.EndHorizontalToolbar();
//     }
// }
