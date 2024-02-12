using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    [Serializable]
    public class BTCompositeChild
    {

        public BTComposite childComposite;

        public BTTask childTask;

        [SerializeField] private List<BTDecorator> decorators;

        [SerializeField] private List<BTDecoratorLogic> decoratorsOps;

        public List<BTDecoratorLogic> DecoratorsOp => decoratorsOps ??= new List<BTDecoratorLogic>();
        public List<BTDecorator> Decorators => decorators ??= new List<BTDecorator>();

        public BTNode GetNode()
        {
            return childComposite ? childComposite : childTask;
        }

#if UNITY_EDITOR
        public Vector2 GetPosition()
        {
            return GetNode() ? GetNode().position : Vector2.zero;
        }
#endif
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

    public abstract class BTComposite : BTNode
    {
        public const byte NOT_INITIALIZED_CHILD = 255;
        public const byte RETURN_TO_PARENT_IDX = 254;

        [SerializeField] private ushort lastExecutionIndex;

        public List<BTService> services = new List<BTService>();

        public List<BTCompositeChild> children = new List<BTCompositeChild>();

        protected byte currentChild;
        protected byte overrideChild;

        public void InitializeComposite(ushort inLastExecutionIndex)
        {
            lastExecutionIndex = inLastExecutionIndex;
        }

        public int FindChildToExecute(BehaviorTreeSearchData searchData, BTNodeResult lastResult)
        {
            int retIdx = RETURN_TO_PARENT_IDX;

            if(children.Count > 0)
            {
                int childIdx = GetNextChild(searchData, currentChild, lastResult);

                while(children.IsValidIndex(childIdx) && !searchData.postponeSearch)
                {
                    if(DoDecoratorsAllowExecution(searchData.ownerComp, searchData.ownerComp.ActiveInstanceIdx, childIdx))
                    {
                        OnChildActivation(searchData, childIdx);
                        retIdx = childIdx;
                        break;
                    }
                    else
                    {
                        lastResult = BTNodeResult.Failed;
                    }

                    childIdx = GetNextChild(searchData, childIdx, lastResult);
                }
            }

            return retIdx;
        }

        public int GetChildIndex(BehaviorTreeSearchData searchData, BTNode childNode)
        {
            if(childNode.GetParentNode() != this)
            {
                return currentChild;
            }

            return GetChildIndex(childNode);
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

            return RETURN_TO_PARENT_IDX;
        }



        private void OnChildActivation(BehaviorTreeSearchData searchData, BTNode childNode)
        {
            OnChildActivation(searchData, GetChildIndex(searchData, childNode));
        }

        public void OnChildActivation(BehaviorTreeSearchData searchData, int childIndex)
        {
            BTCompositeChild childInfo = children[childIndex];

            if(childInfo.childComposite)
            {
                childInfo.childComposite.OnNodeActivation(searchData);
            }

            currentChild = (byte) childIndex;
        }

        public void OnChildDeactivation(BehaviorTreeSearchData searchData, BTNode childNode, BTNodeResult nodeResult, bool requestedFromValidInstance)
        {
            OnChildDeactivation(searchData, GetChildIndex(searchData, childNode), nodeResult, requestedFromValidInstance);
        }

        public void OnChildDeactivation(BehaviorTreeSearchData searchData, int childIndex, BTNodeResult nodeResult, bool requestedFromValidInstance)
        {
            BTCompositeChild childInfo = children[childIndex];

            if(childInfo.childTask)
            {
                for (int serviceIndex = 0; serviceIndex < childInfo.childTask.services.Count; serviceIndex++)
                {
                    BTService service = childInfo.childTask.services[serviceIndex];
                    searchData.AddUniqueUpdate(new BehaviorTreeSearchUpdate(service, searchData.ownerComp.ActiveInstanceIdx, BTNodeUpdateMode.Remove));
                }
            }

            if (childInfo.childComposite)
            {
                childInfo.childComposite.OnNodeDeactivation(searchData, nodeResult);
            }

        }

        public void OnNodeActivation(BehaviorTreeSearchData searchData)
        {
            OnNodeRestart(searchData);

            NotifyNodeActivation(searchData);

            for (int serviceIndex = 0; serviceIndex < services.Count; serviceIndex++)
            {
                BTService service = services[serviceIndex];
                searchData.AddUniqueUpdate(new BehaviorTreeSearchUpdate(service, searchData.ownerComp.ActiveInstanceIdx, BTNodeUpdateMode.Add));

                service.NotifyParentActivation(searchData);
            }
        }

        public void OnNodeDeactivation(BehaviorTreeSearchData searchData, BTNodeResult nodeResult)
        {
            NotifyNodeDeactivation(searchData, nodeResult);

            for (int serviceIndex = 0; serviceIndex < services.Count; serviceIndex++)
            {
                BTService service = services[serviceIndex];
                searchData.AddUniqueUpdate(new BehaviorTreeSearchUpdate(service, searchData.ownerComp.ActiveInstanceIdx, BTNodeUpdateMode.Remove));
            }
        }

        public void OnNodeRestart(BehaviorTreeSearchData searchData)
        {
            currentChild = NOT_INITIALIZED_CHILD;
            overrideChild = NOT_INITIALIZED_CHILD;
        }

        public int GetNextChild(BehaviorTreeSearchData searchData, int lastChildIdx, BTNodeResult lastResult)
        {
            int nextChildIndex = RETURN_TO_PARENT_IDX;
            ushort activeInstanceIdx = searchData.ownerComp.ActiveInstanceIdx;

            if(lastChildIdx == NOT_INITIALIZED_CHILD && searchData.searchStart.IsSet() &&
                new BTNodeIndex(activeInstanceIdx, GetExecutionIndex()).TakesPriorityOver(searchData.searchStart))
            {
                nextChildIndex = GetMatchingChildIndex(activeInstanceIdx, searchData.searchStart);
            }
            else if(overrideChild != NOT_INITIALIZED_CHILD && !searchData.ownerComp.IsRestartPending())
            {
                nextChildIndex = overrideChild;
                overrideChild = NOT_INITIALIZED_CHILD;
            }
            else
            {
                nextChildIndex = GetNextChildHandler(searchData, lastChildIdx, lastResult);
            }

            return nextChildIndex;
        }

        public virtual int GetNextChildHandler(BehaviorTreeSearchData searchData, int lastChildIdx, BTNodeResult lastResult)
        {
            return RETURN_TO_PARENT_IDX;
        }

        private int GetMatchingChildIndex(ushort activeInstanceIdx, BTNodeIndex nodeIdx)
        {
            int outsideRange = RETURN_TO_PARENT_IDX;
            int unlimitedRange = children.Count - 1;

            if(activeInstanceIdx == nodeIdx.instanceIndex)
            {
                if(GetExecutionIndex() > nodeIdx.executionIndex)
                {
                    return outsideRange;
                }

                for (int childIndex = 0; childIndex < children.Count; childIndex++)
                {
                    ushort firstIndexIndBranch = GetChildExecutionIndex(childIndex, BTChildIndex.FirstNode);
                    {
                        if(firstIndexIndBranch > nodeIdx.executionIndex)
                        {
                            return childIndex > 0 ? childIndex - 1 : 0;
                        }
                    }
                }

                return unlimitedRange;
            }

            return (activeInstanceIdx > nodeIdx.instanceIndex) ? unlimitedRange : outsideRange;
        }

        public BTNode GetChildNode(int index)
        {
            return children.IsValidIndex(index) ? (
                children[index].childComposite ? 
                    children[index].childComposite : 
                    children[index].childTask) : null;
        }

        protected void RequestDelayedExecution(BehaviorTreeComponent ownerComp, BTNodeResult lastResult)
        {
            ownerComp.RequestExecution(lastResult);
        }   

        public ushort GetChildExecutionIndex(int index, BTChildIndex childMode = BTChildIndex.TaskNode)
        {
            BTNode childNode = GetChildNode(index);

            if(childNode)
            {
                int offset = 0;

                if(childMode == BTChildIndex.FirstNode)
                {
                    offset += children[index].Decorators.Count;

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
            if (childInfo.Decorators.Count == 0)
                return result;

            BehaviorTree myInstance = ownerComp.knownInstances[instanceIdx];
            ushort instanceIdxUshort = (ushort)instanceIdx;

            if(childInfo.DecoratorsOp.Count == 0)
            {
                for (int decoratorIndex = 0; decoratorIndex < childInfo.Decorators.Count; decoratorIndex++)
                {
                    BTDecorator testDecorator = childInfo.Decorators[decoratorIndex];
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

                for (int operationIndex = 0; operationIndex < childInfo.DecoratorsOp.Count; operationIndex++)
                {
                    BTDecoratorLogic decoratorOp = childInfo.DecoratorsOp[operationIndex];
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
                        BTDecorator testDecorator = childInfo.Decorators[decoratorOp.number];
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

        protected virtual void NotifyChildExecution(BehaviorTreeComponent ownerComp, int childIdx, BTNodeResult nodeResult)
        {

        }

        protected virtual void NotifyNodeActivation(BehaviorTreeSearchData searchData)
        {

        }

        protected virtual void NotifyNodeDeactivation(BehaviorTreeSearchData searchData, BTNodeResult nodeResult)
        {

        }

#if UNITY_EDITOR
        public void SortChildren()
        {
            children.Sort((x, y) => x.GetPosition().x.CompareTo(y.GetPosition().x));

            int nextExecutionIndex = GetNexExecutionIndex(GetExecutionIndex());

            for (int i = 0; i < children.Count; i++)
            {
                var node = children[i].GetNode();

                if(node is BTComposite composite)
                {
                    for (int serviceIndex = 0; serviceIndex < composite.services.Count; serviceIndex++)
                    {
                        composite.services[serviceIndex].SetExecutionIndex(nextExecutionIndex);
                        nextExecutionIndex = GetNexExecutionIndex(composite.services[serviceIndex].GetExecutionIndex());
                    }
                }
                else if (node is BTTask task)
                {
                    for (int serviceIndex = 0; serviceIndex < task.services.Count; serviceIndex++)
                    {
                        task.services[serviceIndex].SetExecutionIndex(nextExecutionIndex);
                        nextExecutionIndex = GetNexExecutionIndex(task.services[serviceIndex].GetExecutionIndex());
                    }
                }

                for (int decoratorIndex = 0; decoratorIndex < children[i].Decorators.Count; decoratorIndex++)
                {
                    children[i].Decorators[decoratorIndex].SetExecutionIndex(nextExecutionIndex);
                    nextExecutionIndex = GetNexExecutionIndex(children[i].Decorators[decoratorIndex].GetExecutionIndex());
                }

                node.SetExecutionIndex(nextExecutionIndex);
                nextExecutionIndex = GetNexExecutionIndex(node.GetExecutionIndex());
            }
        }
#endif
    }
}
