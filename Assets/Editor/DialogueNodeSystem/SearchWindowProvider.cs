using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine.NVIDIA;

public class SearchWindowProvider : ScriptableObject, ISearchWindowProvider
{
    private DialogueGraphView graphView;
    private EditorWindow window;

    private Texture2D icon;

    public void Init(EditorWindow window, DialogueGraphView graphView)
    {
        this.window = window;
        this.graphView = graphView;
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var tree = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Create Elements"), 0),

            new SearchTreeGroupEntry(new GUIContent("Flow"), 1),
            new SearchTreeEntry(new GUIContent("Start Node", icon)) { level = 2, userData = "Start Node" },
            new SearchTreeEntry(new GUIContent("End Node", icon)) { level = 2, userData = "End Node" },

            new SearchTreeGroupEntry(new GUIContent("Dialogue"), 1),
            new SearchTreeEntry(new GUIContent("Dialogue Node", icon)) { level = 2, userData = "Dialogue Node" },
            new SearchTreeEntry(new GUIContent("Choice Node", icon)) { level = 2, userData = "Choice Node" },

            new SearchTreeGroupEntry(new GUIContent("Case Logic"), 1),
            new SearchTreeEntry(new GUIContent("If Evidence Presented", icon)) { level = 2, userData = "If Evidence Presented" },
            new SearchTreeEntry(new GUIContent("If Statement Pressed", icon)) { level = 2, userData = "If Statement Pressed" },
            new SearchTreeEntry(new GUIContent("If Testimony Contradicted", icon)) { level = 2, userData = "If Testimony Contradicted" },
            new SearchTreeEntry(new GUIContent("If Scene Already Seen", icon)) { level = 2, userData = "If Scene Already Seen" },
            new SearchTreeEntry(new GUIContent("If Talked to Character", icon)) { level = 2, userData = "If Talked to Character" },
        };

        return tree;
    }

    public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
    {
        var worldMousePosition = window.rootVisualElement.ChangeCoordinatesTo(
            window.rootVisualElement.parent,
            context.screenMousePosition - window.position.position
        );
        var localMousePosition = graphView.contentViewContainer.WorldToLocal(worldMousePosition);

        string nodeName = entry.userData as string;
        if (string.IsNullOrEmpty(nodeName)) return false;

        var nodeType = graphView.GetNodeTypeFromString(nodeName);
        graphView.CreateNodeAt(nodeType, localMousePosition);
        return true;
    }
}