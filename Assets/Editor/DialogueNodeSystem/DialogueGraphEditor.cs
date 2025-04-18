using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class DialogueGraphEditor : EditorWindow
{
    private DialogueGraphView graphView;

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
        
        toolbar.Add(new Button(() => SaveData()) {text = "Save Data"});
        toolbar.Add(new Button(() => LoadData()) {text = "Load Data"});

        rootVisualElement.Add(toolbar);
    }

    private object SaveData()
    {
        throw new NotImplementedException();
    }

    private object LoadData()
    {
        throw new NotImplementedException();
    }

    private void OnDisable()
    {
        rootVisualElement.Remove(graphView);
    }
}