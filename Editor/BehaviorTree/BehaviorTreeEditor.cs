using GameplayFramework;
using GameplayFramework.AI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Xesin.GameplayFramework.AI;

public class BehaviorTreeEditor : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    BehaviorTreeView treeView;
    InspectorView inspectorView;

    [MenuItem("Window/Gameplay/AI/BehaviorTreeEditor")]
    public static void OpenWindow()
    {
        BehaviorTreeEditor wnd = GetWindow<BehaviorTreeEditor>();
        wnd.titleContent = new GUIContent("BehaviorTreeEditor");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.CloneTree();
        root.Add(labelFromUXML);

        treeView = root.Q<BehaviorTreeView>();
        inspectorView = root.Q<InspectorView>();

        OnSelectionChange();
    }

    private void OnSelectionChange()
    {
        BehaviorTree tree = Selection.activeObject as BehaviorTree;
        if(tree)
        {
            treeView.PopulateView(tree);
        }
    }
}
