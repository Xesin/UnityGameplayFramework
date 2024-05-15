using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using Xesin.GameplayFramework.AI;

namespace GameplayFramework.AI
{
    public class BTRootNodeView : Node
    {
        BehaviorTree tree;
        public Port output;

        public BTRootNodeView(BehaviorTree tree) : base("Packages/com.xesin.gameplay-framework/Editor/BehaviorTree/UXML/BehaviorTreeChildNode.uxml")
        {
            this.tree = tree;
            capabilities -= Capabilities.Movable;
            capabilities -= Capabilities.Deletable;
            viewDataKey = "BTRoot";
            title = "Root";
            CreateOutputPort();
            inputContainer.style.display = DisplayStyle.None;

            this.Q("executionIndex").style.display = DisplayStyle.None;

            UseDefaultStyling();
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.xesin.gameplay-framework/Editor/BehaviorTree/UXML/BehaviorTreeChildNode.uss");
            styleSheets.Add(styleSheet);
            style.top = -100;
        }

        private void CreateOutputPort()
        {
            output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));

            output.portName = string.Empty;
            outputContainer.Add(output);

            inputContainer.style.display = DisplayStyle.None;
        }
    }
}
