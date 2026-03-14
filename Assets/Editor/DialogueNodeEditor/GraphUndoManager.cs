using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Full undo/redo history for the Dialogue Graph Editor.
/// Call RecordSnapshot() before any mutating operation.
/// Call Undo() / Redo() from keyboard shortcuts.
/// </summary>
public class GraphUndoManager
{
    // ── Snapshot ──────────────────────────────────────────────────────────────

    [Serializable]
    public class GraphSnapshot
    {
        public List<NodeSnapshot>  Nodes  = new();
        public List<LinkSnapshot>  Links  = new();
        public List<GroupSnapshot> Groups = new();
    }

    [Serializable]
    public class NodeSnapshot
    {
        public string Guid;
        public string NodeId;
        public Vector2 Position;
        public DialogueNodeType NodeType;

        // Per-type payloads
        public string TextId;
        public List<string> ChoiceIds       = new();
        public string ConditionKey;
        public List<string> BranchLabels    = new();
        public string EvidenceId;
        public string StatementId;
        public string TestimonyId;
        public string ContradictEvidenceId;
        public string SceneId;
        public string CharacterId;
    }

    [Serializable]
    public class LinkSnapshot
    {
        public string BaseNodeGuid;
        public string PortName;
        public string TargetNodeGuid;
    }

    [Serializable]
    public class GroupSnapshot
    {
        public string Guid;
        public string Title;
        public Vector2 Position;
        public List<string> NodeGuids = new();
    }

    // ── Stacks ────────────────────────────────────────────────────────────────

    private readonly Stack<GraphSnapshot> _undoStack = new();
    private readonly Stack<GraphSnapshot> _redoStack = new();
    private readonly DialogueGraphView    _graphView;

    private bool _isRestoring = false;
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public GraphUndoManager(DialogueGraphView graphView)
    {
        _graphView = graphView;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Call this BEFORE any mutating operation.</summary>
    public void RecordSnapshot()
    {
        if (_isRestoring) return; // ignore changes triggered by restore itself
        _undoStack.Push(Capture());
        _redoStack.Clear();
    }

    public void Undo()
    {
        if (!CanUndo) return;
        _redoStack.Push(Capture());
        _isRestoring = true;
        Restore(_undoStack.Pop());
        _isRestoring = false;
    }

    public void Redo()
    {
        if (!CanRedo) return;
        _undoStack.Push(Capture());
        _isRestoring = true;
        Restore(_redoStack.Pop());
        _isRestoring = false;
    }

    // ── Capture ───────────────────────────────────────────────────────────────

    private GraphSnapshot Capture()
    {
        var snap = new GraphSnapshot();

        // Nodes
        foreach (var n in _graphView.nodes.ToList().OfType<DialogueNode>())
        {
            snap.Nodes.Add(new NodeSnapshot
            {
                Guid                = n.GUID,
                NodeId              = n.NodeType.ToString(),
                NodeType            = n.NodeType,
                Position            = n.GetPosition().position,
                TextId              = n.GetTextId(),
                ChoiceIds           = n.GetChoiceIds(),
                ConditionKey        = n.GetConditionKey(),
                BranchLabels        = n.GetBranchLabels(),
                EvidenceId          = n.GetEvidenceId(),
                StatementId         = n.GetStatementId(),
                TestimonyId         = n.GetTestimonyId(),
                ContradictEvidenceId = n.GetContradictEvidenceId(),
                SceneId             = n.GetSceneId(),
                CharacterId         = n.GetCharacterId(),
            });
        }

        // Edges
        foreach (var e in _graphView.edges.ToList())
        {
            if (e.output?.node is DialogueNode outNode && e.input?.node is DialogueNode inNode)
            {
                snap.Links.Add(new LinkSnapshot
                {
                    BaseNodeGuid   = outNode.GUID,
                    PortName       = e.output.portName,
                    TargetNodeGuid = inNode.GUID,
                });
            }
        }

        // Groups
        foreach (var g in _graphView.graphElements.ToList().OfType<DialogueGroup>())
        {
            snap.Groups.Add(new GroupSnapshot
            {
                Guid      = g.GUID,
                Title     = g.title,
                Position  = g.GetPosition().position,
                NodeGuids = g.containedElements.OfType<DialogueNode>().Select(n => n.GUID).ToList(),
            });
        }

        return snap;
    }

    // ── Restore ───────────────────────────────────────────────────────────────

    private void Restore(GraphSnapshot snap)
    {
        // Clear canvas
        _graphView.DeleteElements(_graphView.graphElements.ToList());

        // Recreate nodes
        foreach (var ns in snap.Nodes)
        {
            var node = _graphView.CreateNode(ns.NodeType);
            node.GUID = ns.Guid;
            node.SetPosition(new Rect(ns.Position, new Vector2(220, 150)));
            node.SetTextId(ns.TextId);
            node.SetConditionKey(ns.ConditionKey);
            node.SetEvidenceId(ns.EvidenceId);
            node.SetStatementId(ns.StatementId);
            node.SetTestimonyId(ns.TestimonyId);
            node.SetContradictEvidenceId(ns.ContradictEvidenceId);
            node.SetSceneId(ns.SceneId);
            node.SetCharacterId(ns.CharacterId);

            foreach (var id in ns.ChoiceIds)    node.AddChoicePort(id);
            foreach (var lb in ns.BranchLabels) node.AddBranchPort(lb);

            _graphView.AddElement(node);
        }

        // Reconnect edges
        var nodeMap = _graphView.nodes.ToList()
            .OfType<DialogueNode>()
            .ToDictionary(n => n.GUID);

        foreach (var ls in snap.Links)
        {
            if (!nodeMap.TryGetValue(ls.BaseNodeGuid,   out var baseNode))   continue;
            if (!nodeMap.TryGetValue(ls.TargetNodeGuid, out var targetNode)) continue;

            var outputPort = baseNode.outputContainer.Children().OfType<Port>()
                .FirstOrDefault(p => p.portName == ls.PortName)
                ?? baseNode.outputContainer.Children().OfType<Port>().FirstOrDefault();

            var inputPort = targetNode.inputContainer.Children().OfType<Port>().FirstOrDefault();

            if (outputPort == null || inputPort == null) continue;

            var edge = new Edge { output = outputPort, input = inputPort };
            edge.output.Connect(edge);
            edge.input.Connect(edge);
            _graphView.Add(edge);
        }

        // Recreate groups
        foreach (var gs in snap.Groups)
        {
            var group = new DialogueGroup(gs.Guid) { title = gs.Title };
            group.SetPosition(new Rect(gs.Position, new Vector2(300, 200)));
            _graphView.AddElement(group);

            foreach (var guid in gs.NodeGuids)
                if (nodeMap.TryGetValue(guid, out var node))
                    group.AddElement(node);
        }
    }
}