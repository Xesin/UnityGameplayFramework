using System;
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

        internal BTNode GetMyNode()
        {
            return (ChildIndex == BTSpecialChild.OwnedByComposite) ? GetParentNode() : (GetParentNode() ? GetParentNode().GetChildNode(childIndex) : null);
        }

        public virtual void OnCeaseRelevant()
        {
            
        }

        public void OnBecomeRelevant()
        {
            
        }
    }
}
