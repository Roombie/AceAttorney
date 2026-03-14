using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Runtime dialogue player. Attach to a GameObject, assign a DialogueContainer,
/// wire up the callbacks, then call StartDialogue().
///
/// Generic callbacks:
///   OnShowDialogue       (string text)
///   OnShowChoices        (List<string> choices)   -> call SelectChoice(index)
///   OnCheckCondition     (string key)             -> return bool
///   OnSelectBranch       (List<string> labels)    -> return int index
///   OnDialogueEnd        ()
///
/// Ace Attorney callbacks:
///   OnCheckEvidence      (string evidenceId)      -> return bool
///   OnCheckStatement     (string statementId)     -> return bool
///   OnCheckContradiction (string testimonyId, string evidenceId) -> return bool
///   OnCheckSceneSeen     (string sceneId)         -> return bool
///   OnCheckTalkedTo      (string characterId)     -> return bool
/// </summary>
public class DialogueRunner : MonoBehaviour
{
    [Header("Graph asset to play")]
    public DialogueContainer dialogueContainer;

    [Header("Language code (EN, ES, FR ...)")]
    public string language = "EN";

    // ── Generic callbacks ─────────────────────────────────────────────────────

    public Action<string>          OnShowDialogue;
    public Action<List<string>>    OnShowChoices;
    public Func<string, bool>      OnCheckCondition;
    public Func<List<string>, int> OnSelectBranch;
    public Action                  OnDialogueEnd;

    // ── Ace Attorney callbacks ────────────────────────────────────────────────

    public Func<string, bool>         OnCheckEvidence;
    public Func<string, bool>         OnCheckStatement;
    public Func<string, string, bool> OnCheckContradiction;
    public Func<string, bool>         OnCheckSceneSeen;
    public Func<string, bool>         OnCheckTalkedTo;

    // ── State ─────────────────────────────────────────────────────────────────

    private DialogueNodeData                      _current;
    private Dictionary<string, DialogueNodeData>  _nodeMap;
    private Dictionary<string, List<NodeLinkData>> _linkMap;
    private bool _waitingForChoice;

    // ── Public API ────────────────────────────────────────────────────────────

    public void StartDialogue()
    {
        if (dialogueContainer == null)
        {
            Debug.LogError("[DialogueRunner] No DialogueContainer assigned.");
            return;
        }

        BuildMaps();

        var startNode = dialogueContainer.DialogueNodeData
            .FirstOrDefault(n => n.nodeType == DialogueNodeType.Start);

        if (startNode == null)
        {
            Debug.LogError("[DialogueRunner] No Start node in graph.");
            return;
        }

        _waitingForChoice = false;
        GoTo(startNode);
    }

    /// <summary>Advance past a Dialogue node. No-op if waiting for a choice.</summary>
    public void Advance()
    {
        if (_waitingForChoice || _current == null) return;
        var link = GetLinks(_current.guid).FirstOrDefault();
        if (link != null) GoTo(GetNode(link.targetNodeGuid));
    }

    /// <summary>Resolve a Choice node by index.</summary>
    public void SelectChoice(int index)
    {
        if (!_waitingForChoice) return;
        var links = GetLinks(_current.guid);
        if (index < 0 || index >= links.Count)
        {
            Debug.LogWarning($"[DialogueRunner] Choice index {index} out of range.");
            return;
        }
        _waitingForChoice = false;
        GoTo(GetNode(links[index].targetNodeGuid));
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void GoTo(DialogueNodeData node)
    {
        if (node == null) { Debug.LogWarning("[DialogueRunner] Null node."); return; }
        _current = node;

        switch (node.nodeType)
        {
            case DialogueNodeType.Start:
                Advance();
                break;

            case DialogueNodeType.End:
                OnDialogueEnd?.Invoke();
                break;

            case DialogueNodeType.Dialogue:
                OnShowDialogue?.Invoke(DialogueLocalization.GetText(node.textId, language));
                break;

            case DialogueNodeType.Choice:
                _waitingForChoice = true;
                OnShowChoices?.Invoke(
                    node.choiceIds.Select(id => DialogueLocalization.GetText(id, language)).ToList());
                break;

            case DialogueNodeType.Condition:
                FollowTrueFalse(node, OnCheckCondition?.Invoke(node.conditionKey) ?? false);
                break;

            case DialogueNodeType.Branch:
                var links  = GetLinks(node.guid);
                int chosen = OnSelectBranch?.Invoke(node.branchLabels) ?? 0;
                if (chosen >= 0 && chosen < links.Count)
                    GoTo(GetNode(links[chosen].targetNodeGuid));
                break;

            // ── Ace Attorney ──────────────────────────────────────────────

            case DialogueNodeType.EvidenceCheck:
                FollowTrueFalse(node, OnCheckEvidence?.Invoke(node.evidenceId) ?? false);
                break;

            case DialogueNodeType.StatementPressed:
                FollowTrueFalse(node, OnCheckStatement?.Invoke(node.statementId) ?? false);
                break;

            case DialogueNodeType.TestimonyContradicted:
                FollowTrueFalse(node,
                    OnCheckContradiction?.Invoke(node.testimonyId, node.contradictEvidenceId) ?? false);
                break;

            case DialogueNodeType.SceneAlreadySeen:
                FollowTrueFalse(node, OnCheckSceneSeen?.Invoke(node.sceneId) ?? false);
                break;

            case DialogueNodeType.TalkedToCharacter:
                FollowTrueFalse(node, OnCheckTalkedTo?.Invoke(node.characterId) ?? false);
                break;
        }
    }

    private void FollowTrueFalse(DialogueNodeData node, bool result)
    {
        var links = GetLinks(node.guid);
        var link  = result
            ? links.FirstOrDefault(l => l.portName == "True")
            : links.FirstOrDefault(l => l.portName == "False");
        if (link != null) GoTo(GetNode(link.targetNodeGuid));
    }

    private void BuildMaps()
    {
        _nodeMap = dialogueContainer.DialogueNodeData.ToDictionary(n => n.guid);
        _linkMap = new Dictionary<string, List<NodeLinkData>>();
        foreach (var link in dialogueContainer.NodeLinks)
        {
            if (!_linkMap.ContainsKey(link.baseNodeGuid))
                _linkMap[link.baseNodeGuid] = new List<NodeLinkData>();
            _linkMap[link.baseNodeGuid].Add(link);
        }
    }

    private List<NodeLinkData> GetLinks(string guid)
        => _linkMap.TryGetValue(guid, out var l) ? l : new List<NodeLinkData>();

    private DialogueNodeData GetNode(string guid)
        => _nodeMap.TryGetValue(guid, out var n) ? n : null;
}
