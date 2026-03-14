using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class SearchWindowProvider : ScriptableObject, ISearchWindowProvider
{
    private DialogueGraphView _graphView;
    private EditorWindow      _window;
    private Texture2D         _icon;

    public void Init(EditorWindow window, DialogueGraphView graphView)
    {
        _window    = window;
        _graphView = graphView;
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        return new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),

            // ── Flow ──────────────────────────────────────────────────────
            new SearchTreeGroupEntry(new GUIContent("Flow"), 1),
            Entry("Start", "Start", 2),
            Entry("End",   "End",   2),

            // ── Dialogue ──────────────────────────────────────────────────
            new SearchTreeGroupEntry(new GUIContent("Dialogue"), 1),
            Entry("Dialogue", "Dialogue", 2),
            Entry("Choice",   "Choice",   2),

            // ── Logic ─────────────────────────────────────────────────────
            new SearchTreeGroupEntry(new GUIContent("Logic"), 1),
            Entry("Condition (flag check)", "Condition", 2),
            Entry("Branch (multi-output)",  "Branch",    2),

            // ── Ace Attorney ──────────────────────────────────────────────
            new SearchTreeGroupEntry(new GUIContent("Ace Attorney"), 1),
            Entry("Evidence Check",         "EvidenceCheck",         2),
            Entry("Statement Pressed",      "StatementPressed",      2),
            Entry("Testimony Contradicted", "TestimonyContradicted", 2),
            Entry("Scene Already Seen",     "SceneAlreadySeen",      2),
            Entry("Talked to Character",    "TalkedToCharacter",     2),
        };
    }

    public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
    {
        if (entry.userData is not string nodeTypeName) return false;

        var     root         = _window.rootVisualElement;
        Vector2 localInRoot  = root.ChangeCoordinatesTo(
                                   root.parent,
                                   context.screenMousePosition - _window.position.position);
        Vector2 localInGraph = _graphView.contentViewContainer.WorldToLocal(localInRoot);

        if (System.Enum.TryParse(nodeTypeName, out DialogueNodeType nodeType))
        {
            _graphView.CreateNodeAt(nodeType, localInGraph);
            return true;
        }

        return false;
    }

    private SearchTreeEntry Entry(string label, string userData, int level)
        => new(new GUIContent(label, _icon)) { level = level, userData = userData };
}
