using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Xesin.GameplayFramework.AI;

namespace GameplayFramework.AI
{
    public class BTTaskNodeView : BTNodeView
    {
        public BTTaskNodeView(BTNode node) : base(node)
        {
        }

        protected override void CreateInputNodes()
        {
            input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            input.portName = string.Empty;
            inputContainer.Add(input);
        }
    }
}
