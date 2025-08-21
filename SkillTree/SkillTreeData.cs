using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SkillTree/SkillTreeData")]
public class SkillTreeData : ScriptableObject
{
    public List<SkillNodeData> nodes = new List<SkillNodeData>();
}
