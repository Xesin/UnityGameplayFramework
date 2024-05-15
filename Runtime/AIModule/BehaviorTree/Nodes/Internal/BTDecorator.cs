using System;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{

    public class BTDecorator : BTAuxiliaryNode
    {
        [SerializeField] private bool inverseCondition = false;
        [SerializeField] private BTFlowAbortMode flowAbortMode;

        public bool CanExecute(BehaviorTreeComponent ownerComp)
        {
            return IsInversed() != CalculateRawConditionValue(ownerComp);
        }

        private bool IsInversed()
        {
            return inverseCondition;
        }

        protected virtual bool CalculateRawConditionValue(BehaviorTreeComponent ownerComp)
        {
            return true;
        }

        internal BTFlowAbortMode GetFlowAbortMode()
        {
            return flowAbortMode;
        }

#if UNITY_EDITOR
        protected override string GetDefaultName()
        {
            return "Decorator";
        }
#endif
    }
}
