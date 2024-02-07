using UnityEngine;

namespace Xesin.GameplayFramework.AI
{

    public abstract class BTAuxiliaryNode : BTNode
    {
        [SerializeField] protected ushort childIndex;

        public ushort ChildIndex => childIndex;

        public virtual void TickNode(BehaviorTreeComponent owner, float deltaTime)
        {

        }

        public void InitializeParentLink(ushort InChildIndex)
        {
            childIndex = InChildIndex;
        }
    }
}
