using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class GraphSaveUtility
{
    private DialogueGraphView _graphView;
    private DialogueContainer _cache;

    private List<Edge>          Edges  => _graphView.edges.ToList();
    private List<DialogueNode>  Nodes  => _graphView.nodes.ToList().Cast<DialogueNode>().ToList();
    private List<DialogueGroup> Groups => _graphView.graphElements.ToList().OfType<DialogueGroup>().ToList();

    public static GraphSaveUtility GetInstance(DialogueGraphView graphView)
        => new() { _graphView = graphView };

    // ── Save ──────────────────────────────────────────────────────────────────

    public void SaveGraph(string fileName, string scopeName = "")
    {
        var container      = ScriptableObject.CreateInstance<DialogueContainer>();
        container.scopeName = string.IsNullOrWhiteSpace(scopeName) ? fileName : scopeName;

        // ── Edges ──────────────────────────────────────────────────────────
        foreach (var edge in Edges.Where(e => e.input.node != null))
        {
            var outNode = edge.output.node as DialogueNode;
            var inNode  = edge.input.node  as DialogueNode;
            if (outNode == null || inNode == null) continue;

            container.NodeLinks.Add(new NodeLinkData
            {
                baseNodeGuid   = outNode.GUID,
                portName       = edge.output.portName,
                targetNodeGuid = inNode.GUID
            });
        }

        // ── Nodes ──────────────────────────────────────────────────────────
        var allTextIds = new HashSet<string>(); // for unused-dialogue cleanup

        foreach (var node in Nodes)
        {
            var data = new DialogueNodeData
            {
                guid     = node.GUID,
                nodeId   = node.NodeType.ToString(),
                nodeType = node.NodeType,
                position = node.GetPosition().position,
                textId   = node.GetTextId(),
            };

            if (!string.IsNullOrWhiteSpace(data.textId))
                allTextIds.Add(data.textId.Trim());

            switch (node.NodeType)
            {
                case DialogueNodeType.Choice:
                    data.choiceIds = node.GetChoiceIds();
                    foreach (var id in data.choiceIds.Where(id => !string.IsNullOrWhiteSpace(id)))
                        allTextIds.Add(id.Trim());
                    break;
                case DialogueNodeType.Condition:
                    data.conditionKey = node.GetConditionKey();
                    break;
                case DialogueNodeType.Branch:
                    data.branchLabels = node.GetBranchLabels();
                    break;
                case DialogueNodeType.EvidenceCheck:
                    data.evidenceId = node.GetEvidenceId();
                    break;
                case DialogueNodeType.StatementPressed:
                    data.statementId = node.GetStatementId();
                    break;
                case DialogueNodeType.TestimonyContradicted:
                    data.testimonyId          = node.GetTestimonyId();
                    data.contradictEvidenceId = node.GetContradictEvidenceId();
                    break;
                case DialogueNodeType.SceneAlreadySeen:
                    data.sceneId = node.GetSceneId();
                    break;
                case DialogueNodeType.TalkedToCharacter:
                    data.characterId = node.GetCharacterId();
                    break;
            }

            container.DialogueNodeData.Add(data);
        }

        // ── Groups ─────────────────────────────────────────────────────────
        foreach (var group in Groups)
        {
            container.Groups.Add(new GroupData
            {
                guid      = group.GUID,
                title     = group.title,
                position  = group.GetPosition().position,
                nodeGuids = group.containedElements
                    .OfType<DialogueNode>()
                    .Select(n => n.GUID)
                    .ToList()
            });
        }

        // ── Unused dialogue cleanup ────────────────────────────────────────
        WarnUnusedDialogues(allTextIds, container.scopeName);

        // ── Write asset ────────────────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/DialogueAssets"))
            AssetDatabase.CreateFolder("Assets/Resources", "DialogueAssets");

        string assetPath = $"Assets/Resources/DialogueAssets/{fileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<DialogueContainer>(assetPath);
        if (existing != null)
        {
            existing.scopeName        = container.scopeName;
            existing.NodeLinks        = container.NodeLinks;
            existing.DialogueNodeData = container.DialogueNodeData;
            existing.Groups           = container.Groups;
            EditorUtility.SetDirty(existing);
        }
        else
        {
            AssetDatabase.CreateAsset(container, assetPath);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[DialogueGraph] Saved '{fileName}' (scope: {container.scopeName}) — {container.DialogueNodeData.Count} nodes, {container.Groups.Count} groups.");
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    public void LoadGraph(string fileName)
    {
        _cache = Resources.Load<DialogueContainer>($"DialogueAssets/{fileName}");
        if (_cache == null)
        {
            EditorUtility.DisplayDialog("File Not Found",
                $"Could not find dialogue graph '{fileName}' in Resources/DialogueAssets/.", "OK");
            return;
        }

        ClearGraph();
        CreateNodes();
        ConnectNodes();
        RestoreGroups();
    }

    private void ClearGraph()
    {
        _graphView.DeleteElements(_graphView.graphElements.ToList());
    }

    private void CreateNodes()
    {
        foreach (var data in _cache.DialogueNodeData)
        {
            var type = Enum.TryParse(data.nodeId, out DialogueNodeType parsed)
                       ? parsed : DialogueNodeType.Dialogue;

            var node = _graphView.CreateNode(type);
            node.GUID = data.guid;
            node.SetPosition(new Rect(data.position, new Vector2(220, 150)));
            node.SetTextId(data.textId);

            switch (type)
            {
                case DialogueNodeType.Choice:
                    foreach (var id in data.choiceIds) node.AddChoicePort(id);
                    break;
                case DialogueNodeType.Condition:
                    node.SetConditionKey(data.conditionKey);
                    break;
                case DialogueNodeType.Branch:
                    foreach (var label in data.branchLabels) node.AddBranchPort(label);
                    break;
                case DialogueNodeType.EvidenceCheck:
                    node.SetEvidenceId(data.evidenceId);
                    break;
                case DialogueNodeType.StatementPressed:
                    node.SetStatementId(data.statementId);
                    break;
                case DialogueNodeType.TestimonyContradicted:
                    node.SetTestimonyId(data.testimonyId);
                    node.SetContradictEvidenceId(data.contradictEvidenceId);
                    break;
                case DialogueNodeType.SceneAlreadySeen:
                    node.SetSceneId(data.sceneId);
                    break;
                case DialogueNodeType.TalkedToCharacter:
                    node.SetCharacterId(data.characterId);
                    break;
            }

            _graphView.AddElement(node);
        }
    }

    private void ConnectNodes()
    {
        var nodeMap = Nodes.ToDictionary(n => n.GUID);

        foreach (var link in _cache.NodeLinks)
        {
            if (!nodeMap.TryGetValue(link.baseNodeGuid,   out var baseNode))   continue;
            if (!nodeMap.TryGetValue(link.targetNodeGuid, out var targetNode)) continue;

            Port outputPort = baseNode.outputContainer.Children().OfType<Port>()
                .FirstOrDefault(p => p.portName == link.portName)
                ?? baseNode.outputContainer.Children().OfType<Port>().FirstOrDefault();

            Port inputPort = targetNode.inputContainer.Children().OfType<Port>().FirstOrDefault();

            if (outputPort == null || inputPort == null) continue;

            var edge = new Edge { output = outputPort, input = inputPort };
            edge.output.Connect(edge);
            edge.input.Connect(edge);
            _graphView.Add(edge);
        }
    }

    private void RestoreGroups()
    {
        if (_cache.Groups == null) return;
        var nodeMap = Nodes.ToDictionary(n => n.GUID);

        foreach (var gd in _cache.Groups)
        {
            var group = new DialogueGroup(gd.guid) { title = gd.title };
            group.SetPosition(new Rect(gd.position, new Vector2(300, 200)));
            _graphView.AddElement(group);

            foreach (var nodeGuid in gd.nodeGuids)
                if (nodeMap.TryGetValue(nodeGuid, out var node))
                    group.AddElement(node);
        }
    }

    // ── Unused dialogue cleanup ───────────────────────────────────────────────

    private static void WarnUnusedDialogues(HashSet<string> usedIds, string scope)
    {
        DialogueLocalization.ReloadCSV();
        var allIds  = DialogueLocalization.GetAllIds();
        if (allIds == null || allIds.Count == 0) return;

        // Only check IDs that belong to this scope (prefix match: "scope/")
        string prefix = string.IsNullOrWhiteSpace(scope) ? "" : scope + "/";
        var scopedIds = prefix.Length > 0
            ? allIds.Where(id => id.StartsWith(prefix)).ToList()
            : allIds;

        var unused = scopedIds.Where(id => !usedIds.Contains(id)).ToList();
        if (unused.Count == 0) return;

        string list = string.Join("\n", unused.Take(20));
        if (unused.Count > 20) list += $"\n… and {unused.Count - 20} more";

        bool remove = EditorUtility.DisplayDialog(
            "Unused Dialogue IDs",
            $"Found {unused.Count} text ID(s) in scope '{scope}' that are not used by any node:\n\n{list}\n\nRemove them from the CSV?",
            "Remove", "Keep");

        if (remove)
            DialogueLocalization.RemoveIds(unused);
    }
}
