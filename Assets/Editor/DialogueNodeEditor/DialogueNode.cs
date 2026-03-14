using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class DialogueNode : Node
{
    public string           GUID;
    public DialogueNodeType NodeType;

    private Label     _previewLabel;
    private TextField _textIdField;
    private TextField _conditionKeyField;

    private TextField _evidenceIdField;
    private TextField _statementIdField;
    private TextField _testimonyIdField;
    private TextField _contradictEvidenceIdField;
    private TextField _sceneIdField;
    private TextField _characterIdField;

    private string _pendingTextId               = "";
    private string _pendingConditionKey         = "";
    private string _pendingEvidenceId           = "";
    private string _pendingStatementId          = "";
    private string _pendingTestimonyId          = "";
    private string _pendingContradictEvidenceId = "";
    private string _pendingSceneId              = "";
    private string _pendingCharacterId          = "";

    private string CurrentLanguage => DialogueLocalization.GetEditorLanguageCode();

    public DialogueNode()
    {
        RegisterCallback<DetachFromPanelEvent>(_ => UnsubscribeLocale());
    }

    // ── Initialize ────────────────────────────────────────────────────────────

    public void Initialize(DialogueNodeType nodeType)
    {
        NodeType = nodeType;
        title    = GetNodeTitle(nodeType);

        switch (nodeType)
        {
            case DialogueNodeType.Start:
                SetTitleColor(new Color(0.15f, 0.15f, 0.15f));
                AddPort(Direction.Output, Port.Capacity.Single, "Next", outputContainer);
                break;
            case DialogueNodeType.End:
                SetTitleColor(new Color(0.15f, 0.15f, 0.15f));
                AddPort(Direction.Input, Port.Capacity.Multi, "Previous", inputContainer);
                break;
            case DialogueNodeType.Dialogue:
                SetTitleColor(new Color(0.18f, 0.50f, 0.18f));
                AddPort(Direction.Input,  Port.Capacity.Multi,  "Previous", inputContainer);
                AddPort(Direction.Output, Port.Capacity.Single, "Next",     outputContainer);
                break;
            case DialogueNodeType.Choice:
                SetTitleColor(new Color(0.20f, 0.38f, 0.60f));
                AddPort(Direction.Input, Port.Capacity.Multi, "Input", inputContainer);
                break;
            case DialogueNodeType.Condition:
                SetTitleColor(new Color(0.65f, 0.35f, 0.10f));
                AddPort(Direction.Input,  Port.Capacity.Multi,  "Input", inputContainer);
                AddPort(Direction.Output, Port.Capacity.Single, "True",  outputContainer);
                AddPort(Direction.Output, Port.Capacity.Single, "False", outputContainer);
                break;
            case DialogueNodeType.Branch:
                SetTitleColor(new Color(0.50f, 0.18f, 0.50f));
                AddPort(Direction.Input, Port.Capacity.Multi, "Input", inputContainer);
                break;
            case DialogueNodeType.EvidenceCheck:
            case DialogueNodeType.StatementPressed:
            case DialogueNodeType.TestimonyContradicted:
            case DialogueNodeType.SceneAlreadySeen:
            case DialogueNodeType.TalkedToCharacter:
                SetTitleColor(new Color(0.70f, 0.10f, 0.10f));
                AddPort(Direction.Input,  Port.Capacity.Multi,  "Input", inputContainer);
                AddPort(Direction.Output, Port.Capacity.Single, "True",  outputContainer);
                AddPort(Direction.Output, Port.Capacity.Single, "False", outputContainer);
                break;
        }

        RefreshPorts();
        RefreshExpandedState();
        schedule.Execute(BuildTextUI);
    }

    // ── Deferred UI ───────────────────────────────────────────────────────────

    private void BuildTextUI()
    {
        switch (NodeType)
        {
            case DialogueNodeType.Dialogue:
                BuildDialogueUI();
                break;
            case DialogueNodeType.Condition:
                _conditionKeyField = BuildField("Flag / Variable", _pendingConditionKey);
                _pendingConditionKey = "";
                break;
            case DialogueNodeType.Choice:
                titleButtonContainer.Add(new Button(() => AddChoicePort("choice_id")) { text = "+ Choice" });
                break;
            case DialogueNodeType.Branch:
                titleButtonContainer.Add(new Button(() => AddBranchPort("Branch")) { text = "+ Branch" });
                break;

            // ── Ace Attorney ──────────────────────────────────────────────
            case DialogueNodeType.EvidenceCheck:
                _evidenceIdField = BuildField("Evidence ID", _pendingEvidenceId);
                _pendingEvidenceId = "";
                break;
            case DialogueNodeType.StatementPressed:
                _statementIdField = BuildField("Statement ID", _pendingStatementId);
                _pendingStatementId = "";
                break;
            case DialogueNodeType.TestimonyContradicted:
                _testimonyIdField          = BuildField("Testimony ID", _pendingTestimonyId);
                _contradictEvidenceIdField = BuildField("Evidence ID",  _pendingContradictEvidenceId);
                _pendingTestimonyId = "";
                _pendingContradictEvidenceId = "";
                break;
            case DialogueNodeType.SceneAlreadySeen:
                _sceneIdField = BuildField("Scene ID", _pendingSceneId);
                _pendingSceneId = "";
                break;
            case DialogueNodeType.TalkedToCharacter:
                _characterIdField = BuildField("Character ID", _pendingCharacterId);
                _pendingCharacterId = "";
                break;
        }

        SubscribeLocale();
        RefreshExpandedState();
    }

    // ── UI builders ───────────────────────────────────────────────────────────

    private void BuildDialogueUI()
    {
        _textIdField = new TextField { multiline = false };
        _textIdField.RegisterValueChangedCallback(_ => UpdatePreview());

        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.marginBottom  = 4;
        container.Add(MakeBoldLabel("Text ID"));
        container.Add(_textIdField);
        mainContainer.Add(container);

        _previewLabel = new Label("—")
        {
            style =
            {
                marginTop       = 4,
                paddingLeft     = 4,
                paddingRight    = 4,
                whiteSpace      = WhiteSpace.Normal,
                backgroundColor = new Color(0.18f, 0.50f, 0.18f, 0.35f)
            }
        };
        mainContainer.Add(_previewLabel);

        if (!string.IsNullOrEmpty(_pendingTextId))
        {
            _textIdField.SetValueWithoutNotify(_pendingTextId);
            _pendingTextId = "";
        }

        UpdatePreview();
    }

    /// <summary>Creates a labelled TextField, adds it to mainContainer, and returns it.</summary>
    private TextField BuildField(string label, string pendingValue = "")
    {
        var field = new TextField { multiline = false };

        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.marginBottom  = 4;
        container.Add(MakeBoldLabel(label));
        container.Add(field);
        mainContainer.Add(container);

        if (!string.IsNullOrEmpty(pendingValue))
            field.SetValueWithoutNotify(pendingValue);

        return field;
    }

    private static Label MakeBoldLabel(string text) =>
        new Label(text) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } };

    // ── Public accessors ─────────────────────────────────────────────────────

    public string GetTextId()               => _textIdField?.value               ?? _pendingTextId;
    public string GetConditionKey()         => _conditionKeyField?.value         ?? _pendingConditionKey;
    public string GetEvidenceId()           => _evidenceIdField?.value           ?? _pendingEvidenceId;
    public string GetStatementId()          => _statementIdField?.value          ?? _pendingStatementId;
    public string GetTestimonyId()          => _testimonyIdField?.value          ?? _pendingTestimonyId;
    public string GetContradictEvidenceId() => _contradictEvidenceIdField?.value ?? _pendingContradictEvidenceId;
    public string GetSceneId()              => _sceneIdField?.value              ?? _pendingSceneId;
    public string GetCharacterId()          => _characterIdField?.value          ?? _pendingCharacterId;

    public void SetTextId(string v)
    {
        if (_textIdField != null) { _textIdField.SetValueWithoutNotify(v); UpdatePreview(); }
        else _pendingTextId = v;
    }

    public void SetConditionKey(string v)         => SetOrPend(v, _conditionKeyField,         ref _pendingConditionKey);
    public void SetEvidenceId(string v)           => SetOrPend(v, _evidenceIdField,           ref _pendingEvidenceId);
    public void SetStatementId(string v)          => SetOrPend(v, _statementIdField,          ref _pendingStatementId);
    public void SetTestimonyId(string v)          => SetOrPend(v, _testimonyIdField,          ref _pendingTestimonyId);
    public void SetContradictEvidenceId(string v) => SetOrPend(v, _contradictEvidenceIdField, ref _pendingContradictEvidenceId);
    public void SetSceneId(string v)              => SetOrPend(v, _sceneIdField,              ref _pendingSceneId);
    public void SetCharacterId(string v)          => SetOrPend(v, _characterIdField,          ref _pendingCharacterId);

    private static void SetOrPend(string value, TextField field, ref string pending)
    {
        if (field != null) field.SetValueWithoutNotify(value);
        else pending = value;
    }

    public List<string> GetChoiceIds() =>
        outputContainer.Children()
            .Select(c => c.Q<TextField>()).Where(f => f != null).Select(f => f.value).ToList();

    public List<string> GetBranchLabels() =>
        outputContainer.Children()
            .Select(c => c.Q<TextField>()).Where(f => f != null).Select(f => f.value).ToList();

    // ── Dynamic ports ─────────────────────────────────────────────────────────

    public void AddChoicePort(string choiceId)
    {
        var port = MakePort(Direction.Output, Port.Capacity.Single);
        port.portName = "";

        var idField = new TextField { value = choiceId };
        var preview = new Label(DialogueLocalization.GetText(choiceId.Trim(), CurrentLanguage))
                      { style = { marginLeft = 4 } };

        idField.RegisterValueChangedCallback(evt =>
            preview.text = DialogueLocalization.GetText(evt.newValue.Trim(), CurrentLanguage));

        var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        row.Add(idField);
        row.Add(preview);
        row.Add(new Button(() => RemoveOutputPort(port)) { text = "X" });
        port.contentContainer.Add(row);

        outputContainer.Add(port);
        RefreshPorts();
        RefreshExpandedState();
    }

    public void AddBranchPort(string label)
    {
        var port = MakePort(Direction.Output, Port.Capacity.Single);
        port.portName = "";

        var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        row.Add(new TextField { value = label });
        row.Add(new Button(() => RemoveOutputPort(port)) { text = "X" });
        port.contentContainer.Add(row);

        outputContainer.Add(port);
        RefreshPorts();
        RefreshExpandedState();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AddPort(Direction dir, Port.Capacity capacity, string portName, VisualElement container)
    {
        var port = InstantiatePort(Orientation.Horizontal, dir, capacity, typeof(string));
        port.portName = portName;
        container.Add(port);
    }

    private Port MakePort(Direction dir, Port.Capacity capacity)
        => InstantiatePort(Orientation.Horizontal, dir, capacity, typeof(string));

    private void RemoveOutputPort(Port port)
    {
        var gv = this.GetFirstAncestorOfType<GraphView>();
        if (gv != null)
            foreach (var edge in port.connections.ToList())
                gv.RemoveElement(edge);

        outputContainer.Remove(port);
        RefreshPorts();
        RefreshExpandedState();
    }

    private void SetTitleColor(Color c) => titleContainer.style.backgroundColor = c;

    private static string GetNodeTitle(DialogueNodeType type) => type switch
    {
        DialogueNodeType.Start                 => "START",
        DialogueNodeType.End                   => "END",
        DialogueNodeType.Dialogue              => "Dialogue",
        DialogueNodeType.Choice                => "Choice",
        DialogueNodeType.Condition             => "Condition",
        DialogueNodeType.Branch                => "Branch",
        DialogueNodeType.EvidenceCheck         => "Evidence Check",
        DialogueNodeType.StatementPressed      => "Statement Pressed",
        DialogueNodeType.TestimonyContradicted => "Testimony Contradicted",
        DialogueNodeType.SceneAlreadySeen      => "Scene Already Seen",
        DialogueNodeType.TalkedToCharacter     => "Talked to Character",
        _                                      => type.ToString()
    };

    // ── Locale ────────────────────────────────────────────────────────────────

    private void SubscribeLocale()
    {
#if UNITY_LOCALIZATION
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
#endif
    }

    private void UnsubscribeLocale()
    {
#if UNITY_LOCALIZATION
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
#endif
    }

#if UNITY_LOCALIZATION
    private void OnLocaleChanged(UnityEngine.Localization.Locale _) => ForcePreviewUpdate();
#endif

    public void ForcePreviewUpdate() => UpdatePreview();

    private void UpdatePreview()
    {
        if (_previewLabel == null || _textIdField == null) return;
        string id = _textIdField.value.Trim();
        _previewLabel.text = string.IsNullOrEmpty(id)
            ? "—"
            : DialogueLocalization.GetText(id, CurrentLanguage);
    }
}