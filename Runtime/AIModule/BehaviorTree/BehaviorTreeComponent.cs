using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{
    public struct BranchActionInfo
    {
        BranchActionInfo(BTBranchAction InAction)
        {
            action = InAction;
            node = null;
            continueWithResult = BTNodeResult.Succeeded;
        }

        BranchActionInfo(BTNode InNode, BTBranchAction InAction)
        {
            node = InNode;
            action = InAction;
            continueWithResult = BTNodeResult.Succeeded;
        }

        BranchActionInfo(BTNode InNode, BTNodeResult InContinueWithResult, BTBranchAction InAction)
        {
            node = InNode;
            continueWithResult = InContinueWithResult;
            action = InAction;
        }

        public BTNode node;
        public BTNodeResult continueWithResult;
        public BTBranchAction action;
    };

    public struct BTNodeIndex
    {
        const ushort InvalidIndex = ushort.MaxValue;

        public ushort instanceIndex;
        public ushort executionIndex;

        public BTNodeIndex(ushort inInstanceIndex, ushort inExecutionIndex)
        {
            instanceIndex = inInstanceIndex;
            executionIndex = inExecutionIndex;
        }

        bool TakesPriorityOver(BTNodeIndex other)
        {
            if(instanceIndex != other.instanceIndex)
            {
                return instanceIndex < other.instanceIndex;
            }

            return executionIndex < other.executionIndex;
        }

        bool IsSet()
        {
            return instanceIndex < InvalidIndex;
        }

        public override bool Equals(object obj)
        {
            if (obj is not BTNodeIndex other) return false;

            return other.executionIndex == executionIndex && other.instanceIndex == instanceIndex;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(instanceIndex, executionIndex);
        }
    }

    struct BehaviorTreeSearchData
    {
        public BTNodeIndex searchRootNode;
        public BTNodeIndex searchstart;
        public BTNodeIndex searchEnd;

        public int searchId;
        public int rollbackInstanceIdx;

    }

    public class BehaviorTreeComponent : BrainComponent
    {
        [SerializeField] private BehaviorTree defaultBehaviorTree;

        public bool IsRunningTree { get; protected set; } = false;
        public bool IsPausedTree { get; protected set; } = false;


        protected bool loopExecution = false;


        protected TreeStartInfo treeStartInfo;
        protected List<BehaviorTree> knownInstances = new List<BehaviorTree>();
        protected List<BranchActionInfo> pendingBranchActionRequests = new List<BranchActionInfo>();
        protected int activeInstanceIndex = 0;
        protected BTBranchAction suspendedBranchActions;
        private BehaviorTreeSearchData SearchData;

        public override void StartLogic()
        {
            if (IsRunningTree)
            {
                return;
            }

            if(!treeStartInfo.IsSet())
            {
                treeStartInfo.asset = defaultBehaviorTree;
            }

            if(treeStartInfo.IsSet())
            {
                treeStartInfo.pendingInitialize = true;
                ProcessPendingInitialize();
            }
            else
            {
                Debug.Log("Clould not find BehaviourTree to run");
            }
        }

        private void ProcessPendingInitialize()
        {
            StopTree();
            loopExecution = treeStartInfo.executionMode == TreeExecutionMode.Looped;
            IsRunningTree = true;



            treeStartInfo.pendingInitialize = false;
        }

        protected bool PushInstance(BehaviorTree treeAsset)
        {
            if(treeAsset.BlackboardAsset && blackboardComponent && !blackboardComponent.IsCompatibleWith(treeAsset.BlackboardAsset))
            {
                return false;
            }

            var newInstance = Instantiate(treeAsset);
            newInstance.activeNode = null;

            newInstance.Initialize(this);

            knownInstances.Add(newInstance);
            activeInstanceIndex = knownInstances.Count - 1;

            var rootNode = newInstance.rootNode;

            for (int i = 0; i < rootNode.services.Count; i++)
            {
                var serviceNode = rootNode.services[i];
                serviceNode.NotifyParentActivation();
            }

            RequestExecution(rootNode, activeInstanceIndex, rootNode, 0, BTNodeResult.InProgress);

            return true;
        }

        private void RequestExecution(BTCompositeNode RequestedOn, int instanceIndex, BTNode RequestedBy, int requestedChildIndex, BTNodeResult continueWithResult)
        {
            if(!IsRunningTree || instanceIndex >= knownInstances.Count)
            {
                Debug.Log("skip: tree is not running");
            }

            ushort intanceIdx = unchecked((ushort)instanceIndex);
            bool switchToHigherPriority = continueWithResult == BTNodeResult.Aborted;

            ushort lastExecutionIndex = ushort.MaxValue;
            BTNodeIndex executionIdx = new BTNodeIndex();
            executionIdx.instanceIndex = intanceIdx;
            executionIdx.executionIndex = RequestedBy.GetExecutionIndex();

            // make sure that the request is not coming from a node that has pending branch actions since it won't be accessible anymore
            if (suspendedBranchActions != BTBranchAction.None)
            {
                if ((suspendedBranchActions & ~(BTBranchAction.Changing_Topology_Actions)) != BTBranchAction.None)
                {
                    Debug.LogWarning("Caller should be converted to new Evaluate/Activate/DeactivateBranch API instead of using this RequestExecution directly");
                }

                for (int i = 0; i < pendingBranchActionRequests.Count; i++)
                {
                    var info = pendingBranchActionRequests[i];

                    BTCompositeNode branchRoot = null;
                    switch (info.action)
                    {
                        case BTBranchAction.DecoratorDeactivate:
                            if(info.node is BTDecorator decorator)
                            {
                                if(decorator.GetParentNode() && decorator.GetParentNode().children.IsValidIndex(decorator.ChildIndex))
                                {
                                    branchRoot = decorator.GetParentNode().children[decorator.ChildIndex].ChildComposite;
                                }
                            }
                            break;
                        case BTBranchAction.UnregisterAuxNodes:
                            if(info.node is BTCompositeNode compNode)
                            {
                                branchRoot = compNode;
                            }
                            break;
                        default:
                            break;
                    }

                    if(branchRoot)
                    {
                        int branchRootInstanceIdx = FindInstanceContainingNode(branchRoot);
                        if(branchRootInstanceIdx != -1)
                        {
                            ushort branchRootInstanceIdxUShort = checked((ushort) branchRootInstanceIdx);


                        }
                    }
                }
            }

            if(switchToHigherPriority && requestedChildIndex >= 0)
            {
                executionIdx.executionIndex = RequestedOn.GetChildExecutionIndex(requestedChildIndex, BTChildIndex.FirstNode);

                lastExecutionIndex = RequestedOn.GetChildExecutionIndex(requestedChildIndex + 1, BTChildIndex.FirstNode);
            }

            BTNodeIndex SearchEnd = new BTNodeIndex(intanceIdx, lastExecutionIndex);

            if(switchToHigherPriority)
            {

            }
            else
            {

            }


        }

        private int FindInstanceContainingNode(BTNode node)
        {
            int instanceIdx = -1;

            if(knownInstances.Count > 0)
            {
                if (knownInstances[activeInstanceIndex].activeNode != node)
                {
                    BTNode rootNode = node;

                    while(rootNode.GetParentNode())
                    {
                        rootNode = rootNode.GetParentNode();
                    }

                    for (int i = 0; i < knownInstances.Count; i++)
                    {
                        if (knownInstances[i].rootNode == rootNode)
                        {
                            instanceIdx = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                instanceIdx = activeInstanceIndex;
            }

            return instanceIdx;
        }

        public override void RestartLogic()
        {
            
        }

        public override void StopLogic()
        {
            
        }

        public override void PauseLogic()
        {
            
        }

        public override void ResumeLogic()
        {
            
        }


        protected void StopTree()
        {

        }

        protected void RestartTree()
        {

        }
    }
}
