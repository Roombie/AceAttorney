using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

public class DialogueNode : Node
{
    public string GUID;
    public DialogueNodeType NodeType;

    private Label previewLabel;
    private TextField textIdField;
    private string CurrentLanguage => LocalizationSettings.SelectedLocale.Identifier.Code.ToUpper();

    private Port GeneratePort(Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        return InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(string));
    }

    public void Initialize(DialogueNodeType nodeType)
    {
        if (Application.isEditor)
        {
            DialogueLocalization.Load();
        }

        title = GetNodeTitle(nodeType);
        NodeType = nodeType;

        switch (nodeType)
        {
            case DialogueNodeType.Start:
                SetupStartNode();
                titleContainer.style.backgroundColor = new Color(0f, 0f, 0f);
                break;
            case DialogueNodeType.End:
                SetupEndNode();
                titleContainer.style.backgroundColor = new Color(0f, 0f, 0f);
                break;
            case DialogueNodeType.Choice:
                SetupChoiceNode();
                titleContainer.style.backgroundColor = new Color(0.2f, 0.4f, 0.6f);
                break;
            case DialogueNodeType.IfEvidencePresented:
            case DialogueNodeType.IfStatementPressed:
            case DialogueNodeType.IfTestimonyContradicted:
            case DialogueNodeType.IfSceneAlreadySeen:
            case DialogueNodeType.IfTalkedToCharacter:
                SetupCaseLogicNode(title);
                titleContainer.style.backgroundColor = new Color(0.8f, 0.1f, 0.1f);
                break;
            default:
                SetupDialogueNode();
                titleContainer.style.backgroundColor = new Color(0.2f, 0.6f, 0.2f);
                break;
        }

        RefreshPorts();
        RefreshExpandedState();

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private string GetNodeTitle(DialogueNodeType type)
    {
        return type switch
        {
            DialogueNodeType.IfEvidencePresented => "If Evidence Presented",
            DialogueNodeType.IfStatementPressed => "If Statement Pressed",
            DialogueNodeType.IfTestimonyContradicted => "If Testimony Contradicted",
            DialogueNodeType.IfSceneAlreadySeen => "If Scene Already Seen",
            DialogueNodeType.IfTalkedToCharacter => "If Talked to Character",
            _ => type.ToString()
        };
    }

    public string GetTextId()
    {
        return textIdField?.value ?? "";
    }

    public void SetTextId(string textId)
    {
        if (textIdField != null)
        {
            textIdField.SetValueWithoutNotify(textId); // avoid duplicated callback
            previewLabel.text = DialogueLocalization.GetText(textId, CurrentLanguage);
        }
    }

    public List<string> GetChoiceIds()
    {
        var ids = new List<string>();
        foreach (var child in outputContainer.Children())
        {
            var field = child.Q<TextField>();
            if (field != null) ids.Add(field.value);
        }
        return ids;
    }

    public string GetConditionLabel(bool isTrue)
    {
        var port = outputContainer.Children().FirstOrDefault(p =>
            (p as Port)?.portName == (isTrue ? "True" : "False")) as VisualElement;

        return port?.Q<Label>()?.text ?? "";
    }

    public void SetConditionLabel(bool isTrue, string value)
    {
        var port = outputContainer.Children().FirstOrDefault(p =>
            (p as Port)?.portName == (isTrue ? "True" : "False")) as VisualElement;

        var label = port?.Q<Label>();
        if (label != null)
        {
            label.text = value;
        }
    }

    public DialogueNode()
    {
        RegisterCallback<DetachFromPanelEvent>(OnNodeDetached);
    }

    private void OnNodeDetached(DetachFromPanelEvent evt)
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale locale)
    {
        UpdatePreview();
    }

    public void ForcePreviewUpdate()
    {
        UpdatePreview();
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

    private void SetupCaseLogicNode(string titleText)
    {
        var inputPort = GeneratePort(Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        var truePort = GeneratePort(Direction.Output, Port.Capacity.Single);
        truePort.portName = "True";
        outputContainer.Add(truePort);

        var falsePort = GeneratePort(Direction.Output, Port.Capacity.Single);
        falsePort.portName = "False";
        outputContainer.Add(falsePort);

        title = titleText;
    }

    private void SetupDialogueNode()
    {
        var inputPort = GeneratePort(Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Previous";
        inputContainer.Add(inputPort);

        var outputPort = GeneratePort(Direction.Output, Port.Capacity.Single);
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        // Text ID field
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.marginBottom = 4;

        var label = new Label("Text ID");
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginBottom = 2;

        textIdField = new TextField();
        textIdField.RegisterValueChangedCallback(evt => UpdatePreview());
        textIdField.multiline = false;

        container.Add(label);
        container.Add(textIdField);
        mainContainer.Add(container);

        // Preview label
        previewLabel = new Label("NO PREVIEW");
        previewLabel.style.marginTop = 6;
        previewLabel.style.backgroundColor = new Color(0.2f, 0.6f, 0.2f, 0.4f); // #339933 with alpha
        mainContainer.Add(previewLabel);

        UpdatePreview();
    }

    private void SetupChoiceNode()
    {
        var inputPort = GeneratePort(Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        var addChoiceButton = new Button(() => AddChoicePort("Choice"))
        {
            text = "Add Choice"
        };
        titleButtonContainer.Add(addChoiceButton);
    }

    private void UpdatePreview()
    {
        if (previewLabel == null || textIdField == null)
            return;

        string id = textIdField.value.Trim();

        if (string.IsNullOrEmpty(id))
        {
            previewLabel.text = "NO PREVIEW";
        }
        else
        {
            string result = DialogueLocalization.GetText(id, CurrentLanguage);
            previewLabel.text = result;
        }
    }

    public void AddChoicePort(string choiceId)
    {
        var outputPort = GeneratePort(Direction.Output, Port.Capacity.Single);

        var idField = new TextField
        {
            value = choiceId
        };
        var label = new Label(DialogueLocalization.GetText(choiceId.Trim(), CurrentLanguage))
        {
            style = { marginLeft = 4 }
        };

        idField.RegisterValueChangedCallback(evt =>
        {
            string trimmedId = evt.newValue.Trim();
            label.text = DialogueLocalization.GetText(trimmedId, CurrentLanguage);
        });

        outputPort.portName = "";

        var deleteButton = new Button(() =>
        {
            outputContainer.Remove(outputPort);
            RefreshPorts();
            RefreshExpandedState();
        })
        {
            text = "X"
        };

        var container = new VisualElement
        {
            style = { flexDirection = FlexDirection.Row }
        };
        container.Add(idField);
        container.Add(label);
        container.Add(deleteButton);

        outputPort.contentContainer.Add(container);
        outputContainer.Add(outputPort);

        RefreshPorts();
        RefreshExpandedState();
    }
}