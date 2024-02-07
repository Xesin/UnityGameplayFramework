using System;
using System.Collections.Generic;
using System.Linq;

namespace Xesin.GameplayFramework.AI
{
    public enum BTDecoratorLogicType
    {
        Invalid,
        Test,
        And,
        Or,
        Not
    }

    public enum BTChildIndex
    {
        FirstNode,
        TaskNode,
    }

    public struct BTCompositeChild
    {

        public BTCompositeNode childComposite;

        public BTTaskNode childTask;

        public List<BTDecorator> decorators;

        public List<BTDecoratorLogic> decoratorsOps;
    }

    public struct BTDecoratorLogic
    {

        public BTDecoratorLogicType operation;
        public ushort number;

        public BTDecoratorLogic(BTDecoratorLogicType inOperation, ushort inNumber) 
        {
            operation = inOperation;
            number = inNumber;
        }
    };

    struct OperationStackInfo
    {
        public ushort numLeft;
        public BTDecoratorLogicType Op;
        public bool hasForcedResult;
        public bool forcedResult;

        public OperationStackInfo(BTDecoratorLogic DecoratorOp)
        {
            numLeft = DecoratorOp.number;
            Op = DecoratorOp.operation;
            forcedResult = false;
            hasForcedResult = false;
        }
    };

    public class BTCompositeNode : BTNode
    {
        public const int NOT_INITIALIZED_CHILD = -1;
        public const int RETURN_TO_PARENT_CHILD = -2;

        public List<BTService> services = new List<BTService>();
        public List<BTDecorator> decorators = new List<BTDecorator>();

        public List<BTCompositeChild> children = new List<BTCompositeChild>();

        private ushort lastExecutionIndex;

        internal override void InitializeInSubtree(BehaviorTreeComponent ownerComp, BehaviorTree behaviorTree, int instanceIndex)
        {
            base.InitializeInSubtree(ownerComp, behaviorTree, instanceIndex);
            lastExecutionIndex += (ushort)instanceIndex;
        }

        public int GetChildIndex(BTNode childNode)
        {
            for (int childIndex = 0; childIndex < children.Count; childIndex++)
            {
                if (children[childIndex].childComposite == childNode ||
                    children[childIndex].childTask == childNode)
                {
                    return childIndex;
                }
            }

            return RETURN_TO_PARENT_CHILD;
        }

        public BTNode GetChildNode(int index)
        {
            return children.IsValidIndex(index) ? (
                children[index].childComposite ? 
                    children[index].childComposite : 
                    children[index].childTask) : null;
        }

        public ushort GetChildExecutionIndex(int index, BTChildIndex childMode = BTChildIndex.TaskNode)
        {
            BTNode childNode = GetChildNode(index);

            if(childNode)
            {
                int offset = 0;

                if(childMode == BTChildIndex.FirstNode)
                {
                    offset += children[index].decorators.Count;

                    if (children[index].childTask)
                    {
                        offset += children[index].childTask.services.Count;
                    }
                }

                return checked((ushort) (childNode.GetExecutionIndex() - offset));
            }

            return (ushort)(lastExecutionIndex + 1);
        }

        internal ushort GetLastExecutionIndex()
        {
            return lastExecutionIndex;
        }

        public bool DoDecoratorsAllowExecution(BehaviorTreeComponent ownerComp, int instanceIdx, int childIdx)
        {
            if(!children.IsValidIndex(childIdx))
            {
                return false;
            }

            BTCompositeChild childInfo = children[childIdx];
            bool result = true;
            if (childInfo.decorators.Count == 0)
                return result;

            BehaviorTree myInstance = ownerComp.knownInstances[instanceIdx];
            ushort instanceIdxUshort = (ushort)instanceIdx;

            if(childInfo.decoratorsOps.Count == 0)
            {
                for (int decoratorIndex = 0; decoratorIndex < childInfo.decorators.Count; decoratorIndex++)
                {
                    BTDecorator testDecorator = childInfo.decorators[decoratorIndex];
                    bool isAllowed = testDecorator ? testDecorator.CanExecute(ownerComp) : false;

                    if(!isAllowed)
                    {
                        result = false;
                    }
                }
            }
            else
            {
                List<OperationStackInfo> operationStack = new List<OperationStackInfo>(10);

                int nodeDecoratorIdx = -1;
                int failedDecoratorIdx = -1;
                bool shouldrestoreNodeIndex = true;

                for (int operationIndex = 0; operationIndex < childInfo.decoratorsOps.Count; operationIndex++)
                {
                    BTDecoratorLogic decoratorOp = childInfo.decoratorsOps[operationIndex];
                    if(IsLogicOp(decoratorOp))
                    {
                        operationStack.Add(new OperationStackInfo(decoratorOp));
                    }
                    else if(decoratorOp.operation == BTDecoratorLogicType.Test)
                    {
                        bool hasOverride = operationStack.Count > 0 ? operationStack[^1].hasForcedResult : false;
                        bool currentOverride = operationStack.Count > 0 ? operationStack[^1].forcedResult : false;

                        if(shouldrestoreNodeIndex)
                        {
                            shouldrestoreNodeIndex = false;
                            nodeDecoratorIdx = decoratorOp.number;
                        }
                        BTDecorator testDecorator = childInfo.decorators[decoratorOp.number];
                        bool isAllowed = hasOverride ? currentOverride : testDecorator.CanExecute(ownerComp);

                        result = UpdateOperationStack(ownerComp, operationStack, isAllowed, failedDecoratorIdx, nodeDecoratorIdx, shouldrestoreNodeIndex);

                        if(operationStack.Count == 0)
                        {
                            break;
                        }
                    }

                }
            }

            return result;
        }

        private bool UpdateOperationStack(BehaviorTreeComponent ownerComp, List<OperationStackInfo> stack, bool testResult, int failedDecoratorIdx, int nodeDecoratorIdx, bool shouldrestoreNodeIndex)
        {
            if (stack.Count == 0)
                return testResult;

            OperationStackInfo currentOp = stack[^1];
            currentOp.numLeft--;

            if(currentOp.Op == BTDecoratorLogicType.And)
            {
                if(!currentOp.hasForcedResult && !testResult)
                {
                    currentOp.hasForcedResult = true;
                    currentOp.forcedResult = testResult;
                }
            }
            else if (currentOp.Op == BTDecoratorLogicType.Or)
            {
                if (!currentOp.hasForcedResult && testResult)
                {
                    currentOp.hasForcedResult = true;
                    currentOp.forcedResult = testResult;
                }
            }
            else if(currentOp.Op == BTDecoratorLogicType.Not)
            {
                testResult = !testResult;
            }

            if(stack.Count == 1)
            {
                shouldrestoreNodeIndex = true;

                if(!testResult && failedDecoratorIdx == -1)
                {
                    failedDecoratorIdx = nodeDecoratorIdx;
                }
            }

            if(currentOp.hasForcedResult)
            {
                testResult = currentOp.forcedResult;
            }

            if(currentOp.numLeft == 0)
            {
                stack.RemoveAt(stack.Count - 1);
                return UpdateOperationStack(ownerComp, stack, testResult, failedDecoratorIdx, nodeDecoratorIdx, shouldrestoreNodeIndex);
            }

            return testResult;
        }

        private bool IsLogicOp(BTDecoratorLogic info)
        {
            return (info.operation != BTDecoratorLogicType.Test) && (info.operation != BTDecoratorLogicType.Invalid);
        }
    }
}
