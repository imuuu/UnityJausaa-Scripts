// InspectorGrid.cs
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class InspectorGrid
{
    private const int GRID_SIZE = 15;
    [TableMatrix(
        HorizontalTitle = "Custom Cell Drawing",
        DrawElementMethod = "DrawColoredEnumElement",
        ResizableColumns = false,
        RowHeight = GRID_SIZE+1)]
    public bool[,] CustomCellDrawing;

    // default / selected
    private static readonly Color _defaultColor = new Color(0, 0, 0, 0.5f);
    private static readonly Color _selectedColor = new Color(0.1f, 0.8f, 0.2f);

    /// <summary>
    /// Override this to change your “center” color.
    /// </summary>
    protected virtual Color GetCenterColor()
    {
        // fallback if subclass doesn’t override:
        return new Color(0.5f, 0f, 0.5f, 0.5f);
    }

    /// <summary>
    /// signature must match: (Rect, bool, int row, int column)
    /// </summary>
    protected virtual bool DrawColoredEnumElement(Rect rect, bool value, int row, int column)
    {
        // toggle on click
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            value = !value;
            GUI.changed = true;
            Event.current.Use();
        }

        // detect the “true” center cell
        int rows = CustomCellDrawing.GetLength(0);
        int cols = CustomCellDrawing.GetLength(1);
        bool isCenter = row == rows / 2 && column == cols / 2;

        // pick color
        Color c = isCenter
            ? GetCenterColor()
            : (value ? _selectedColor : _defaultColor);

#if UNITY_EDITOR
        EditorGUI.DrawRect(rect.Padding(1), c);
#endif

        return value;
    }

    [OnInspectorInit]
    protected virtual void CreateData()
    {
        if (CustomCellDrawing != null
         && CustomCellDrawing.GetLength(0) == GRID_SIZE
         && CustomCellDrawing.GetLength(1) == GRID_SIZE)
            return;

        CustomCellDrawing = new bool[GRID_SIZE, GRID_SIZE];
    }
}
