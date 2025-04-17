using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class DialogueNode : Node
{
    public string GUID;
    public string NodeType; // e.g., "Dialogue Node" or "Choice Node"

    private Port GeneratePort(Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        return InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(string));
    }

    public void Initialize(string nodeType)
    {
        NodeType = nodeType;

        switch (nodeType)
        {
            case "Start Node":
                SetupStartNode();
                break;
            case "End Node":
                SetupEndNode();
                break;
            case "Choice Node":
                SetupChoiceNode();
                break;
            default:
                SetupDialogueNode();
                break;
        }

        RefreshPorts();
        RefreshExpandedState();
    }

    private void SetupStartNode()
    {
        var outputPort = GeneratePort(Direction.Output, Port.Capacity.Single);
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        title = "START";
    }

    private void SetupEndNode()
    {
        var inputPort = GeneratePort(Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Previous";
        inputContainer.Add(inputPort);

        title = "END";
    }

    private void SetupDialogueNode()
    {
        var inputPort = GeneratePort(Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Previous";
        inputContainer.Add(inputPort);

        var outputPort = GeneratePort(Direction.Output, Port.Capacity.Single);
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);
    }

    private void SetupChoiceNode()
    {
        var inputPort = GeneratePort(Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Add button to add choices
        var addChoiceButton = new Button(() =>
        {
            AddChoicePort("Choice");
        })
        {
            text = "Add Choice"
        };
        titleButtonContainer.Add(addChoiceButton);
    }

    public void AddChoicePort(string choiceName)
    {
        var outputPort = GeneratePort(Direction.Output, Port.Capacity.Single);

        var textField = new TextField
        {
            value = choiceName
        };
        textField.RegisterValueChangedCallback(evt =>
        {
            outputPort.portName = evt.newValue;
        });

        outputPort.portName = choiceName;

        var deleteButton = new Button(() =>
        {
            outputContainer.Remove(outputPort);
            RefreshPorts();
            RefreshExpandedState();
        })
        {
            text = "X"
        };

        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Row;
        container.Add(textField);
        container.Add(deleteButton);

        outputPort.contentContainer.Add(container);

        outputContainer.Add(outputPort);
        RefreshPorts();
        RefreshExpandedState();
    }
}