using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class SkillNodeView : Node
{
    public SkillNodeData Data { get; private set; }
    public Port inputPort;

    public SkillNodeView(SkillNodeType type)
    {
        Data = new SkillNodeData
        {
            id = Guid.NewGuid().ToString(),
            type = type,
            position = Vector2.zero
        };
        Initialize();
    }

    public SkillNodeView(SkillNodeData data)
    {
        Data = data;
        Initialize();
        SetPosition(new Rect(data.position, Vector2.zero));
    }

    private void Initialize()
    {
        title = Data.type.ToString() + " Node";
        // Single input port:
        inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "In";
        inputPort.style.backgroundColor = Color.white;
        inputContainer.Add(inputPort);

        // Dynamic output ports per color:
        foreach (var color in new[] { Color.red, Color.green, Color.blue })
        {
            AddColoredOutput(color);
        }

        // Inspector: choose node‑type or edit modifiers
        var typeField = new EnumField(Data.type)
        {
            value = Data.type
        };
        typeField.RegisterValueChangedCallback(evt =>
        {
            Data.type = (SkillNodeType)evt.newValue;
            title = Data.type + " Node";
        });
        extensionContainer.Add(typeField);

        // Allow editing your Modifier list:
        var modList = new ObjectField("Modifiers")
        {
            objectType = typeof(Game.StatSystem.Modifier),
            allowSceneObjects = false,
            value = null
        };
        // You’d want a better UI here—this is just a placeholder.
        extensionContainer.Add(modList);

        RefreshExpandedState();
        RefreshPorts();
    }

    private void AddColoredOutput(Color c)
    {
        var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        port.portName = "→";
        port.portColor = c;
        // Later, when edges draw, GraphView will pick up portColor on the line.
        outputContainer.Add(port);
    }

    // Helper when loading:
    public Port GetOutputPortForColor(Color c)
    {
        return outputContainer.Children()
            .OfType<Port>()
            .First(p => p.portColor == c);
    }
}
