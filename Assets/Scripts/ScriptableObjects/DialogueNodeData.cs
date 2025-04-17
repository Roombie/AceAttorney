 using UnityEngine;
using System;

[Serializable]
public class DialogueNodeData
{
    public string guid = Guid.NewGuid().ToString();
    public string nodeId;
    public string speaker;
    public string expression;
    public Vector2 position;
    public string[] outputGUIDs;
}