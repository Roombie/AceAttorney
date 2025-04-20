using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

public class DialogueGraphEditor : EditorWindow
{
    private DialogueGraphView graphView;
    private string currentFileName = "";
    private TextField fileNameField;

    [MenuItem("Tools/Dialogue Graph Editor")]
    private static void OpenWindow()
    {
        DialogueGraphEditor window = GetWindow<DialogueGraphEditor>();
        window.titleContent = new GUIContent("Dialogue Graph");
        window.minSize = new Vector2(800, 600);
    }

    void OnEnable()
    {
        ConstructGraphView();
        GenerateToolbar();
        GenerateMinimap();
    }
    
    private void ConstructGraphView()
    {
        graphView = new DialogueGraphView(this)
        {
            name = "Dialogue Graph"
        };

        graphView.StretchToParentSize();
        rootVisualElement.Add(graphView);
    }

    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();
        
        var fileNameField = new TextField("File Name");
        fileNameField.RegisterValueChangedCallback(evt =>
        {
            currentFileName = evt.newValue.Trim();
        });
        toolbar.Add(fileNameField);

        toolbar.Add(new Button(() => RequestDataOperation(true)) {text = "Save"});
        toolbar.Add(new Button(() => RequestDataOperation(false)) {text = "Load"});

        var reloadButton = new Button(() =>
        {
            DialogueLocalization.Load();

            // Actualiza todos los nodos visibles con la nueva localización
            foreach (var node in graphView.nodes.ToList().OfType<DialogueNode>())
            {
                node.ForcePreviewUpdate();
            }

        }) { text = "Reload" };

        toolbar.Add(reloadButton);

        var clearButton = new Button(() =>
        {
            graphView.DeleteElements(graphView.graphElements.ToList());
        })
        { text = "Clear" };
        toolbar.Add(clearButton);

        rootVisualElement.Add(toolbar);
    }

    private void GenerateMinimap()
    {
        var miniMap = new MiniMap();
        miniMap.anchored = true;
        miniMap.SetPosition(new Rect(10, 30, 200, 140));
        graphView.Add(miniMap);
    }

    private void RequestDataOperation(bool save)
    {
        if (string.IsNullOrEmpty(currentFileName))
        {
            EditorUtility.DisplayDialog("Invalid file name!", "Please enter a valid file name", "OK");
        }

        var saveUtility = GraphSaveUtility.GetInstance(graphView);

        if (save)
        {
            saveUtility.SaveGraph(currentFileName);
        }
        else
        {
            saveUtility.LoadGraph(currentFileName);
        }
    }

    private void OnDisable()
    {
        rootVisualElement.Remove(graphView);
    }
}