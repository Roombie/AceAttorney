using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class DialogueNodeData
{
    public string guid = Guid.NewGuid().ToString();
    public string nodeId;
    public Vector2 position;
    public string[] outputGUIDs;

    public DialogueNodeType nodeType;

    // Dialogue nodes
    public string textId;

    // Choice nodes
    public List<string> choiceIds = new();

    // Condition nodes
    public string conditionLabelTrue;
    public string conditionLabelFalse;
}