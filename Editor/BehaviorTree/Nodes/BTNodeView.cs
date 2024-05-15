using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Xesin.GameplayFramework.AI;

namespace GameplayFramework.AI
{
    public class BTNodeView : Node
    {
        public BTNode node;
        public Port input;
        public Port output;

        public VisualElement DecoratorsContainer { get; private set; }
        public VisualElement ServicesContainer { get; private set; }
        public Label ExecutionIndexLabel { get; private set; }

        public BTNodeView(BTNode node) : base("Packages/com.xesin.gameplay-framework/Editor/BehaviorTree/UXML/BehaviorTreeChildNode.uxml")
        {
            this.node = node;
            title = node.nodeName;
            viewDataKey = node.nodeId;


            UseDefaultStyling();
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.xesin.gameplay-framework/Editor/BehaviorTree/UXML/BehaviorTreeChildNode.uss");
            styleSheets.Add(styleSheet);
            style.left = node.position.x;
            style.top = node.position.y;

            CreateInputNodes();
            CreateOutputNodes();

            DecoratorsContainer = this.Q("decorators-container");
            ServicesContainer = this.Q("services-container");
            ExecutionIndexLabel = this.Q<Label>("executionIndexLabel");

            UpdateView();
        }

        public BTNodeView(BTNode node, string uxmlPath) : base(uxmlPath)
        {
            this.node = node;
            title = node.nodeName;
            viewDataKey = node.nodeId;


            UseDefaultStyling();
            style.left = node.position.x;
            style.top = node.position.y;

            CreateInputNodes();
            CreateOutputNodes();

            ExecutionIndexLabel = this.Q<Label>("executionIndexLabel");

            UpdateView();
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);

            node.position.x = newPos.xMin;
            node.position.y = newPos.yMin;

            if (node.GetParentNode())
            {
                node.GetParentNode().SortChildren();
                UpdateView(true);
            }
        }

        protected virtual void CreateOutputNodes()
        {

        }

        protected virtual void CreateInputNodes()
        {

        }

        protected void HideInputNode()
        {
            inputContainer.style.display = DisplayStyle.None;
        }

        protected void HideOutputNode()
        {
            outputContainer.style.display = DisplayStyle.None;
        }

        public void AddDecorator(BTAuxNodeView auxNodeView)
        {
            DecoratorsContainer.Add(auxNodeView);
        }

        public void AddService(BTAuxNodeView auxNodeView)
        {
            ServicesContainer.Add(auxNodeView);
        }

        public virtual void UpdateView(bool notifyParent = false)
        {
            if (notifyParent)
            {
                if (input != null && input.connected)
                {
                    foreach (var item in input.connections)
                    {
                        if (item.output.node is BTNodeView nodeView)
                        {
                            nodeView.UpdateView(false);
                        }
                    }
                }
            }

            if (DecoratorsContainer != null)
            {
                foreach (var item in DecoratorsContainer.Children())
                {
                    if (item is BTNodeView nodeView)
                    {
                        nodeView.UpdateView(false);
                    }
                }
            }

            if (ServicesContainer != null)
            {
                foreach (var item in ServicesContainer.Children())
                {
                    if (item is BTNodeView nodeView)
                    {
                        nodeView.UpdateView(false);
                    }
                }
            }

            ushort execIndex = node.GetExecutionIndex();
            ExecutionIndexLabel.text = execIndex == BTComposite.NOT_INITIALIZED_CHILD ? "-1" : node.GetExecutionIndex().ToString();
            title = node.nodeName;
        }
    }
}
