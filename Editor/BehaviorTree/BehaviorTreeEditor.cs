using GameplayFramework;
using GameplayFramework.AI;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
using Xesin.GameplayFramework.AI;

public class BehaviorTreeEditor : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;
    [SerializeField] 
    private BehaviorTree currentTree;

    BehaviorTreeView treeView;
    InspectorView inspectorView;


    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.CloneTree();
        root.Add(labelFromUXML);


        treeView = root.Q<BehaviorTreeView>();
        inspectorView = root.Q<InspectorView>();
        if(currentTree != null )
        {
            treeView.PopulateView(currentTree);
        }

    }

    internal static bool ShowGraphEditWindow(BehaviorTree tree)
    {
        if (tree == null)
            return false;

        foreach (var w in Resources.FindObjectsOfTypeAll<BehaviorTreeEditor>())
        {
            if (w.currentTree == tree)
            {
                w.Focus();
                return true;
            }
        }

        var window = CreateWindow<BehaviorTreeEditor>(typeof(BehaviorTreeEditor), typeof(SceneView));
        window.Initialize(tree);
        window.Focus();
        return true;
    }

    private void Initialize(BehaviorTree tree)
    {
        titleContent = new GUIContent(tree.name);
        treeView.PopulateView(tree);
        currentTree = tree;
    }

    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceId, int line)
    {
        if (Selection.activeObject is BehaviorTree tree)
        {
            return ShowGraphEditWindow(tree);
        }

        return false;
    }
}
