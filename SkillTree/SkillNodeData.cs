using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SkillNodeData
{
    public string id;
    public SkillNodeType type;
    public Vector2 position;
    public List<ConnectionData> outgoingConnections = new List<ConnectionData>();

    // The actual effect — e.g. what Modifier(s) this node grants.
    // You can serialise your existing Modifier here:
    public List<Game.StatSystem.Modifier> modifiers = new List<Game.StatSystem.Modifier>();
}
