using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class SkillTreeEditorWindow : EditorWindow
{
    [MenuItem("Window/Skill Tree Editor")]
    public static void OpenWindow() => GetWindow<SkillTreeEditorWindow>("Skill Tree");

    private SkillTreeGraphView _graphView;
    private SkillTreeData _currentTree;

    private void OnEnable()
    {
        ConstructGraphView();
        GenerateToolbar();
    }

    private void OnDisable()
    {
        rootVisualElement.Remove(_graphView);
    }

    private void ConstructGraphView()
    {
        _graphView = new SkillTreeGraphView
        {
            name = "Skill Tree Graph"
        };
        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }

    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        // Load dropdown
        var treeField = new ObjectField("Tree")
        {
            objectType = typeof(SkillTreeData),
            allowSceneObjects = false
        };
        treeField.RegisterValueChangedCallback(evt =>
        {
            _currentTree = evt.newValue as SkillTreeData;
            if (_currentTree != null)
                _graphView.LoadFromData(_currentTree);
        });
        toolbar.Add(treeField);

        // Save button
        var saveBtn = new Button(() =>
        {
            if (_currentTree == null)
            {
                Debug.LogWarning("Assign a SkillTreeData asset first.");
                return;
            }
            _graphView.SaveToData(_currentTree);
            EditorUtility.SetDirty(_currentTree);
            AssetDatabase.SaveAssets();
        })
        { text = "Save Tree" };
        toolbar.Add(saveBtn);

        // Add node button
        var addBtn = new Button(() => _graphView.CreateNode()) { text = "Add Node" };
        toolbar.Add(addBtn);

        rootVisualElement.Add(toolbar);
    }
}
