using UnityEditor.Experimental.GraphView;
using Xesin.GameplayFramework.AI;

namespace GameplayFramework.AI
{
    public class BTRootNodeView : Node
    {
        BehaviorTree tree;
        public Port output;

        public BTRootNodeView(BehaviorTree tree)
        {
            this.tree = tree;
            capabilities -= Capabilities.Movable;
            capabilities -= Capabilities.Deletable;
            viewDataKey = "BTRoot";
            title = "Root";
            CreateOutputPort();
            inputContainer.AddToClassList("hidden");

            style.top = -100;
        }

        private void CreateOutputPort()
        {
            output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));

            output.portName = string.Empty;
            outputContainer.Add(output);
        }
    }
}
