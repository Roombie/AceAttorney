using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.UIElements;

public class GraphSaveUtility
{
    private DialogueGraphView targetGraphView;
    private DialogueContainer containerCache;

    private List<Edge> Edges => targetGraphView.edges.ToList();
    private List<DialogueNode> Nodes => targetGraphView.nodes.ToList().Cast<DialogueNode>().ToList();

    public static GraphSaveUtility GetInstance(DialogueGraphView graphView)
    {
        return new GraphSaveUtility { targetGraphView = graphView };
    }

    public void SaveGraph(string fileName)
    {
        if (!Edges.Any()) return;

        var dialogueContainer = ScriptableObject.CreateInstance<DialogueContainer>();
        var connectedPorts = Edges.Where(x => x.input.node != null).ToList();

        foreach (var edge in connectedPorts)
        {
            var outputNode = edge.output.node as DialogueNode;
            var inputNode = edge.input.node as DialogueNode;

            dialogueContainer.NodeLinks.Add(new NodeLinkData
            {
                baseNodeGuid = outputNode.GUID,
                targetNodeGuid = inputNode.GUID,
                portName = edge.output.portName
            });
        }

        foreach (var node in Nodes)
        {
            var data = new DialogueNodeData
            {
                guid = node.GUID,
                nodeId = node.NodeType.ToString(),
                position = node.GetPosition().position,
                textId = node.GetTextId(),
                nodeType = node.NodeType
            };

            // Si es un nodo de tipo "Choice"
            if (node.NodeType == DialogueNodeType.Choice)
            {
                data.choiceIds = node.GetChoiceIds(); // ← necesitas definir GetChoiceIds()
            }

            // Si es un nodo condicional
            if (node.NodeType.ToString().StartsWith("If"))
            {
                data.conditionLabelTrue = node.GetConditionLabel(true);
                data.conditionLabelFalse = node.GetConditionLabel(false);
            }

            dialogueContainer.DialogueNodeData.Add(data);
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources/DialogueAssets"))
            AssetDatabase.CreateFolder("Assets/Resources", "DialogueAssets");

        AssetDatabase.CreateAsset(dialogueContainer, $"Assets/Resources/DialogueAssets/{fileName}.asset");
        AssetDatabase.SaveAssets();
    }

    public void LoadGraph(string fileName)
    {
        containerCache = Resources.Load<DialogueContainer>($"DialogueAssets/{fileName}");

        if (containerCache == null)
        {
            EditorUtility.DisplayDialog("File Not Found", "Target dialogue graph file does not exist!", "OK");
            return;
        }

        ClearGraph();
        CreateNodes();
        ConnectNodes();
    }

    private void ClearGraph()
    {
        foreach (var node in Nodes)
        {
            Edges.Where(x => x.input.node == node || x.output.node == node).ToList()
                 .ForEach(edge => targetGraphView.RemoveElement(edge));
            targetGraphView.RemoveElement(node);
        }
    }

    private void CreateNodes()
    {
        foreach (var nodeData in containerCache.DialogueNodeData)
        {
            var type = Enum.TryParse(nodeData.nodeId, out DialogueNodeType parsedType) ? parsedType : DialogueNodeType.Dialogue;
            var tempNode = targetGraphView.CreateNode(type);
            tempNode.GUID = nodeData.guid;
            tempNode.SetPosition(new Rect(nodeData.position, new Vector2(200, 200)));
            tempNode.SetTextId(nodeData.textId);

            if (parsedType == DialogueNodeType.Choice)
            {
                foreach (var choice in nodeData.choiceIds)
                {
                    tempNode.AddChoicePort(choice);
                }
            }
            targetGraphView.AddElement(tempNode);
        }
    }

    private void ConnectNodes()
    {
        foreach (var link in containerCache.NodeLinks)
        {
            var baseNode = Nodes.FirstOrDefault(n => n.GUID == link.baseNodeGuid);
            var targetNode = Nodes.FirstOrDefault(n => n.GUID == link.targetNodeGuid);

            if (baseNode == null || targetNode == null) continue;

            var outputPort = baseNode.outputContainer.Q<Port>();
            var inputPort = targetNode.inputContainer.Q<Port>();

            var tempEdge = new Edge
            {
                output = outputPort,
                input = inputPort
            };
            tempEdge.input.Connect(tempEdge);
            tempEdge.output.Connect(tempEdge);

            targetGraphView.Add(tempEdge);
        }
    }
}