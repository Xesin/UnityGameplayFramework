using System;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{
    public class BTComposite_Selector : BTComposite
    {
        public override int GetNextChildHandler(BehaviorTreeSearchData searchData, int prevChild, BTNodeResult lastResult)
        {
            int nextChildIdx = RETURN_TO_PARENT_IDX;

            if(prevChild == NOT_INITIALIZED_CHILD)
            {
                nextChildIdx = 0;
            }
            else if(lastResult == BTNodeResult.Failed && (prevChild + 1) < children.Count)
            {
                nextChildIdx = prevChild + 1;
            }

            return nextChildIdx;
        }

#if UNITY_EDITOR
        protected override string GetDefaultName()
        {
            return "Selector";
        }
#endif
    }
}
