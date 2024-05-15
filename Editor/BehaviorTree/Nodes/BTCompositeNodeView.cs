using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Xesin.GameplayFramework.AI;

namespace GameplayFramework.AI
{
    public class BTCompositeNodeView : BTNodeView
    {
        public BTCompositeNodeView(BTNode node) : base(node)
        {

        }

        protected override void CreateInputNodes()
        {
            input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            input.portName = string.Empty;
            inputContainer.Add(input);
        }

        protected override void CreateOutputNodes()
        {
            output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
            output.portName = string.Empty;
            outputContainer.Add(output);
        }

        public override void UpdateView(bool notifyParent = false)
        {
            base.UpdateView(notifyParent);

            foreach (var item in output.connections)
            {
                if(item.input.node is BTNodeView nodeView)
                {
                    nodeView.UpdateView();
                }
            }
        }
    }
}
