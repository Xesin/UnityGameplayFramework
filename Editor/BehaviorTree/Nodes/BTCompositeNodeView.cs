using UnityEditor.Experimental.GraphView;
using UnityEngine;
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
            var input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            input.portName = string.Empty;
            inputContainer.Add(input);
        }

        protected override void CreateOutputNodes()
        {
            var output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
            output.portName = string.Empty;
            outputContainer.Add(output);
        }
    }
}
