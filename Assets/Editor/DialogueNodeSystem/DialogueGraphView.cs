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

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Assets/Editor/Resources/GraphViewStyles.uss"
        );
        if (styleSheet != null)
            styleSheets.Add(styleSheet);

        AddSearchWindow(editorWindow);

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

    private bool HasNodeType(DialogueNodeType type)
    {
        return nodes.ToList().OfType<DialogueNode>().Any(n => n.NodeType == type);
    }

    private void CreateEssentialNodes()
    {
        if (!HasNodeType(DialogueNodeType.Start))
            CreateNodeAt(DialogueNodeType.Start, new Vector2(100, 200));

        if (!HasNodeType(DialogueNodeType.End))
            CreateNodeAt(DialogueNodeType.End, new Vector2(600, 200));
    }

    public DialogueNode CreateNode(DialogueNodeType nodeType)
    {
        var node = new DialogueNode
        {
            title = GetNodeTitle(nodeType),
            GUID = Guid.NewGuid().ToString()
        };

        node.Initialize(nodeType);
        return node;
    }

    private string GetNodeTitle(DialogueNodeType type)
    {
        return type switch
        {
            DialogueNodeType.Start => "Start Node",
            DialogueNodeType.End => "End Node",
            DialogueNodeType.Choice => "Choice Node",
            DialogueNodeType.IfEvidencePresented => "If Evidence Presented",
            DialogueNodeType.IfStatementPressed => "If Statement Pressed",
            DialogueNodeType.IfTestimonyContradicted => "If Testimony Contradicted",
            DialogueNodeType.IfSceneAlreadySeen => "If Scene Already Seen",
            DialogueNodeType.IfTalkedToCharacter => "If Talked to Character",
            _ => "Dialogue Node"
        };
    }

    public void CreateNodeAt(DialogueNodeType type, Vector2 position)
    {
        if ((type == DialogueNodeType.Start || type == DialogueNodeType.End) && HasNodeType(type))
        {
            EditorUtility.DisplayDialog(
                "Node Already Exists",
                $"Only one {type} node is allowed in the graph.",
                "OK"
            );
            return;
        }

        var node = new DialogueNode
        {
            title = type.ToString() + " Node",
            GUID = Guid.NewGuid().ToString()
        };

        node.Initialize(type);
        node.SetPosition(new Rect(position, new Vector2(200, 200)));
        AddElement(node);
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

    public DialogueNodeType GetNodeTypeFromString(string userData)
    {
        return userData switch
        {
            "Start Node" => DialogueNodeType.Start,
            "End Node" => DialogueNodeType.End,
            "Choice Node" => DialogueNodeType.Choice,
            "If Evidence Presented" => DialogueNodeType.IfEvidencePresented,
            "If Statement Pressed" => DialogueNodeType.IfStatementPressed,
            "If Testimony Contradicted" => DialogueNodeType.IfTestimonyContradicted,
            "If Scene Already Seen" => DialogueNodeType.IfSceneAlreadySeen,
            "If Talked To Character" => DialogueNodeType.IfTalkedToCharacter,
            _ => DialogueNodeType.Dialogue
        };
    }
}