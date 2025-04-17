using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Graph")]
public class DialogueGraphData : ScriptableObject
{
    public List<DialogueNodeData> nodes = new();
}