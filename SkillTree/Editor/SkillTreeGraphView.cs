using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class SkillTreeGraphView : GraphView
{
    public SkillTreeGraphView()
    {
        Insert(0, new GridBackground());
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
    }

    // 1) Create a new node in the graph
    public void CreateNode(SkillNodeType type = SkillNodeType.Small)
    {
        var node = new SkillNodeView(type);
        AddElement(node);
    }

    // 2) Load from ScriptableObject
    public void LoadFromData(SkillTreeData tree)
    {
        graphViewChanged -= OnGraphViewChanged;
        DeleteElements(graphElements.ToList());
        foreach (var data in tree.nodes)
        {
            var node = new SkillNodeView(data);
            AddElement(node);
        }

        // Re-create edges
        foreach (var n in nodes.Cast<SkillNodeView>())
        {
            foreach (var conn in n.Data.outgoingConnections)
            {
                var target = nodes.Cast<SkillNodeView>().FirstOrDefault(x => x.Data.id == conn.toNodeId);
                if (target != null)
                {
                    var edge = n.GetOutputPortForColor(conn.lineColor)
                                .ConnectTo(target.inputPort);
                    edge.edgeControl.edgeColor = conn.lineColor;
                    AddElement(edge);
                }
            }
        }
        graphViewChanged += OnGraphViewChanged;
    }

    // 3) Save to ScriptableObject
    public void SaveToData(SkillTreeData tree)
    {
        tree.nodes.Clear();
        foreach (var node in nodes.Cast<SkillNodeView>())
        {
            // Update position
            node.Data.position = node.GetPosition().position;
            // Rebuild connection list
            node.Data.outgoingConnections = node
                .outputContainer
                .Children()
                .OfType<Port>()
                .SelectMany(p => p.connections.Select(e => new { Port = p, Edge = e }))
                .Select(x => new ConnectionData
                {
                    fromNodeId = node.Data.id,
                    toNodeId = ((SkillNodeView)x.Edge.input.node).Data.id,
                    lineColor = x.Port.portColor
                }).ToList();
            tree.nodes.Add(node.Data);
        }
    }

    // Keep data in sync when edges/nodes change
    private GraphViewChange OnGraphViewChanged(GraphViewChange changes)
    {
        // You can hook into create/delete here if needed.
        return changes;
    }
}
