using UnityEngine;

namespace Xesin.GameplayFramework.AI
{
    [CreateAssetMenu(fileName = "Selector", menuName = "TestingSelector")]
    public class BTComposite_Selector : BTCompositeNode
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
    }
}
