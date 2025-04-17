using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;
using System.Collections.Generic;

public class DialogueGraphView : GraphView
{
    private SearchWindowProvider searchWindowProvider;

    public DialogueGraphView(EditorWindow editorWindow)
    {
        this.StretchToParentSize();
        style.flexGrow = 1;

        // Zoom y manipulación
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // Fondo cuadriculado
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        // Estilo visual
        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Assets/Editor/Resources/GraphViewStyles.uss"
        );
        if (styleSheet != null)
            styleSheets.Add(styleSheet);

        // Search window contextual
        AddSearchWindow(editorWindow);

        // Menú contextual personalizado
        contentViewContainer.RegisterCallback<ContextClickEvent>(evt =>
        {
            if (selection == null || selection.Count == 0)
            {
                evt.StopImmediatePropagation();
                nodeCreationRequest?.Invoke(new NodeCreationContext
                {
                    screenMousePosition = evt.mousePosition
                });
            }
        });

        CreateEssentialNodes();
    }

    private void AddSearchWindow(EditorWindow editorWindow)
    {
        searchWindowProvider = ScriptableObject.CreateInstance<SearchWindowProvider>();
        searchWindowProvider.Init(editorWindow, this);
        nodeCreationRequest = context =>
        {
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindowProvider);
        };
    }

    private bool HasNodeType(string nodeType)
    {
        return nodes.ToList().OfType<DialogueNode>().Any(n => n.NodeType == nodeType);
    }

    private void CreateEssentialNodes()
    {
        if (!HasNodeType("Start Node"))
            CreateNodeAt("Start Node", new Vector2(100, 200));

        if (!HasNodeType("End Node"))
            CreateNodeAt("End Node", new Vector2(600, 200));
    }

    public void CreateNodeAt(string nodeName, Vector2 position)
    {
        if ((nodeName == "Start Node" || nodeName == "End Node") && HasNodeType(nodeName))
        {
            EditorUtility.DisplayDialog(
                "Node Already Exists",
                $"Only one {nodeName} is allowed in the graph.",
                "OK"
            );
            return;
        }

        DialogueNode node;

        switch (nodeName)
        {
            case "Choice Node":
                node = CreateChoiceNode(nodeName);
                break;
            default:
                node = CreateDialogueNode(nodeName);
                break;
        }

        node.SetPosition(new Rect(position, new Vector2(200, 200)));
        AddElement(node);
    }

    public DialogueNode CreateDialogueNode(string nodeName)
    {
        var node = new DialogueNode
        {
            title = nodeName,
            GUID = Guid.NewGuid().ToString()
        };

        node.Initialize(nodeName);
        return node;
    }

    public DialogueNode CreateChoiceNode(string nodeName)
    {
        var node = new DialogueNode
        {
            title = nodeName,
            GUID = Guid.NewGuid().ToString()
        };

        node.Initialize("Choice Node");
        return node;
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        ports.ForEach(port =>
        {
            if (startPort != port && startPort.node != port.node)
                compatiblePorts.Add(port);
        });
        return compatiblePorts;
    }
}