using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Xesin.GameplayFramework.AI;

namespace GameplayFramework.AI
{
    public class BTNodeView : Node
    {
        public BTNode node;

        public BTNodeView(BTNode node)
        {
            this.node = node;
            title = node.nodeName;
            viewDataKey = node.nodeId;

            style.left = node.position.x;
            style.top = node.position.y;

            CreateInputNodes();
            CreateOutputNodes();
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);

            node.position.x = newPos.xMin;
            node.position.y = newPos.yMin;
        }

        protected virtual void CreateOutputNodes()
        {

        }

        protected virtual void CreateInputNodes()
        {

        }
    }
}
