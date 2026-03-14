using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class DialogueNodeData
{
    public string guid = Guid.NewGuid().ToString();
    public string nodeId;
    public Vector2 position;

    public DialogueNodeType nodeType;

    // Dialogue node
    public string textId;

    // Choice node
    public List<string> choiceIds = new();

    // Condition node
    public string conditionKey;

    // Branch node
    public List<string> branchLabels = new();

    // ── Ace Attorney fields ───────────────────────────────────────────────────

    // EvidenceCheck: which piece of evidence to check for
    public string evidenceId;

    // StatementPressed: which statement index or id was pressed
    public string statementId;

    // TestimonyContradicted: which testimony + which evidence contradicts it
    public string testimonyId;
    public string contradictEvidenceId;

    // SceneAlreadySeen: which scene key to check
    public string sceneId;

    // TalkedToCharacter: which character key to check
    public string characterId;
}
