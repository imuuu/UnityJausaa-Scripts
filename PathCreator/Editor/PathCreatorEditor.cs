#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.PathSystem
{
[CustomEditor(typeof(PathCreator))]
public class PathCreatorEditor : UnityEditor.Editor
{
    PathCreator creator;
    const float pickSize = 0.1f;

    void OnEnable()
    {
        creator = (PathCreator)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();
        if (GUILayout.Button("Clear Points"))
        {
            Undo.RecordObject(creator, "Clear Points");
            creator.ClearPoints();
        }
    }

    void OnSceneGUI()
    {
        if (!creator.IsEditable) return;

        Event guiEvent = Event.current;
        for (int i = 0; i < creator.NumPoints; i++)
        {
            Vector3 worldPoint = creator.GetPoint(i);
            float size = HandleUtility.GetHandleSize(worldPoint) * creator.GetGizmoSize();
            if (Handles.Button(worldPoint, Quaternion.identity, size, size + pickSize, Handles.DotHandleCap))
            {
                Selection.activeTransform = creator.transform;
                EditorGUIUtility.editingTextField = false;
            }
            EditorGUI.BeginChangeCheck();
            Vector3 newWorld = Handles.PositionHandle(worldPoint, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(creator, "Move Point");
                Vector3 delta = newWorld - worldPoint;
                MovePoint(i, delta);
            }
        }

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        if (guiEvent.type == EventType.MouseDown && guiEvent.shift && guiEvent.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Undo.RecordObject(creator, "Add Segment");
                if (creator.GetDrawMode() == DrawMode.Bezier) creator.AddSegment(hit.point);
                else creator.AddPoint(hit.point);
            }
            guiEvent.Use();
        }
    }

    // void MovePoint(int i, Vector3 delta)
    // {
    //     Undo.RecordObject(creator, "Move Point");
    //     Vector3 localDelta = creator.useTransform && creator.applyRotation
    //         ? creator.transform.InverseTransformVector(delta)
    //         : delta;
    //     if (creator.space == PathSpace.XZ)
    //         localDelta = new Vector3(localDelta.x, localDelta.z, 0);
    //     else if (creator.space == PathSpace.XY)
    //         localDelta = new Vector3(localDelta.x, localDelta.y, 0);
    //     creator.points[i] += localDelta;
    //     creator.EnforceMode(i);
    // }

    void MovePoint(int i, Vector3 delta)
    {
        Undo.RecordObject(creator, "Move Point");
        Vector3 localDelta = creator.IsUseTransform() && creator.IsApplyRotation()
            ? creator.transform.InverseTransformVector(delta)
            : delta;
        if (creator.GetPathSpace() == PathSpace.XZ)
            localDelta = new Vector3(localDelta.x, localDelta.z, 0);
        else if (creator.GetPathSpace() == PathSpace.XY)
            localDelta = new Vector3(localDelta.x, localDelta.y, 0);

        creator.points[i] += localDelta;
        //creator.EnforceMode(i); // ← remove or comment this line
    }
}
#endif
}